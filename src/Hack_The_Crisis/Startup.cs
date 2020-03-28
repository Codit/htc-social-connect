using System;
using Arcus.Security.Core;
using Arcus.Security.Core.Caching;
using Arcus.Security.Providers.AzureKeyVault;
using Arcus.Security.Providers.AzureKeyVault.Authentication;
using Arcus.Security.Providers.AzureKeyVault.Configuration;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Hack_The_Crisis
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
            var secretProvider = CreateSecretProvider().WithCaching(TimeSpan.FromMinutes(1));
            services.AddSingleton<ISecretProvider>(secretProvider);
            services.AddSingleton<ICachedSecretProvider>(secretProvider);

            services.AddRazorPages();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseRouting();
            app.UseAuthorization();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapRazorPages();
            });
        }

        private ISecretProvider CreateSecretProvider()
        {
            var keyVaultEndpoint = Configuration["KEYVAULT_ENDPOINT"];

#if RELEASE
            var vaultAuthentication = new ManagedServiceIdentityAuthentication();
#elif DEBUG
            var clientId = Configuration["KEYVAULT_AUTH_ID"];
            var clientSecret = Configuration["KEYVAULT_AUTH_SECRET"];
            var vaultAuthentication = new ServicePrincipalAuthentication(clientId, clientSecret);
#endif
            var vaultConfiguration = new KeyVaultConfiguration(keyVaultEndpoint);
            var keyVaultSecretProvider = new KeyVaultSecretProvider(vaultAuthentication, vaultConfiguration);
            return keyVaultSecretProvider;
        }
    }
}