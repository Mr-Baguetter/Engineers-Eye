using Discord;
using Discord.WebSocket;
using Okolni.Source.Query.Responses;
using static SEBotV2.API.Helpers.ServerManager;

namespace SEBotV2.API.Helpers
{
    public class PlayerManager
    {
        public static async Task Register()
        {
            Bot.OnPlayerJoined += OnPlayerJoined;
            Bot.OnPlayerLeft += OnPlayerLeft;
            Bot.OnPlayerJoinedBatch += OnPlayerJoinedBatch;
            Bot.OnPlayerLeftBatch += OnPlayerLeftBatch;
        }

        private static void OnPlayerJoined(ServerInfo old, ServerInfo info, Player player)
        {
            LogManager.Info("Player Joined");
            if (string.IsNullOrWhiteSpace(player.Name))
                return;

            SendPlayerNotification(info, player.Name, "Join Message", Color.Magenta);
        }

        private static void OnPlayerLeft(ServerInfo old, ServerInfo info, Player player)
        {
            if (string.IsNullOrWhiteSpace(player.Name))
                return;

            SendPlayerNotification(info, player.Name, "Leave Message", Color.Blue);
        }

        private static void OnPlayerJoinedBatch(ServerInfo old, ServerInfo info, List<Player> players)
        {
            LogManager.Info($"{players.Count} Players Joined");
            
            string playerNames = string.Join(", ", players.Select(p => p.Name));
            string message = TranslationManager.Get("Batch Join Message")
                .Replace("{Count}", players.Count.ToString())
                .Replace("{PlayerNames}", playerNames);
            
            SendPlayerNotification(info, message, null, Color.Magenta, isBatch: true);
        }

        private static void OnPlayerLeftBatch(ServerInfo old, ServerInfo info, List<Player> players)
        {
            LogManager.Info($"{players.Count} Players Left");
            
            string playerNames = string.Join(", ", players.Select(p => p.Name));
            string message = TranslationManager.Get("Batch Leave Message")
                .Replace("{Count}", players.Count.ToString())
                .Replace("{PlayerNames}", playerNames);
            
            SendPlayerNotification(info, message, null, Color.Blue, isBatch: true);
        }

        private static void SendPlayerNotification(ServerInfo info, string playerName, string translationKey, Color color, bool isBatch = false)
        {
            Dictionary<ulong, ulong> logChannel = ConfigManager.LoadAsync<Dictionary<ulong, ulong>>("LogChannel").GetAwaiter().GetResult() ?? [];
            Dictionary<ulong, Bot.ServerPingInfo> pingInfo = ConfigManager.LoadAsync<Dictionary<ulong, Bot.ServerPingInfo>>("PingInfo").GetAwaiter().GetResult() ?? [];
            List<ulong> guildId = [];
            foreach (var key in pingInfo.Where(kvp => kvp.Value.IP == info.Ip && kvp.Value.Port == info.Port))
                guildId.Add(key.Key);
            
            foreach (ulong id in guildId)
            {
                SocketGuild guild = Bot.Instance._client.GetGuild(id);
                SocketGuildChannel channel = null;
                if (guild is not null)
                    channel = guild.GetChannel(logChannel[id]);
                
                ContainerBuilder container = new();
                string displayMessage = isBatch ? playerName : TranslationManager.Get(translationKey).Replace("{PlayerName}", playerName);
                container.AddComponent(new TextDisplayBuilder(displayMessage));
                container.AddComponent(new TextDisplayBuilder($"-# {ServerInfoByGuildId[id].Name}"));
                container.AccentColor = color;

                if (channel is not null && channel is IMessageChannel messageChannel)
                    messageChannel.SendMessageAsync(components: new ComponentBuilderV2(container).Build());
            }
        }
    }
}