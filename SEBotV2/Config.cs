using System.Text.Json.Serialization;

namespace SEBotV2
{
    public class Config
    {
        [JsonPropertyName("discordToken")]
        public string DiscordToken { get; set; } = "YOUR_DISCORD_BOT_TOKEN_HERE";

        [JsonPropertyName("steamApiKey")]
        public string SteamApiKey { get; set; } = "YOUR_STEAM_API_KEY_HERE_OPTIONAL";

        [JsonPropertyName("serverIp")]
        public string ServerIp { get; set; } = "YOUR_SERVER_IP_HERE";

        [JsonPropertyName("serverPort")]
        public int ServerPort { get; set; } = 27016;

        [JsonPropertyName("updateIntervalMs")]
        public int UpdateIntervalMs { get; set; } = 60000;

        [JsonPropertyName("_comments")]
        public ConfigComments Comments { get; set; } = new();

        public bool IsValid()
        {
            return !string.IsNullOrEmpty(DiscordToken) && !DiscordToken.Contains("YOUR_DISCORD_BOT_TOKEN") && !string.IsNullOrEmpty(ServerIp) && !ServerIp.Contains("YOUR_SERVER_IP");
        }
    }
    public class ConfigComments
    {
        [JsonPropertyName("discordToken")]
        public string DiscordToken { get; set; } = "Get your Discord bot token from https://discord.com/developers/applications";

        [JsonPropertyName("steamApiKey")]
        public string SteamApiKey { get; set; } = "Optional: Get from https://steamcommunity.com/dev/apikey - improves server detection";

        [JsonPropertyName("serverIp")]
        public string ServerIp { get; set; } = "Your Space Engineers server IP address";

        [JsonPropertyName("serverPort")]
        public string ServerPort { get; set; } = "Space Engineers server port (usually 27016)";

        [JsonPropertyName("updateIntervalMs")]
        public string UpdateInterval { get; set; } = "How often to check server status in milliseconds (60000 = 1 minute)";
    }
    public class ServerInfo
    {
        public string Name { get; set; } = string.Empty;
        public int Players { get; set; }
        public int MaxPlayers { get; set; }
        public bool IsOnline { get; set; }
    }
}
