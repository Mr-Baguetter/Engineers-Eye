using Discord;
using Discord.Commands;
using SEBotV2.API.Net;

namespace SEBotV2.Commands.Admin
{
    public class CheckForUpdate : CommandBase
    {
        public override string Name => "CheckForUpdate";
        public override ContextType ContextType => ContextType.Guild;
        public override string Description => "Checks for updates to the Bot";
        public override GuildPermission RequiredPermission => GuildPermission.ManageMessages;
        public override bool ShouldDefer => true;
        public override int RequiredArgsCount => 1;
        public override string VisibleArgs => "%boolean%AllowPrereleases";

        public override async Task<CommandResult> ExecuteAsync(List<string> arguments, ICommandSender sender, CancellationToken ct = default)
        {
            ContainerBuilder container = new();
            container.AddComponent(new TextDisplayBuilder($"## Updater"));
            if (!bool.TryParse(arguments[0], out bool allowPreReleases))
            {
                container.AddComponent(new TextDisplayBuilder($"### Invalid boolean value. Use true or false."));
                return CommandResult.From(false, string.Empty, component: new ComponentBuilderV2(container).Build());
            }

            (Version, string) update = await UpdateManager.CheckForUpdate(Bot.Instance.version, allowPreReleases);
            if (update.Item1 is not null)
            {
                container.AddComponent(new TextDisplayBuilder($"### {update} Update available. \n Run `Update` from the console to update the bot"));
                return CommandResult.From(true, string.Empty, component: new ComponentBuilderV2(container).Build());
            }
            else
            {
                container.AddComponent(new TextDisplayBuilder($"### {update.Item2}"));
                return CommandResult.From(true, string.Empty, component: new ComponentBuilderV2(container).Build());
            }
        }
    }
}