using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Logging;
using System.Collections.Generic;
using System.IO;
using System.Security.Claims;
using System.Security.Principal;
using System.Threading.Tasks;

namespace Site
{

    public class Startup
    {
        public Startup(IHostEnvironment env)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("config/appsettings.json", optional: true, reloadOnChange: true)
                .AddJsonFile("config/secrets.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables();
            Configuration = builder.Build();
        }

        public IConfigurationRoot Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            IdentityModelEventSource.ShowPII = true;

            services.AddOptions();

            services.AddAuthentication(QueryStringAuthDefaults.SchemaName)
                .AddJwtBearer(options =>
                {
                    options.Authority = $"{Configuration["AzureAd:AadInstance"]}{Configuration["AzureAd:Tenant"]}";
                    options.Audience = Configuration["api://home.cannehag.se/api"];
                    options.RequireHttpsMetadata = false;
                    options.TokenValidationParameters.ValidateAudience = false;
                    options.TokenValidationParameters.ValidateIssuer = false;
                })
                .AddScheme<QueryStringAuthOptions, QueryStringAuth>(QueryStringAuthDefaults.SchemaName, opt =>
                {
                    opt.QueryStringKey = "hash";
                    opt.AddDevice("Jonas iPhone");
                });


            services.Configure<ParticleConfig>(p =>
            {
                p.DeviceId = Configuration.GetValue<string>("particle-device-id");
                p.AccessToken = Configuration.GetValue<string>("particle-access-token");
            });
            services.AddMvc(options =>
            {
                options.EnableEndpointRouting = false;
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostEnvironment env, ILoggerFactory loggerFactory)
        {
            app.UseDeveloperExceptionPage();

            app.UseAuthentication();

            app.Use(async (context, next) =>
            {
                await next();

                if (context.Response.StatusCode == 404 &&
                    !Path.HasExtension(context.Request.Path.Value) &&
                    !context.Request.Path.Value.StartsWith("/api/"))
                {
                    context.Request.Path = "/index.html";
                    await next();
                }
            });

            app.UseMvcWithDefaultRoute();

            app.UseDefaultFiles(new DefaultFilesOptions { DefaultFileNames = new[] { "index.html" } });
            app.UseStaticFiles();

        }
    }

    public class ParticleConfig
    {
        public string DeviceId { get; set; }
        public string AccessToken { get; set; }
    }
}
