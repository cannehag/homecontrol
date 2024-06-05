using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Logging;
using System.IO;
using System.Security.Claims;
using System.Security.Principal;
using System.Threading.Tasks;

namespace Site
{
    //public class ApiKeyMiddleware
    //{
    //    public ApiKeyMiddleware(RequestDelegate next)
    //    {
    //        _next = next;
    //    }

    //    private const string _apikey = "ApiKey";
    //    private RequestDelegate _next;

    //    public async Task InvokeAsync(HttpContext context)
    //    {
    //        var authHash = context.Request.Query["hash"];

    //        if (!string.IsNullOrEmpty(authHash))
    //        {
    //            if (authHash != "5f91d8a557f42172732707c9a9e77264")
    //            {
    //                context.Response.StatusCode = 401;
    //                await context.Response.WriteAsync("Unauthorized client");
    //                return;
    //            }

    //            var identity = new GenericIdentity("apikey", "system");
    //            identity.AddClaim(new Claim(ClaimTypes.Name, "Jonas"));
    //            identity.AddClaim(new Claim(ClaimTypes.Email, "jonas@cannehag.se"));
    //            context.User = new ClaimsPrincipal(identity);
    //        }

    //        await _next(context);
    //    }
    //}

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
            services.AddTransient<ApiKeyAuthFilter>();
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

            //app.UseMiddleware<ApiKeyMiddleware>();
            app.UseDeveloperExceptionPage();
            //loggerFactory.AddConsole(Configuration.GetSection("Logging"));
            //loggerFactory.AddDebug();

            app.UseAuthentication();
            app.UseAuthorization();
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
