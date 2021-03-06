﻿using System.IO;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Site
{
    public class Startup
    {
        public Startup(IHostingEnvironment env)
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
            services.AddOptions();
            services.Configure<Auth0Settings>(Configuration.GetSection("Auth0"));

            services.AddAuthentication(options =>
                {
                    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
                })

                .AddJwtBearer(options =>
                {
                    options.Authority = $"{Configuration["AzureAd:AadInstance"]}{Configuration["AzureAd:Tenant"]}";
                    options.Audience = Configuration["AzureAd:ClientId"];
                    options.RequireHttpsMetadata = false;
                });


            services.Configure<ParticleConfig>(p =>
            {
                p.DeviceId = Configuration.GetValue<string>("particle-device-id");
                p.AccessToken = Configuration.GetValue<string>("particle-access-token");
            });
            services.AddMvc();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            app.UseDeveloperExceptionPage();

            loggerFactory.AddConsole(Configuration.GetSection("Logging"));
            loggerFactory.AddDebug();

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

    public class Auth0Settings
    {
        public string Domain { get; set; }
        public string ClientId { get; set; }
    }

    public class ParticleConfig
    {
        public string DeviceId { get; set; }
        public string AccessToken { get; set; }
    }
}
