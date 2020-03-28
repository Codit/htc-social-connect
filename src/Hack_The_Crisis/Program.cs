using Arcus.Security.Providers.AzureKeyVault;
using Arcus.Security.Providers.AzureKeyVault.Authentication;
using Arcus.Security.Providers.AzureKeyVault.Configuration;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;

namespace Hack_The_Crisis
{
    public class Program
    {
        public static void Main(string[] args)
        {
            BuildWebHost(args).Run();
        }

        public static IWebHost BuildWebHost(string[] args) =>
           WebHost.CreateDefaultBuilder(args)
               .ConfigureAppConfiguration((ctx, builder) =>
                   {
                       builder.AddEnvironmentVariables();

                       var configuration = builder.Build();
                       var keyVaultEndpoint = configuration["KEYVAULT_ENDPOINT"];

                       if (!string.IsNullOrEmpty(keyVaultEndpoint))
                       {
                           var keyVaultSecretProvider = CreateSecretProvider(configuration, keyVaultEndpoint);
                           builder.AddAzureKeyVault(keyVaultSecretProvider);
                       }
                   }
            ).UseStartup<Startup>()
             .Build();

        private static KeyVaultSecretProvider CreateSecretProvider(IConfigurationRoot configuration,
            string keyVaultEndpoint)
        {
#if RELEASE
            var vaultAuthentication = new ManagedServiceIdentityAuthentication();
#elif DEBUG
            var clientId = configuration["KEYVAULT_AUTH_ID"];
            var clientSecret = configuration["KEYVAULT_AUTH_SECRET"];
            var vaultAuthentication = new ServicePrincipalAuthentication(clientId, clientSecret);
#endif
            var vaultConfiguration = new KeyVaultConfiguration(keyVaultEndpoint);
            var keyVaultSecretProvider = new KeyVaultSecretProvider(vaultAuthentication, vaultConfiguration);
            return keyVaultSecretProvider;
        }
    }
}
