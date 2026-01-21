using Discord;
using Discord.Commands;
using Okolni.Source.Query.Responses;
using SEBotV2.API.Helpers;
using static SEBotV2.API.Helpers.ServerManager;

namespace SEBotV2.Commands.User
{
    public class PlayerList : CommandBase
    {
        public override string Name => "PlayerList";
        public override ContextType ContextType => ContextType.Guild;
        public override string Description => "Gets the Space Engineers server player list for this Guild";
        public override GuildPermission RequiredPermission => GuildPermission.SendMessages;
        public override bool ShouldDefer => true;

        public override async Task<CommandResult> ExecuteAsync(List<string> arguments, ICommandSender sender, CancellationToken ct = default)
        {
            ServerInfo? serverInfo = await QueryServer(sender.GuildUser.Guild.Id);
            ContainerBuilder container = new();

            if (serverInfo is null || string.IsNullOrEmpty(serverInfo.Name))
            {
                container.AddComponent(new TextDisplayBuilder("### Exception"));
                container.AddComponent(new TextDisplayBuilder("Failed to get server info. Failed to query!"));
                container.AccentColor = Color.Red;

                return CommandResult.From(false, string.Empty, component: new ComponentBuilderV2(container).Build());
            }

            List<string> playerLines = [];
            foreach (Player player in serverInfo.Players)
            {
                DateTimeOffset joinTime = DateTimeOffset.UtcNow - player.Duration;
                playerLines.Add($"{player.Name} - <t:{joinTime.ToUnixTimeSeconds()}:R>");
            }

            container.AddComponent(new TextDisplayBuilder(TranslationManager.Get("Player List Current").Replace("{PlayerCount}", $"{serverInfo.PlayerCount}").Replace("{MaxPlayers}", $"{serverInfo.MaxPlayers}")));
            container.AddComponent(new TextDisplayBuilder(string.Join("\n", playerLines)));
            container.AddComponent(new TextDisplayBuilder($"-# {ServerInfoByGuildId[sender.GuildUser.Guild.Id].Name}"));
            container.AccentColor = Color.Green;

            return CommandResult.From(true, string.Empty, component: new ComponentBuilderV2(container).Build());
        }
    }
}