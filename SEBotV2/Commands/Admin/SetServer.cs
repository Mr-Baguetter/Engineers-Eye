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
        public override List<Option> Options => 
        [
            new Option
            {
                Name = "IP",
                Description = "The Ip address of the server",
                Required = true,
                Type = ApplicationCommandOptionType.String
            },
            new Option
            {
                Name = "Port",
                Description = "The port of the server",
                Required = true,
                Type = ApplicationCommandOptionType.Integer
            },
        ];

        public override async Task<Response> ExecuteAsync(List<string> arguments, ICommandSender sender, Dictionary<string, string> optionValues, CancellationToken ct = default)
        {
            if (!optionValues.TryGetValue("ip", out var ip) || !optionValues.TryGetValue("port", out var portval))
                return Response.Failed("Failed to get Ip or Port values");

            int port = int.Parse(portval);

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

            return Response.Succeed(new ComponentBuilderV2(container).Build());
        }
    }
}