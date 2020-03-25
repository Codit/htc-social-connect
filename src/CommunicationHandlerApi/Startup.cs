using System;
using System.IO;
using System.Linq;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Extensions.DependencyInjection;
using Swashbuckle.AspNetCore.Swagger;
using Arcus.Security.Secrets.Core.Caching;
using Arcus.Security.Secrets.Core.Interfaces;
using Arcus.WebApi.Security.Authentication.SharedAccessKey;
using CommunicationHandlerApi.Interfaces;
using CommunicationHandlerApi.Models;
using CommunicationHandlerApi.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.OpenApi.Models;

namespace CommunicationHandlerApi
{
    public class Startup
    {
        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            var cfgBuilder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile($"appsettings.json", true, true)
                .AddJsonFile($"appsettings.dev.json", true, true)
                .AddJsonFile($"local.settings.json", true, true)
                .AddEnvironmentVariables();
            var configuration = cfgBuilder.Build();
            
            //#warning Please provide a valid secret provider, for example Azure Key Vault: https://security.arcus-azure.net/features/secrets/consume-from-key-vault
            services.AddScoped<ICachedSecretProvider>(serviceProvider => new CachedSecretProvider(new SharedSecretProvider()));
            services.AddMvc(options => 
            {
                options.ReturnHttpNotAcceptable = true;
                options.RespectBrowserAcceptHeader = true;
                //TODO : added after upgrade 3
                options.EnableEndpointRouting = false;
                RestrictToJsonContentType(options);

                options.Filters.Add(new SharedAccessKeyAuthenticationFilter("x-api-key", "x-api-key", "whatsapp-key"));
            });
            services.AddSingleton<IWhatsappHandlerService, BlobWhatsappHandlerService>();
            services.AddSingleton<IUserMatcher, TwilioUserMatcher>();

            services.AddHealthChecks();

            services.AddOptions();
            services.Configure<StorageSettings>(options => configuration.GetSection("storage").Bind(options));

            
#if DEBUG
            var openApiInformation = new OpenApiInfo
            {
                Title = "CommunicationHandlerApi",
                Version = "v1"
            };

            services.AddSwaggerGen(swaggerGenerationOptions =>
            {
                swaggerGenerationOptions.SwaggerDoc("v1", openApiInformation);
                swaggerGenerationOptions.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, "CommunicationHandlerApi.Open-Api.xml"));
            });
#endif
        }

        private static void RestrictToJsonContentType(MvcOptions options)
        {
            // TODO : changed jsoninput type class
            var allButJsonInputFormatters = options.InputFormatters.Where(formatter => !(formatter is SystemTextJsonInputFormatter));
            foreach (IInputFormatter inputFormatter in allButJsonInputFormatters)
            {
                options.InputFormatters.Remove(inputFormatter);
            }
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            app.UseMiddleware<Arcus.WebApi.Logging.ExceptionHandlingMiddleware>();

            #warning Please configure application with HTTPS transport layer security

            app.UseMvc();

#if DEBUG
            app.UseSwagger();
            app.UseSwaggerUI(swaggerUiOptions =>
            {
                swaggerUiOptions.SwaggerEndpoint("v1/swagger.json", "CommunicationHandlerApi");
                swaggerUiOptions.DocumentTitle = "CommunicationHandlerApi";
            });
#endif
        }
    }
}
