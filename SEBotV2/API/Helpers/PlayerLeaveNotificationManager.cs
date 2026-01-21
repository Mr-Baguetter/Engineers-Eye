using static SEBotV2.API.Helpers.ServerManager;
using Okolni.Source.Query.Responses;
using Discord.WebSocket;
using Discord;

namespace SEBotV2.API.Helpers
{
    public class PlayerLeaveNotificationManager
    {
        /// <summary>
        /// Key: User ID (ulong) - The unique identifier of the user.
        /// Value: Tuple containing:
        ///   - Item1 (ulong): Guild ID where the user created this.
        ///   - Item2 (DateTime): Timestamp when this was created.
        /// </summary>
        public static Dictionary<ulong, (ulong, DateTime)> ActiveUsers = [];

        private static CancellationTokenSource? _cts;
        private static Task? _checkLoopTask;
        public static bool IsCheckLoopRunning => _checkLoopTask != null && !_checkLoopTask.IsCompleted;

        public static async Task Register()
        {
            Bot.OnPlayerLeft += OnPlayerLeft;
            StartCheckTimeLoop(ConfigManager.LoadAsync<int?>("PlayerLeaveNotificationAutoRemoveTime").GetAwaiter().GetResult() ?? 3600, ConfigManager.LoadAsync<int?>("PlayerLeaveNotificationCheckInterval").GetAwaiter().GetResult() ?? 30);
        }

        public static void StartCheckTimeLoop(int removeTime, int checkInterval)
        {
            if (IsCheckLoopRunning)
            {
                LogManager.Debug("CheckTimeLoop already running, start request ignored.");
                return;
            }

            _cts = new CancellationTokenSource();
            _checkLoopTask = CheckTimeLoop(removeTime, checkInterval, _cts.Token);
        }

        public static async Task StopCheckTimeLoop(int timeoutMs = 5000)
        {
            if (_cts == null)
            {
                LogManager.Debug("CheckTimeLoop cancellation requested but it wasn't running.");
                return;
            }

            try
            {
                LogManager.Info("Stopping CheckTimeLoop...");
                _cts.Cancel();

                if (_checkLoopTask != null)
                {
                    Task completed = await Task.WhenAny(_checkLoopTask, Task.Delay(timeoutMs));
                    if (completed != _checkLoopTask)
                    {
                        LogManager.Warn("CheckTimeLoop did not stop within timeout.");
                    }
                    else
                        LogManager.Info("CheckTimeLoop stopped gracefully.");
                }
            }
            catch (Exception ex)
            {
                LogManager.Error($"Error while stopping CheckTimeLoop: {ex.Message}");
            }
            finally
            {
                try { _cts.Dispose(); } catch { }
                _cts = null;
                _checkLoopTask = null;
            }
        }

        private static async Task CheckTimeLoop(int removeTime, int checkInterval, CancellationToken ct)
        {
            try
            {
                while (!ct.IsCancellationRequested)
                {
                    try
                    {
                        foreach (var kvp in ActiveUsers.ToArray())
                        {
                            TimeSpan timeAdded = DateTime.Now - kvp.Value.Item2;

                            if (timeAdded.TotalSeconds >= removeTime)
                            {
                                ContainerBuilder container = new();
                                container.AddComponent(new TextDisplayBuilder(TranslationManager.Get("Player Leave Notification Header")));
                                container.AddComponent(new TextDisplayBuilder(TranslationManager.Get("Player Leave Notification Automatic Removal")));

                                SocketUser? user = Bot.Instance._client.GetUser(kvp.Key);
                                if (user != null)
                                {
                                    try
                                    {
                                        IUserMessage userMessage = await user.SendMessageAsync(components: new ComponentBuilderV2(container).Build());
                                    }
                                    catch (Exception sendEx)
                                    {
                                        LogManager.Warn($"Failed to send automatic removal DM to user {kvp.Key}: {sendEx.Message}");
                                    }
                                }

                                LogManager.Debug($"User {kvp.Key} created longer than {timeAdded.TotalSeconds} seconds, removing from active users");
                                ActiveUsers.Remove(kvp.Key);
                            }
                        }

                        await Task.Delay(checkInterval * 1000, ct);
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                    catch (Exception ex)
                    {
                        LogManager.Error($"Error in CheckTimeLoop iteration: {ex.Message}");
                        try 
                        {
                            await Task.Delay(30000, ct);
                        }
                        catch (OperationCanceledException)
                        {
                            break;
                        }
                    }
                }
            }
            finally
            {
                LogManager.Debug("CheckTimeLoop exiting.");
            }
        }

        private static void OnPlayerLeft(ServerInfo old, ServerInfo info, Player player)
        {
            if (ActiveUsers.Count == 0)
                return;

            ContainerBuilder container = new();

            Dictionary<ulong, ulong> logChannel = ConfigManager.LoadAsync<Dictionary<ulong, ulong>>("LogChannel").GetAwaiter().GetResult() ?? [];

            foreach (var kvp in ActiveUsers)
            {
                SocketUser user = Bot.Instance._client.GetUser(kvp.Key);

                container.AddComponent(new TextDisplayBuilder(TranslationManager.Get("Player Leave Notification Header")));
                container.AddComponent(new TextDisplayBuilder(TranslationManager.Get("Player Leave Notification Leave Message").Replace("{ServerName}", info.Name)));
                IUserMessage userMessage = user.SendMessageAsync(components: new ComponentBuilderV2(container).Build()).GetAwaiter().GetResult();
                if (userMessage is null)
                {
                    container.AddComponent(new TextDisplayBuilder($"{user.Mention}"));
                    SocketChannel baseChannel = Bot.Instance._client.GetChannel(logChannel[kvp.Value.Item1]);
                    if (baseChannel is IMessageChannel channel)
                        channel.SendMessageAsync(components: new ComponentBuilderV2(container).Build());
                }
            }
        }
    }
}