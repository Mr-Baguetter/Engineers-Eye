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
        public override List<Option> Options => 
        [
            new Option
            {
                Name = "AllowPrereleases",
                Description = "Allow the downloading of prereleases",
                Required = true,
                Type = ApplicationCommandOptionType.Boolean
            }
        ];

        public override async Task<Response> ExecuteAsync(List<string> arguments, ICommandSender sender, Dictionary<string, string> optionValues, CancellationToken ct = default)
        {
            if (!optionValues.TryGetValue("allowprereleases", out var boolval))
                return Response.Failed("Failed to get AllowPrereleases value");

            ContainerBuilder container = new();
            container.AddComponent(new TextDisplayBuilder($"## Updater"));
            if (!bool.TryParse(boolval, out bool allowPreReleases))
            {
                container.AddComponent(new TextDisplayBuilder($"### Invalid boolean value. Use true or false."));
                return Response.Failed(new ComponentBuilderV2(container).Build());
            }

            (Version, string) update = await UpdateManager.CheckForUpdate(Bot.Instance.version, allowPreReleases);
            if (update.Item1 is not null)
            {
                container.AddComponent(new TextDisplayBuilder($"### {update} Update available. \n Run `Update` from the console to update the bot"));
                return Response.Succeed(new ComponentBuilderV2(container).Build());
            }
            else
            {
                container.AddComponent(new TextDisplayBuilder($"### {update.Item2}"));
                return Response.Succeed(new ComponentBuilderV2(container).Build());
            }
        }
    }
}