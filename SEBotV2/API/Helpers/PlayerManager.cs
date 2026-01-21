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
        }

        private static void OnPlayerJoined(ServerInfo old, ServerInfo info, Player player)
        {
            LogManager.Info("Player Joined");
            if (string.IsNullOrWhiteSpace(player.Name))
                return;

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
                container.AddComponent(new TextDisplayBuilder(TranslationManager.Get("Join Message").Replace("{PlayerName}", player.Name)));
                container.AddComponent(new TextDisplayBuilder($"-# {ServerInfoByGuildId[id].Name}"));
                container.AccentColor = Color.Magenta;

                if (channel is not null && channel is IMessageChannel messageChannel)
                    messageChannel.SendMessageAsync(components: new ComponentBuilderV2(container).Build());
            }
        }

        private static void OnPlayerLeft(ServerInfo old, ServerInfo info, Player player)
        {
            if (string.IsNullOrWhiteSpace(player.Name))
                return;

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
                container.AddComponent(new TextDisplayBuilder(TranslationManager.Get("Leave Message").Replace("{PlayerName}", player.Name)));
                container.AddComponent(new TextDisplayBuilder($"-# {ServerInfoByGuildId[id].Name}"));
                container.AccentColor = Color.Blue;

                if (channel is not null && channel is IMessageChannel messageChannel)
                    messageChannel.SendMessageAsync(components: new ComponentBuilderV2(container).Build());
            }
        }
    }
}