using System.Text.Json;

namespace SEBotV2.API.Services
{
    public class ConfigService
    {
        private readonly string _configFile;

        public ConfigService(string configFile)
        {
            _configFile = configFile;
        }

        public async Task<Config> LoadOrCreateConfigAsync()
        {
            if (File.Exists(_configFile))
            {
                try
                {
                    var json = await File.ReadAllTextAsync(_configFile);
                    var config = JsonSerializer.Deserialize<Config>(json, new JsonSerializerOptions
                    {
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                        WriteIndented = true
                    });

                    Console.WriteLine("Configuration loaded successfully");
                    return config ?? new Config();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error loading config: {ex.Message}");
                    Console.WriteLine("Creating new configuration file...");
                }
            }

            var newConfig = new Config();
            await SaveConfigAsync(newConfig);

            Console.WriteLine("Created new configuration file: config.json");
            Console.WriteLine("Please edit the configuration file with your bot token and server details.");

            return newConfig;
        }

        public async Task SaveConfigAsync(Config config)
        {
            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true
            };

            var json = JsonSerializer.Serialize(config, options);
            await File.WriteAllTextAsync(_configFile, json);
        }
    }
}