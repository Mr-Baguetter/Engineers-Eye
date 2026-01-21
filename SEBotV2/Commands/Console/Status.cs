using System.Text;
using Discord.WebSocket;
using SEBotV2.API.Helpers;

namespace SEBotV2.Commands.Console
{
    public class Status : ConsoleCommandBase
    {
        public override string Name => "Status";
        public override string Description => "Gets the status";
        public override async Task<CommandResult> ExecuteAsync(List<string> arguments, CancellationToken ct = default)
        {
            StringBuilder sb = new();
            sb.AppendLine("\n=== Bot Status ===");
            sb.AppendLine($"Username: {Bot.Instance._client.CurrentUser?.Username ?? "Not logged in"}");
            sb.AppendLine($"Guilds: {Bot.Instance._client.Guilds?.Count ?? 0}");
            sb.AppendLine($"Shards: {Bot.Instance._client.Shards?.Count ?? 0}");
            
            Dictionary<ulong, Bot.ServerPingInfo> pingInfo = await ConfigManager.LoadAsync<Dictionary<ulong, Bot.ServerPingInfo>>("PingInfo") ?? [];
            sb.AppendLine($"Monitored Servers: {pingInfo.Count}");
            return CommandResult.From(true, sb.ToString());
        }
    }
}