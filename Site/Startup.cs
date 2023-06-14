using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Logging;
using System.IO;

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
            //services.Configure<Auth0Settings>(Configuration.GetSection("Auth0"));

            services.AddAuthentication(options =>
                {
                    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
                })
                .AddJwtBearer(options =>
                {
                    options.Authority = $"{Configuration["AzureAd:AadInstance"]}{Configuration["AzureAd:Tenant"]}";
                    options.Audience = Configuration["api://home.cannehag.se/api"];
                    options.RequireHttpsMetadata = false;
                    options.TokenValidationParameters.ValidateAudience = false;
                    options.TokenValidationParameters.ValidateIssuer = false;
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
            //loggerFactory.AddConsole(Configuration.GetSection("Logging"));
            //loggerFactory.AddDebug();

            app.UseAuthentication();
            //var settings = app.ApplicationServices.GetService<IOptions<Auth0Settings>>();

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

    //public class Auth0Settings
    //{
    //    public string Domain { get; set; }
    //    public string ClientId { get; set; }
    //}

    public class ParticleConfig
    {
        public string DeviceId { get; set; }
        public string AccessToken { get; set; }
    }
}
