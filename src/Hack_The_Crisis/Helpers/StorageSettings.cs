using System.IO;
using Microsoft.Extensions.Configuration;

namespace Hack_The_Crisis.Helpers
{
    public class StorageSettings
    {
        public string AccountKey { get; set; }
        public string AccountName { get; set; }
    }

    public static class ConfigurationSettings
    {
        public static StorageSettings Settings
        {
            get
            {
                var cfgBuilder = new ConfigurationBuilder()
                    .SetBasePath(Directory.GetCurrentDirectory())
                    .AddJsonFile($"appsettings.json", true, true)
                    .AddJsonFile($"appsettings.Development.json", true, true)
                    .AddJsonFile($"local.settings.json", true, true)
                    .AddEnvironmentVariables();
                var configuration = cfgBuilder.Build();
                var section = configuration.GetSection("storage");
                var storageSettings = new StorageSettings();
                section.Bind(storageSettings);
                return storageSettings;
            }
        }
    }
}