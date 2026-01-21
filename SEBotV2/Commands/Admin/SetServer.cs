using Discord;
using Discord.Commands;
using SEBotV2.API.Helpers;

namespace SEBotV2.Commands.Admin
{
    public class SetServer : CommandBase
    {
        public override string Name => "SetServer";
        public override ContextType ContextType => ContextType.Guild;
        public override string Description => "Sets the Space Engineers server to ping in this Guild";
        public override bool DeferEphemeral => true;
        public override GuildPermission RequiredPermission => GuildPermission.ManageMessages;
        public override bool ShouldDefer => true;
        public override int RequiredArgsCount => 2;
        public override string VisibleArgs => "ServerIP, ServerPort";

        public override async Task<CommandResult> ExecuteAsync(List<string> arguments, ICommandSender sender, CancellationToken ct = default)
        {
            string ip = arguments[0];
            int port = int.Parse(arguments[1]);

            Bot.ServerPingInfo info = new()
            {
                IP = ip,
                Port = port
            };

            Dictionary<ulong, Bot.ServerPingInfo> kvp = await ConfigManager.LoadAsync<Dictionary<ulong, Bot.ServerPingInfo>>("PingInfo") ?? [];
            kvp[sender.GuildUser.Guild.Id] = info;
            await ConfigManager.SaveAsync<Dictionary<ulong, Bot.ServerPingInfo>>("PingInfo", kvp);

            ContainerBuilder container = new();
            container.AddComponent(new TextDisplayBuilder($"Set Server IP for {sender.GuildUser.Guild.Id} to {ip}"));
            container.AddComponent(new TextDisplayBuilder($"Set Server Port for {sender.GuildUser.Guild.Id} to {port}"));

            return CommandResult.From(true, string.Empty, component: new ComponentBuilderV2(container).Build());
        }
    }
}