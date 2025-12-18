using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
//using Microsoft.AspNetCore.HttpsPolicy;
//using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using Microsoft.EntityFrameworkCore; // Use Sql Server
using YoungManGomoku_WebServer.Data;
using YoungManGomoku_WebServer.SingletoneManager;

namespace YoungManGomoku_WebServer
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration; 
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers();

            services.AddDbContext<ApplicationDBContext>(options => 
                options.UseSqlServer(
                    Configuration.GetConnectionString("DefaultConnection")));

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
            logger.LogInformation($"[{DateTime.UtcNow}] Server Start!");
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
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
