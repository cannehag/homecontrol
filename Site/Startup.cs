using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Site
{
   public class Startup
   {
      public Startup(IHostingEnvironment env)
      {
         var builder = new ConfigurationBuilder()
             .SetBasePath(env.ContentRootPath)
             .AddJsonFile("config/appsettings.json", optional: true, reloadOnChange: true)
             .AddJsonFile("config/auth0-config.json")
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

         var logger = loggerFactory.CreateLogger("Auth0");
         var settings = app.ApplicationServices.GetService<IOptions<Auth0Settings>>();

         var options = new JwtBearerOptions
         {
            RequireHttpsMetadata = false,
            Audience = settings.Value.ClientId,
            Authority = $"https://{settings.Value.Domain}",
            Events = new JwtBearerEvents
            {
               OnAuthenticationFailed = context =>
               {
                  logger.LogError("Authentication failed.", context.Exception);
                  return Task.FromResult(0);
               },

               OnTokenValidated = context =>
               {
                  var claimsIdentity = context.Ticket.Principal.Identity as ClaimsIdentity;
                  claimsIdentity.AddClaim(new Claim("id_token",
                     context.Request.Headers["Authorization"][0].Substring(context.Ticket.AuthenticationScheme.Length +
                                                                           1)));
                  return Task.FromResult(0);
               }
            }
         };

         app.UseJwtBearerAuthentication(options);

         app.UseDefaultFiles(new DefaultFilesOptions { DefaultFileNames = new[] { "index.html" } });
         app.UseStaticFiles();
         app.UseMvcWithDefaultRoute();
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
