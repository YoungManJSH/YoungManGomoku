#if !USE_AWS_MYSQL && !USE_URL_MSSQL && !USE_URL_MSSQL_IIS
#define USE_URL_MSSQL
#endif
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;

namespace YoungManGomoku_WebServer
{
    public class Program
    {
        public static void Main(string[] args)
        {
#if USE_AWS_MYSQL
			// AWS EC2 + Local MySQL
#elif USE_URL_MSSQL
            // Local MSSQL
#elif USE_URL_MSSQL_IIS
            // Local MSSQL with IIS
#else
#error "DB Provider is Not Defined..."
#endif
			CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
#if USE_AWS_MYSQL
                    webBuilder
						.UseKestrel(options =>
						{
							options.ListenAnyIP(5001, listenOptions =>
							{
								listenOptions.UseHttps(
									"/home/ec2-user/gomoku/server.pfx",
									"password"
								);
							});
						})
						.UseStartup<Startup>();
#elif USE_URL_MSSQL
					webBuilder.UseStartup<Startup>()
                        .UseUrls("https://0.0.0.0:5001"); // 모든 IP로부터 5001 포트에 한해 https 접근 허용
                        // UseUrl은 kestrel 직접 설정 시 충돌난다

                    /*
                    UseIISIntegration()은 IIS / IIS Express 환경 전용
                    AWS Linux + Kestrel 단독 실행에서는 의미 없고 가끔 문제를 발생시킨다
                    AWS에 HTTPS 직접 바인딩하고 나서 IISIntegration -> 모호한 동작을 유발하기 때문
                    */
#elif USE_URL_MSSQL_IIS
                    webBuilder.UseIISIntegration();
#endif
				});
    }
}