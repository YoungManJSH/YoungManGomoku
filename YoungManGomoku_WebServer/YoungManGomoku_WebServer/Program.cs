using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;

namespace YoungManGomoku_WebServer
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>()
                        .UseUrls("https://0.0.0.0:5001"); // 모든 IP로부터 5001 포트에 한해 https 접근 허용
                    webBuilder.UseIISIntegration();
                });
    }
}
