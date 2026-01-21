using System.Diagnostics;
using System.Reflection;
using Discord;
using Discord.WebSocket;
using SEBotV2.API.Helpers;

namespace SEBotV2.Commands.Text
{
    public class Restart : TextCommandBase
    {
        public override string Name => "Restart";
        public override bool ShouldDefer => true;
        public override bool OnlyAllowOwner => true;

        public override async Task<CommandResult> ExecuteAsync(List<string> arguments, SocketUser user, CancellationToken ct = default)
        {
            ContainerBuilder container = new();
            try
            {
                ContainerBuilder initcontainer = new();
                initcontainer.AddComponent(new TextDisplayBuilder($"## Restart\n### Restarting bot, please wait..."));
                await user.SendMessageAsync(components: new ComponentBuilderV2(container).Build());
            }
            catch (Exception ex)
            {
                LogManager.Warn($"Could not DM user before restart: {ex.Message}");
            }

            try
            {
                LogManager.Info("Begin graceful shutdown for restart...");
                try
                {
                    if (Bot.Instance?._client != null)
                    {
                        await Bot.Instance._client.LogoutAsync().ConfigureAwait(false);
                        await Bot.Instance._client.StopAsync().ConfigureAwait(false);
                        await Bot.Instance.ShutdownAsync().ConfigureAwait(false);
                    }
                }
                catch (Exception ex)
                {
                    LogManager.Warn($"Error stopping Discord client: {ex.Message}");
                }
            }
            catch (Exception ex)
            {
                LogManager.Error($"Error during graceful shutdown steps: {ex.Message}");
            }

            try
            {
                string entry = Environment.ProcessPath ?? Process.GetCurrentProcess().MainModule?.FileName;
                if (string.IsNullOrEmpty(entry))
                {
                    LogManager.Error("Could not determine location for restart.");
                    container.AddComponent(new TextDisplayBuilder($"## Restart\n### Restart failed: entry path unknown"));
                    return CommandResult.From(false, string.Empty, component: new ComponentBuilderV2(container).Build());
                }

                string ext = Path.GetExtension(entry).ToLowerInvariant();
                ProcessStartInfo psi;

                string restartArg = $"restarted:{user.Id}";
                string otherArgs = string.Join(' ', Environment.GetCommandLineArgs().Skip(1).Select(a => $"\"{a}\""));
                string allArgs = string.IsNullOrWhiteSpace(otherArgs) ? restartArg : $"{otherArgs} {restartArg}";

                psi = new ProcessStartInfo
                {
                    FileName = entry,
                    Arguments = allArgs,
                    WorkingDirectory = Path.GetDirectoryName(entry) ?? Environment.CurrentDirectory,
                    UseShellExecute = true
                };

                LogManager.Info($"Spawning new process to restart: {psi.FileName} {psi.Arguments}");

                Process? newProc = Process.Start(psi);

                if (newProc == null)
                {
                    LogManager.Error("Failed to spawn restart process (Process.Start returned null).");
                    container.AddComponent(new TextDisplayBuilder($"## Restart\n### Restart failed (couldn't start new process)."));
                    return CommandResult.From(false, string.Empty, component: new ComponentBuilderV2(container).Build());
                }

                await Task.Delay(1000, ct);

                LogManager.Info("Exiting current process to complete restart.");
                Environment.Exit(0);

                    container.AddComponent(new TextDisplayBuilder($"## Restart\n### Restart initiated"));
                return CommandResult.From(true, string.Empty, component: new ComponentBuilderV2(container).Build());
            }
            catch (Exception ex)
            {
                LogManager.Error($"Restart error: {ex.Message}");
                    container.AddComponent(new TextDisplayBuilder($"## Restart\n### Restart failed: {ex.Message}"));
                return CommandResult.From(false, string.Empty, component: new ComponentBuilderV2(container).Build());
            }
        }
    }
}