using System.Text;
using Discord.WebSocket;

namespace SEBotV2.Commands.Console
{
    public class Guilds : ConsoleCommandBase
    {
        public override string Name => "Guilds";
        public override string Description => "Lists all connected Discord guilds";
        public override async Task<CommandResult> ExecuteAsync(List<string> arguments, CancellationToken ct = default)
        {
            StringBuilder sb = new();
            sb.AppendLine("\n=== Connected Guilds ===");
            
            if (Bot.Instance._client.Guilds == null || Bot.Instance._client.Guilds.Count == 0)
            {
                sb.AppendLine("No guilds connected.");
                return CommandResult.From(false, sb.ToString());
            }

            foreach (SocketGuild guild in Bot.Instance._client.Guilds)
            {
                sb.AppendLine($"[{guild.Id}] {guild.Name} - {guild.MemberCount} members");
            }

            return CommandResult.From(true, sb.ToString());
        }
    }
}