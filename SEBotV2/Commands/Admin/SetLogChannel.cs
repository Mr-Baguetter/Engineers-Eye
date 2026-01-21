using Discord;
using Discord.Commands;
using Discord.WebSocket;
using SEBotV2.API.Helpers;

namespace SEBotV2.Commands.Admin
{
    public class SetLogChannel : CommandBase
    {
        public override string Name => "SetLogChannel";
        public override ContextType ContextType => ContextType.Guild;
        public override string Description => "Sets the Log Channel for this Guild";
        public override bool DeferEphemeral => true;
        public override GuildPermission RequiredPermission => GuildPermission.ManageMessages;
        public override bool ShouldDefer => true;
        public override int RequiredArgsCount => 1;
        public override string VisibleArgs => "LogChannelId";

        public override async Task<CommandResult> ExecuteAsync(List<string> arguments, ICommandSender sender, CancellationToken ct = default)
        {
            ulong id = ulong.Parse(arguments[0]);
            Dictionary<ulong, ulong> kvp = await ConfigManager.LoadAsync<Dictionary<ulong, ulong>>("LogChannel") ?? [];
                
            SocketTextChannel channel = sender.GuildUser.Guild.GetTextChannel(id);
            ContainerBuilder container = new();
            if (channel is not null)
            {
                kvp[sender.GuildUser.Guild.Id] = id;
                await ConfigManager.SaveAsync<Dictionary<ulong, ulong>>("LogChannel", kvp);
                container.AddComponent(new TextDisplayBuilder($"Set Log Channel to {channel.Name}"));
                return CommandResult.From(true, string.Empty, component: new ComponentBuilderV2(container).Build());
            }
            else
            {
                container.AddComponent(new TextDisplayBuilder($"Failed to set Log Channel to {channel.Name} Does it exist?"));
                return CommandResult.From(true, string.Empty, component: new ComponentBuilderV2(container).Build());
            }
        }
    }
}