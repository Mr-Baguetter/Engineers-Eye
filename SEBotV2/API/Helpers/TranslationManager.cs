using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace SEBotV2.API.Helpers
{
    public class Translation
    {
        private Dictionary<string, string> _translations = [];

        public Translation(Dictionary<string, string> translations)
        {
            _translations = translations ?? [];
        }

        public string GetTranslation(string key)
        {
            if (_translations.TryGetValue(key, out string value))
                return value;

            LogManager.Warn($"Translation key not found: {key}");
            return $"[MISSING: {key}]";
        }

        public bool HasTranslation(string key) =>
            _translations.ContainsKey(key);

        public Dictionary<string, string> GetAll() =>
            _translations;
    }

    public class TranslationManager
    {
        private static TranslationManager _instance;
        private Translation _translation = new([]);
        private string _translationsPath;
        private readonly string _translationFileName = "translations.yaml";

        public static TranslationManager Instance
        {
            get
            {
                _instance ??= new TranslationManager();
                return _instance;
            }
        }

        private TranslationManager()
        {
            _translationsPath = Path.Combine(Directory.GetCurrentDirectory(), "Translations");
        }

        public static void Init()
        {
            Instance.LoadTranslation();
        }

        public static void Init(string customPath)
        {
            Instance._translationsPath = customPath;
            Instance.LoadTranslation();
        }

        private void LoadTranslation()
        {
            try
            {
                if (!Directory.Exists(_translationsPath))
                {
                    LogManager.Warn($"Translations directory not found: {_translationsPath}");
                    Directory.CreateDirectory(_translationsPath);
                    CreateExampleTranslation();
                    return;
                }

                string filePath = Path.Combine(_translationsPath, _translationFileName);

                if (!File.Exists(filePath))
                {
                    LogManager.Info("No translation file found, creating example");
                    CreateExampleTranslation();
                    return;
                }

                string yamlContent = File.ReadAllText(filePath);
                IDeserializer deserializer = new DeserializerBuilder().WithNamingConvention(CamelCaseNamingConvention.Instance).Build();
                Dictionary<string, string> translations = deserializer.Deserialize<Dictionary<string, string>>(yamlContent) ?? [];

                _translation = new Translation(translations);
                LogManager.Info($"Loaded translation file: {filePath}");
            }
            catch (Exception ex)
            {
                LogManager.Error($"Failed to load translation file: {ex.Message}");
            }
        }

        private static void CreateExampleTranslation()
        {
            Dictionary<string, string> exampleTranslations = new()
            {
                { "Join Message", "## Player Joined\n### {PlayerName} Joined the server" },
                { "Leave Message", "## Player Left\n### {PlayerName} Left the server" },
                { "Player Leave Notification Leave Message", "A player has left the server: {ServerName}" },
                { "Player Leave Notification Automatic Removal", "You were automaticly removed from the Player Leave Notification pool" },
                { "Player Leave Notification Header", "## Player Leave Notification" },
                { "Player Leave Notification Stop", "Stopped Player Leave Notifications" },
                { "Player Leave Notification Update", "Updated existing monitor to {ServerName}." },
                { "Player Leave Notification Start", "Monitoring {ServerName} for player leaves. \n Run this command again in the same Guild to stop" },
                { "Player List Current", "## Current Players\n### Player Count: {PlayerCount}/{MaxPlayers}" },
                { "Wiki Message", "## Wiki\n### {WikiUrl}" },
                { "Batch Join Message", "{Count} players joined: \n {PlayerNames}"},
                { "Batch Leave Message", "{Count} players left: \n {PlayerNames}"}
            };

            SaveTranslations(exampleTranslations);
        }

        public static void SaveTranslations(Dictionary<string, string> translations)
        {
            try
            {
                string filePath = Path.Combine(Instance._translationsPath, Instance._translationFileName);
                ISerializer serializer = new SerializerBuilder().WithNamingConvention(CamelCaseNamingConvention.Instance).WithDefaultScalarStyle(YamlDotNet.Core.ScalarStyle.DoubleQuoted).Build();

                string yaml = serializer.Serialize(translations);
                File.WriteAllText(filePath, yaml);

                Instance._translation = new Translation(translations);
                LogManager.Info($"Saved translation file: {filePath}");
            }
            catch (Exception ex)
            {
                LogManager.Error($"Failed to save translation file: {ex.Message}");
            }
        }

        public static string Get(string key)
        {
            if (Instance._translation == null)
            {
                LogManager.Error("Translations not loaded.");
                return $"[ERROR: {key}]";
            }

            return Instance._translation.GetTranslation(key);
        }

        public static bool HasKey(string key)
        {
            if (Instance._translation == null)
                return false;

            return Instance._translation.HasTranslation(key);
        }

        public static Dictionary<string, string> GetAllTranslations() =>
            Instance._translation?.GetAll() ?? [];

        public static void ReloadTranslations() =>
            Instance.LoadTranslation();
    }
}
