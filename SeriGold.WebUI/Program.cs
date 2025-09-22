using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.AspNetCore.StaticFiles;
using SeriGold.Core.Hubs;
using SeriGold.Services.Abstracts;
using SeriGold.Services.Concrete;

namespace SeriGold.WebUI
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Services
            builder.Services.AddHttpClient(); // genel fabrika
            builder.Services.AddMemoryCache();

            builder.Services.AddResponseCompression(opts =>
            {
                opts.EnableForHttps = true;
                opts.Providers.Add<GzipCompressionProvider>();
                opts.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(new[]
                {
                    "application/octet-stream"
                });
            });

            builder.Services.AddSignalR(o =>
            {
                o.ClientTimeoutInterval = TimeSpan.FromSeconds(30);
                o.KeepAliveInterval = TimeSpan.FromSeconds(10);
                o.EnableDetailedErrors = false;
            });

            builder.Services.AddControllersWithViews();

            // Providers
            builder.Services.AddHttpClient<HaremAltinProvider>();
            builder.Services.AddHttpClient<TruncgilFxProvider>();
            builder.Services.AddHttpClient<FastForexProvider>();

            builder.Services.AddTransient<IRateProvider, FastForexProvider>();  // saniyelik FX
            builder.Services.AddTransient<IRateProvider, TruncgilFxProvider>(); // serbest piyasa çıpası + yedek
            builder.Services.AddTransient<IRateProvider, HaremAltinProvider>(); // altın + sarrafiye
            builder.Services.AddTransient<IRateProvider, SerbestFxProvider>();
            builder.Services.AddHostedService<RateEngine>();

            builder.Services.AddCors(o => o.AddPolicy("any", p => p
                .AllowAnyHeader().AllowAnyMethod().AllowCredentials().SetIsOriginAllowed(_ => true)));

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
            app.UseAuthorization();

            app.MapHub<RatesHub>("/ratesHub");
            app.MapControllers();

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");

            app.Run();
        }
    }
}
