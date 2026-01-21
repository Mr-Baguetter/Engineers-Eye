using System.Text;
using SEBotV2.API.Helpers;

namespace SEBotV2.Commands.Console
{
    public class Restart : ConsoleCommandBase
    {
        public override string Name => "Restart";
        public override string Description => "Restarts the bot";
        public override async Task<CommandResult> ExecuteAsync(List<string> arguments, CancellationToken ct = default)
        {
            StringBuilder sb = new();
            ulong ownerId = await ConfigManager.LoadAsync<ulong>("OwnerUserId");
            
            sb.AppendLine("Initiating restart...");
            await Bot.Instance.ShutdownAsync();
            
            string exePath = Environment.ProcessPath ?? System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName;
            if (string.IsNullOrEmpty(exePath)) 
            {
                LogManager.Error("Could not determine executable path for restart");
                return CommandResult.From(false, "Could not determine executable path for restart");;
            }

            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = exePath,
                Arguments = $"restarted:{ownerId}",
                UseShellExecute = true
            });

            Environment.Exit(0);
            return CommandResult.From(true, sb.ToString());
        }
    }
}