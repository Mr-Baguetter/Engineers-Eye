using System.Text;
using Okolni.Source.Query.Responses;
using SEBotV2.API.Helpers;
using static SEBotV2.API.Helpers.ServerManager;

namespace SEBotV2.Commands.Console
{
    public class QueryServer : ConsoleCommandBase
    {
        public override string Name => "QueryServer";
        public override string Description => "Querys the specified server";
        public override async Task<CommandResult> ExecuteAsync(List<string> arguments, CancellationToken ct = default)
        {
            int port = 0;
            string ip = string.Empty;
            StringBuilder sb = new();
            if (arguments.Count <= 1)
            {
                string[] msg = arguments[0].Split(":");
                ip = msg[0];
                if (!int.TryParse(msg[1], out port) || port > 65535)
                    return CommandResult.From(false, "Failed to parse server port!");
            }
            else if (arguments.Count == 2)
            {
                ip = arguments[0];
                if (!int.TryParse(arguments[1], out port) || port > 65535)
                    return CommandResult.From(false, "Failed to parse server port!");
            }
            else
                return CommandResult.From(false, "Usage: queryserver <ServerIp:ServerPort>");

            ServerInfo info = await ServerManager.PerformQuery(ip, port, string.Empty);
            if (info is null)
                return CommandResult.From(false, "Failed to query server!");

            sb.AppendLine($"Server Name: {info.Name}");
            sb.AppendLine($"Server Version: {info.InfoResponse.Version}");
            sb.AppendLine($"Player Count: {info.PlayerCount}/{info.MaxPlayers}");
            sb.AppendLine($"Players:");
            foreach (Player player in info.Players.ToArray())
            {
                DateTime joinTime = DateTime.UtcNow - player.Duration;
                TimeZoneInfo localZone = TimeZoneInfo.Local;
                DateTime localTime = TimeZoneInfo.ConvertTimeFromUtc(joinTime, localZone);
                sb.AppendLine($"{player.Name}");
                sb.AppendLine($"  Joined: {localTime:HH:mm:ss}");
            }

            return CommandResult.From(true, sb.ToString());
        }
    }
}