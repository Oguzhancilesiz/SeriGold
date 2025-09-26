using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.EntityFrameworkCore;
using SeriGold.Core.Abstracts;
using SeriGold.Core.Hubs;
using SeriGold.DAL;
using SeriGold.Entity;
using SeriGold.Services.Abstracts;
using SeriGold.Services.Concrete;
using SeriGold.Services.MapsterMap;

namespace SeriGold.WebUI
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // DbContext
            builder.Services.AddDbContext<BaseContext>(opt =>
                opt.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

            // Core DI
            builder.Services.AddScoped<IEFContext, BaseContext>();
            builder.Services.AddScoped(typeof(IBaseRepository<>), typeof(BaseRepository<>));
            builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

            // Identity
            builder.Services
                .AddIdentity<AppUser, AppRole>(o =>
                {
                    o.Password.RequiredLength = 6;
                    o.Password.RequireDigit = true;
                    o.Password.RequireUppercase = true;
                    o.Password.RequireLowercase = true;
                    o.Password.RequireNonAlphanumeric = false;
                })
                .AddEntityFrameworkStores<BaseContext>()
                .AddDefaultTokenProviders();

            builder.Services.ConfigureApplicationCookie(o =>
            {
                o.LoginPath = "/Admin/Account/Login";
                o.AccessDeniedPath = "/Admin/Account/Denied";
            });

            // App servisleri
            builder.Services.AddScoped<ISiteSettingService, SiteSettingService>();
            builder.Services.AddScoped<IFaqService, FaqService>();
            builder.Services.AddScoped<IServiceItemService, ServiceItemService>();

            // Mapster
            MapsterConfig.Register();

            // Diğer servisler
            builder.Services.AddHttpClient();
            builder.Services.AddMemoryCache();

            builder.Services.AddResponseCompression(opts =>
            {
                opts.EnableForHttps = true;
                opts.Providers.Add<GzipCompressionProvider>();
                opts.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(new[] { "application/octet-stream" });
            });

            builder.Services.AddSignalR(o =>
            {
                o.ClientTimeoutInterval = TimeSpan.FromSeconds(30);
                o.KeepAliveInterval = TimeSpan.FromSeconds(10);
                o.EnableDetailedErrors = false;
            });

            builder.Services.AddControllersWithViews();

            // Rate providers + engine
            builder.Services.AddHttpClient<HaremAltinProvider>();
            builder.Services.AddHttpClient<TruncgilFxProvider>();
            builder.Services.AddHttpClient<FastForexProvider>();

            builder.Services.AddTransient<IRateProvider, FastForexProvider>();
            builder.Services.AddTransient<IRateProvider, TruncgilFxProvider>();
            builder.Services.AddTransient<IRateProvider, HaremAltinProvider>();
            builder.Services.AddTransient<IRateProvider, SerbestFxProvider>();
            builder.Services.AddHostedService<RateEngine>();

            // CORS
            builder.Services.AddCors(o => o.AddPolicy("any", p => p
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials()
                .SetIsOriginAllowed(_ => true)));

            var app = builder.Build();

            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseResponseCompression();

            var provider = new FileExtensionContentTypeProvider();
            provider.Mappings[".webmanifest"] = "application/manifest+json";
            app.UseStaticFiles(new StaticFileOptions { ContentTypeProvider = provider });

            app.UseRouting();

            app.UseCors("any");

            // Doğru sıra:
            app.UseAuthentication();
            app.UseAuthorization();

            app.MapHub<RatesHub>("/ratesHub");
            app.MapControllers();

            // Area route'u önce koymak daha berrak
            app.MapAreaControllerRoute(
                name: "admin",
                areaName: "Admin",
                pattern: "Admin/{controller=Dashboard}/{action=Index}/{id?}");

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");

            // Seed (await ile)
            using (var scope = app.Services.CreateScope())
            {
                await SeedAsync(scope.ServiceProvider);
            }

            app.Run();
        }

        public static async Task SeedAsync(IServiceProvider sp)
        {
            var um = sp.GetRequiredService<UserManager<AppUser>>();
            var rm = sp.GetRequiredService<RoleManager<AppRole>>();

            const string email = "admin@admin.com";
            const string pass = "REDACTED";

            if (!await rm.RoleExistsAsync("Admin"))
                await rm.CreateAsync(new AppRole { Name = "Admin" });

            var u = await um.FindByEmailAsync(email);
            if (u is null)
            {
                u = new AppUser { UserName = email, Email = email, EmailConfirmed = true };
                var res = await um.CreateAsync(u, pass);
                if (!res.Succeeded)
                    throw new Exception(string.Join(", ", res.Errors.Select(x => x.Description)));
            }
            if (!await um.IsInRoleAsync(u, "Admin"))
                await um.AddToRoleAsync(u, "Admin");

            var settings = sp.GetRequiredService<ISiteSettingService>();
            await settings.EnsureSeedAsync();
        }
    }
}
