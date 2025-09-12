// WebUI/Program.cs
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

            builder.Services.AddHttpClient();
            builder.Services.AddMemoryCache();
            builder.Services.AddSignalR();
            builder.Services.AddControllersWithViews();

            // Tipli HttpClient’lar
            builder.Services.AddHttpClient<MetalsAnyProvider>(c =>
            {
                c.Timeout = TimeSpan.FromSeconds(8);
                c.DefaultRequestHeaders.UserAgent.ParseAdd("SeriGold/1.0 (+serigold)");
            });
            builder.Services.AddHttpClient<ErApiFxProvider>(c =>
            {
                c.Timeout = TimeSpan.FromSeconds(8);
                c.DefaultRequestHeaders.UserAgent.ParseAdd("SeriGold/1.0 (+serigold)");
            });
            builder.Services.AddHttpClient<BinanceFxProvider>(c =>
            {
                c.Timeout = TimeSpan.FromSeconds(8);
                c.DefaultRequestHeaders.UserAgent.ParseAdd("SeriGold/1.0 (+serigold)");
            });

            // IRateProvider kayıtları
            builder.Services.AddSingleton<IRateProvider>(sp => sp.GetRequiredService<MetalsAnyProvider>());
            builder.Services.AddSingleton<IRateProvider>(sp => sp.GetRequiredService<BinanceFxProvider>()); // canlı USD/TRY
            builder.Services.AddSingleton<IRateProvider>(sp => sp.GetRequiredService<ErApiFxProvider>());

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
            app.UseStaticFiles();
            app.UseRouting();
            app.UseCors("any");
            app.UseAuthorization();

            app.MapHub<RatesHub>("/ratesHub");
            app.MapControllerRoute(name: "default", pattern: "{controller=Home}/{action=Index}/{id?}");

            app.Run();
        }
    }
}
