#if !USE_AWS_MYSQL && !USE_URL_MSSQL && !USE_URL_MSSQL_IIS
#define USE_URL_MSSQL
#endif


//using System.Collections.Generic;
//using System.Linq;
//using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore; // Use Sql Server
//using Microsoft.AspNetCore.HttpsPolicy;
//using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Pomelo.EntityFrameworkCore.MySql.Storage;
using System;
using YoungManGomoku_WebServer.Data;
using YoungManGomoku_WebServer.SingletoneManager;
using YoungManGomoku_WebServer.SingletoneManager.Interface;



namespace YoungManGomoku_WebServer
{
    public class Startup
    {
        public Startup(IConfiguration configuration, IWebHostEnvironment env)
        {
            Configuration = configuration;
            Environment = env;
		}

        public IConfiguration Configuration { get; }
		public IWebHostEnvironment Environment { get; }

		// This method gets called by the runtime. Use this method to add services to the container.
		public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers();
            
            var connectionString = Environment.IsDevelopment() ?
                Configuration.GetConnectionString("LocalMySql")
                : Configuration.GetConnectionString("AwsMySqlEC2");
            services.AddDbContext<ApplicationDBContext>(options =>
#if USE_AWS_MYSQL
	            options.UseMySql(
					connectionString,
					mysqlOptions =>
					{
						mysqlOptions.ServerVersion(ServerVersion.AutoDetect(connectionString));
					}
				)
#elif USE_URL_MSSQL
				options.UseSqlServer(Configuration.GetConnectionString("LocalMsSql"))			
#else		
				options.UseSqlServer(Configuration.GetConnectionString("DefaultConnection"))
#endif
            );
			services.AddSingleton<ServerManager>();

            // IUIDProvider를 요청하면 이미 만든 ServerManager를 써라
            services.AddSingleton<IUIDProvider>(serviceProvider =>
                serviceProvider.GetRequiredService<ServerManager>());
            services.AddSingleton<IServerContext>(serverContext =>
                serverContext.GetRequiredService<ServerManager>());

            services.AddSingleton<MatchingManager>();
            services.AddSingleton<GameRoomManager>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, ILogger<Startup> logger)
        {
#if USE_AWS_MYSQL
	        logger.LogInformation($"[{DateTime.Now}] AWS with MySQL Server Start!");
#elif USE_URL_MSSQL
            logger.LogInformation($"[{DateTime.Now}] URL with MSSQL Server Start!");
#elif USE_URL_MSSQL_IIS
            logger.LogInformation($"[{DateTime.Now}] URL with MSSQL and IIS Server Start!");
#else
            logger.LogInformation($"[{DateTime.Now}] Unknown Type Server Start...");
#endif
			if (env.IsDevelopment())
            {
				logger.LogInformation($"[{DateTime.Now}] Env : Local PC in Visual Studio.");
				app.UseDeveloperExceptionPage();
            }
			else
			{
				logger.LogInformation($"[{DateTime.Now}] Env : Build Instance.");
			}


			app.UseHttpsRedirection();

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
