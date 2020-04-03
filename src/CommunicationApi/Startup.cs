using System;
using System.IO;
using System.Linq;
using System.Text.Json.Serialization;
using Arcus.Security.Core;
using Arcus.Security.Core.Caching;
using Arcus.Security.Providers.AzureKeyVault.Authentication;
using Arcus.Security.Providers.AzureKeyVault.Configuration;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Serilog.Events;
using Microsoft.OpenApi.Models;
using Arcus.WebApi.Security.Authentication.SharedAccessKey;
using Arcus.WebApi.Correlation;
using AutoMapper;
using CommunicationApi.Interfaces;
using CommunicationApi.Services;
using CommunicationApi.Services.Blobstorage;
using CommunicationApi.Services.Tablestorage;
using Serilog.Configuration;
using IUserMatcher = CommunicationApi.Interfaces.IUserMatcher;
using IWhatsappHandlerService = CommunicationApi.Interfaces.IWhatsappHandlerService;
using TwilioUserMatcher = CommunicationApi.Services.TwilioUserMatcher;

namespace CommunicationApi
{
    public class Startup
    {
        private const string ApplicationInsightsInstrumentationKeyName = "Telemetry:ApplicationInsights:InstrumentationKey";

        /// <summary>
        /// Initializes a new instance of the <see cref="Startup"/> class.
        /// </summary>
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        /// <summary>
        /// Gets the configuration of key/value application properties.
        /// </summary>
        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            var secretProvider = CreateSecretProvider().WithCaching(TimeSpan.FromMinutes(1));
            services.AddSingleton<ISecretProvider>(secretProvider);
            services.AddControllers(options => 
            {
                options.ReturnHttpNotAcceptable = true;
                options.RespectBrowserAcceptHeader = true;

                RestrictToJsonContentType(options);
                AddEnumAsStringRepresentation(options);

                options.Filters.Add(new SharedAccessKeyAuthenticationFilter("x-api-key", "x-api-key", "HTC-API-Key"));
            });

            services.AddOptions();

            services.AddAutoMapper(typeof(Startup));
            services.AddSingleton<IBoxStore, TableStorageBoxStore>();
            services.AddSingleton<IWhatsappHandlerService, WhatsappHandlerService>();
            services.AddSingleton<IUserStore, TableUserStorage>();
            services.AddSingleton<IUserMatcher, TwilioUserMatcher>();
            services.AddSingleton<IMediaServiceProvider, TableTextMessageServiceProvider>();
            services.AddSingleton<IMediaServiceProvider, BlobImageMediaServiceProvider>();
            services.AddSingleton<IMessageTranslater, DefaultMessageTranslater>();
            services.AddSingleton<IMessageAuditStore, TableMessageAuditStore>();
            services.AddHealthChecks();
            
            services.AddHttpCorrelation();

            var openApiInformation = new OpenApiInfo
            {
                Title = "Hack The Crisis - Communication API",
                Version = "v1"
            };

            services.AddSwaggerGen(swaggerGenerationOptions =>
            {
                swaggerGenerationOptions.SwaggerDoc("v1", openApiInformation);
                swaggerGenerationOptions.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, "CommunicationApi.Open-Api.xml"));
            });
        }

        private static void RestrictToJsonContentType(MvcOptions options)
        {
            var allButJsonInputFormatters = options.InputFormatters.Where(formatter => !(formatter is SystemTextJsonInputFormatter));
            foreach (IInputFormatter inputFormatter in allButJsonInputFormatters)
            {
                options.InputFormatters.Remove(inputFormatter);
            }

            // Removing for text/plain, see https://docs.microsoft.com/en-us/aspnet/core/web-api/advanced/formatting?view=aspnetcore-3.0#special-case-formatters
            options.OutputFormatters.RemoveType<StringOutputFormatter>();
        }

        private static void AddEnumAsStringRepresentation(MvcOptions options)
        {
            var onlyJsonOutputFormatters = options.OutputFormatters.OfType<SystemTextJsonOutputFormatter>();
            foreach (SystemTextJsonOutputFormatter outputFormatter in onlyJsonOutputFormatters)
            {
                outputFormatter.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
            }
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseMiddleware<Arcus.WebApi.Logging.ExceptionHandlingMiddleware>();
            app.UseCorrelation();
            app.UseRouting();

            app.UseSerilogRequestLogging();
            
            app.UseSwagger(swaggerOptions =>
            {
                swaggerOptions.RouteTemplate = "api/{documentName}/docs.json";
            });
            app.UseSwaggerUI(swaggerUiOptions =>
            {
                swaggerUiOptions.SwaggerEndpoint("/api/v1/docs.json", "CommunicationApi");
                swaggerUiOptions.RoutePrefix = "api/docs";
                swaggerUiOptions.DocumentTitle = "CommunicationApi";
            });
            app.UseEndpoints(endpoints => endpoints.MapControllers());

            Log.Logger = CreateLoggerConfiguration().CreateLogger();
        }

        private LoggerConfiguration CreateLoggerConfiguration()
        {
            var instrumentationKey = Configuration.GetValue<string>(ApplicationInsightsInstrumentationKeyName);
            return new LoggerConfiguration()
                .MinimumLevel.Debug()    
                .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
                .Enrich.FromLogContext()
                .Enrich.WithVersion()
                .Enrich.WithComponentName("API")
                .Enrich.WithCorrelationInfo()
                .WriteTo.Console()
                .WriteTo.AzureApplicationInsights(instrumentationKey);
        }

        private ISecretProvider CreateSecretProvider()
        {
            var keyVaultEndpoint = Configuration["KeyVaultUri"];

#if RELEASE
            var vaultAuthentication = new ManagedServiceIdentityAuthentication();
#elif DEBUG
            var clientId = Configuration["KEYVAULT_AUTH_ID"];
            var clientSecret = Configuration["KEYVAULT_AUTH_SECRET"];
            var vaultAuthentication = new ServicePrincipalAuthentication(clientId, clientSecret);
#endif
            var vaultConfiguration = new KeyVaultConfiguration(keyVaultEndpoint);
            var keyVaultSecretProvider = new Arcus.Security.Providers.AzureKeyVault.KeyVaultSecretProvider(vaultAuthentication, vaultConfiguration);
            return keyVaultSecretProvider;
        }
    }
}
