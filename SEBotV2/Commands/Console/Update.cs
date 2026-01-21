using System.Text;
using Discord.WebSocket;
using SEBotV2.API.Helpers;
using SEBotV2.API.Net;

namespace SEBotV2.Commands.Console
{
    public class Update : ConsoleCommandBase
    {
        public override string Name => "Update";
        public override string Description => "Updates the Bot";
        public override async Task<CommandResult> ExecuteAsync(List<string> arguments, CancellationToken ct = default)
        {
            if (arguments.Count < 2)
                return CommandResult.From(false, "Usage: update <force (true/false)> <allowPreReleases (true/false)>");

            if (!bool.TryParse(arguments[0], out bool force))
                return CommandResult.From(false, "Invalid boolean value in force. Use true or false");
            if (!bool.TryParse(arguments[1], out bool allowPreReleases))
                return CommandResult.From(false, "Invalid boolean value in allowPreReleases. Use true or false");

            if (await UpdateManager.DownloadUpdate(Bot.Instance.version, force, allowPreReleases))
                return CommandResult.From(true, "Update downloaded. Restart the bot to apply the update");
            else
                return CommandResult.From(true, "Update failed. Are you on the latest version?");
        }
    }
}