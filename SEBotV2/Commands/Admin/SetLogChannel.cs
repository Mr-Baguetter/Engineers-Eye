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
        public override List<Option> Options => 
        [
            new Option
            {
                Name = "Channel",
                Description = "The channel to logs join and leaves to",
                Required = true,
                Type = ApplicationCommandOptionType.Channel
            }
        ];

        public override async Task<Response> ExecuteAsync(List<string> arguments, ICommandSender sender, Dictionary<string, string> optionValues, CancellationToken ct = default)
        {
            if (!optionValues.TryGetValue("channel", out var channelval))
                return Response.Failed("Failed to get Channel value");

            SocketGuildChannel? channel = sender.GuildUser.Guild.Channels.FirstOrDefault(c => c.Name.Equals(channelval, StringComparison.InvariantCultureIgnoreCase));
            if (channel is null)
                return Response.Failed($"Failed to find channel with name {channelval}");

            Dictionary<ulong, ulong> kvp = await ConfigManager.LoadAsync<Dictionary<ulong, ulong>>("LogChannel") ?? [];
            
            ContainerBuilder container = new();
            if (channel is not null)
            {
                kvp[sender.GuildUser.Guild.Id] = channel.Id;
                await ConfigManager.SaveAsync<Dictionary<ulong, ulong>>("LogChannel", kvp);
                container.AddComponent(new TextDisplayBuilder($"Set Log Channel to {channel.Name}"));
                return Response.Succeed(new ComponentBuilderV2(container).Build());
            }
            else
            {
                container.AddComponent(new TextDisplayBuilder($"Failed to set Log Channel Does it exist?"));
                return Response.Succeed(new ComponentBuilderV2(container).Build());
            }
        }
    }
}