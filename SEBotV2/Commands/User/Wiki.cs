using Discord;
using Discord.Commands;
using SEBotV2.API.Helpers;

namespace SEBotV2.Commands.User
{
    public class Wiki : CommandBase
    {
        public override string Name => "Wiki";
        public override ContextType ContextType => ContextType.Guild;
        public override string Description => "Sends the Space Engineers Wiki.gg page";
        public override GuildPermission RequiredPermission => GuildPermission.SendMessages;
        public override bool ShouldDefer => true;

        public override async Task<CommandResult> ExecuteAsync(List<string> arguments, ICommandSender sender, CancellationToken ct = default)
        {
            ContainerBuilder container = new();
            container.AddComponent(new TextDisplayBuilder(TranslationManager.Get("Wiki Message").Replace("{WikiUrl}", "https://spaceengineers.wiki.gg/")));
            return CommandResult.From(true, string.Empty, component: new ComponentBuilderV2(container).Build());
        }
    }
}