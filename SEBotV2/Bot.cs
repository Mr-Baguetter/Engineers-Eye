using System.Diagnostics;
using Discord;
using Discord.WebSocket;
using Okolni.Source.Query.Responses;
using SEBotV2.API.Helpers;
using SEBotV2.API.Net;
using SEBotV2.Commands;
using SEBotV2.Commands.Console;
using SEBotV2.Commands.Text;
using static SEBotV2.API.Helpers.ServerManager;

// & "C:\Program Files\Microsoft Visual Studio\18\Insiders\MSBuild\Current\Bin\MSBuild.exe" SEBotV2.csproj
namespace SEBotV2
{
    public class Bot
    {
        public class ServerPingInfo
        {
            public string IP { get; set; }
            public int Port { get; set; }
        }

        private CancellationTokenSource? _mainCts;
        private Task? _mainLoopTask;

        private CancellationTokenSource? _statusCts;
        private Task? _statusLoopTask;
        private readonly TimeSpan _statusInterval = TimeSpan.FromMilliseconds(1000 * 30);

        private readonly SemaphoreSlim _statusSemaphore = new(1, 1);

        public static event Action<ServerInfo, ServerInfo, Player>? OnPlayerJoined;
        public static event Action<ServerInfo, ServerInfo, Player>? OnPlayerLeft;

        public DiscordShardedClient _client;
        public HttpClient _httpClient;
        public Timer _updateTimer;
        internal Version version { get; set; } = new(1, 0, 0);
        public static Bot Instance { get; set; }
        internal CommandHandler CommandHandler = new();
        internal ConsoleCommandHandler ConsoleCommandHandler;

        public static async Task Main(string[] args)
        {
            bool hasRestartArg = args?.Any(a => a.StartsWith("restarted:", StringComparison.OrdinalIgnoreCase)) ?? false;

            if (Process.GetProcessesByName("SEBotV2").Length > 1 && !hasRestartArg)
            {
                Console.WriteLine("Another instance of SEBotV2 is already running. Exiting...");
                return;
            }

            Bot bot = new();
            await bot.RunAsync(args);
        }

        public async Task RunAsync(string[] args = null)
        {
#region Configs
            await ConfigManager.AddTypeAsync<string>("BotToken", default);
            await ConfigManager.AddTypeAsync<Dictionary<ulong, ServerPingInfo>>("PingInfo", default);
            await ConfigManager.AddTypeAsync<float>("UpdateIntervalMs", default);
            await ConfigManager.AddTypeAsync<Dictionary<ulong, ulong>>("LogChannel", default);
            await ConfigManager.AddTypeAsync<bool>("Debug", true);
            await ConfigManager.AddTypeAsync<int>("PlayerLeaveNotificationAutoRemoveTime", 3600);
            await ConfigManager.AddTypeAsync<int>("PlayerLeaveNotificationCheckInterval", 30);
            await ConfigManager.AddTypeAsync<ulong>("OwnerUserId", default);
#endregion

            await LogManager.Init();

            _httpClient = new HttpClient();
            Instance = this;
            DiscordSocketConfig discordConfig = new()
            {
                GatewayIntents = GatewayIntents.All
            };

            _client = new DiscordShardedClient(discordConfig);
            _client.Log += LogAsync;
            _client.ShardReady += ReadyAsync;
            _client.SlashCommandExecuted += OnSlashCommand;

            try
            {
                await _client.LoginAsync(TokenType.Bot, await ConfigManager.LoadAsync<string>("BotToken") ?? "");
                await _client.StartAsync();

                LogManager.Debug($"Total shards: {_client.Shards.Count}");

                Console.CancelKeyPress += OnCancelKeyPress;
                LogManager.Info("Bot is running. Press Ctrl+C to stop.");
                CommandHandler.GetAndAddAllCommands();
                await TextCommandHandler.Register();
                TextCommandHandler.RegisterAllCommands();
                await PlayerManager.Register();
                await PlayerLeaveNotificationManager.Register();
                TranslationManager.Init();
                await UpdateManager.Init();

                _mainCts = new CancellationTokenSource();
                _mainLoopTask = Task.Run(() => RunUpdateLoopAsync(_mainCts.Token), CancellationToken.None);

                if (args.Any(a => a.StartsWith("restarted:", StringComparison.OrdinalIgnoreCase)))
                {
                    string arg = args.First(a => a.StartsWith("restarted:", StringComparison.OrdinalIgnoreCase));
                    if (ulong.TryParse(arg.Substring("restarted:".Length), out ulong userId))
                    {
                        SocketUser user = _client.GetUser(userId);
                        if (user != null)
                        {
                            LogManager.Info($"Successfully restarted by {user.GlobalName}");
                        }
                        else
                            LogManager.Info($"Successfully restarted by null user");
                    }
                }

                await Task.Delay(-1);
            }
            catch (Exception ex)
            {
                LogManager.Warn($"Failed to start bot: {ex.Message}");
                LogManager.Warn("Please check your Discord token in config.json");
                LogManager.Warn("Press any key to exit...");
                Console.ReadKey();
            }
        }

        private async Task RunUpdateLoopAsync(CancellationToken token)
        {
            try
            {
                while (!token.IsCancellationRequested)
                {
                    try
                    {
                        await UpdateServerInfo();
                    }
                    catch (OperationCanceledException) when (token.IsCancellationRequested)
                    {
                        break;
                    }
                    catch (Exception ex)
                    {
                        LogManager.Error($"Error updating server info: {ex}");
                    }

                    try
                    {
                        await Task.Delay(TimeSpan.FromSeconds(5), token);
                    }
                    catch (TaskCanceledException)
                    {
                        break;
                    }
                }
            }
            finally
            {
                LogManager.Info("Main update loop stopped.");
            }
        }

        private static async Task UpdateServerInfo()
        {
            Dictionary<ulong, ServerPingInfo> pingInfo = await ConfigManager.LoadAsync<Dictionary<ulong, ServerPingInfo>>("PingInfo") ?? [];
            foreach (ulong id in pingInfo.Keys.ToArray())
            {
                ServerInfo old = ServerManager.ServerInfoByGuildId.ContainsKey(id) ? ServerManager.ServerInfoByGuildId[id] : null;
                ServerInfo info = await ServerManager.QueryServer(id) ?? new();
                old ??= new();

                List<string> playerNames = [];
                foreach (Player player in info.Players)
                    playerNames.Add(player.Name);

                List<string> oldPlayerNames = [];
                foreach (Player player in old.Players)
                    oldPlayerNames.Add(player.Name);

                if (old != null && !oldPlayerNames.SequenceEqual(playerNames))
                {
                    LogManager.Info($"Server {info.Name} player list changed!");
                    List<Player> newPlayers = info.Players.Where(p => old.Players.All(op => op.Name != p.Name)).ToList();
                    foreach (Player player in newPlayers)
                    {
                        if (!string.IsNullOrWhiteSpace(player.Name) && player.Name.Trim().Length != 0 && player.Name != "")
                        {
                            Logging.LogToFile($"New player joined: {player.Name}");
                            LogManager.Info($"New player joined: {player.Name}");
                            OnPlayerJoined?.Invoke(old, info, player);
                        }
                        else
                            LogManager.Debug($"Unknown player left the server");
                    }

                    List<Player> leftPlayers = old.Players.Where(op => info.Players.All(p => p.Name != op.Name)).ToList();
                    foreach (Player player in leftPlayers)
                    {
                        if (!string.IsNullOrWhiteSpace(player.Name) && player.Name.Trim().Length != 0 && player.Name != "")
                        {
                            Logging.LogToFile($"Player left: {player.Name}");
                            LogManager.Info($"Player left: {player.Name}");
                            OnPlayerLeft?.Invoke(old, info, player);
                        }
                        else
                            LogManager.Debug($"Unknown player left the server");
                    }
                }

                ServerManager.ServerInfoByGuildId[id] = info;
                LogManager.Info($"Server {info.Name}: {info.PlayerCount}/{info.MaxPlayers} players online");
                LogManager.Info($"Server {info.Name}: {string.Join(", ", playerNames)}");
            }
        }

        private Task OnSlashCommand(SocketSlashCommand command)
        {
            _ = Task.Run(async () =>
            {
                try 
                {
                    await CommandHandler.ExecuteAsync(command);
                }
                catch (Exception ex)
                {
                    LogManager.Error($"Background command error: {ex}");

                    try
                    {
                        if (!command.HasResponded)
                            await command.RespondAsync("An internal error occurred while executing the command.", ephemeral: true);
                        else
                            await command.FollowupAsync("An internal error occurred while executing the command.", ephemeral: true);
                    }
                    catch (Exception inner)
                    {
                        LogManager.Error($"Failed to send error response for command '{command.Data.Name}': {inner}");
                    }
                }
            });

            return Task.CompletedTask;
        }

        private async Task ReadyAsync(DiscordSocketClient client)
        {
            LogManager.Info($"Bot logged in as {client.CurrentUser.Username}");

            ConsoleCommandHandler = new();
            ConsoleCommandHandler.Start();
            foreach (SocketGuild guild in _client.Guilds.ToArray())
            {
                LogManager.Debug($"{guild.Name}");
                await CommandHandler.RegisterCommandsWithDiscordAsync(guild.Id);
            }

            await UpdateBotStatusAsync();
            StartStatusLoop();
        }

        private void StartStatusLoop()
        {
            if (_statusLoopTask != null && !_statusLoopTask.IsCompleted) 
                return;

            _statusCts = new CancellationTokenSource();
            CancellationToken token = _statusCts.Token;
            _statusLoopTask = Task.Run(async () =>
            {
                try
                {
                    while (!token.IsCancellationRequested)
                    {
                        await _statusSemaphore.WaitAsync(token);
                        try
                        {
                            if (_client?.CurrentUser == null)
                                break;

                            try
                            {
                                await UpdateBotStatusAsync();
                            }
                            catch (InvalidOperationException ioe)
                            {
                                LogManager.Warn($"Status update aborted: {ioe.Message}");
                                break;
                            }
                            catch (OperationCanceledException) when (token.IsCancellationRequested)
                            {
                                break;
                            }
                            catch (Exception ex)
                            {
                                LogManager.Error($"Unhandled error while updating status: {ex}");
                            }
                        }
                        finally
                        {
                            _statusSemaphore.Release();
                        }

                        try
                        {
                            await Task.Delay(_statusInterval, token);
                        }
                        catch (TaskCanceledException)
                        {
                            break;
                        }
                    }
                }
                finally
                {
                    LogManager.Info("Status loop stopped.");
                }
            }, token);
        }

        private async Task StopStatusLoopAsync()
        {
            if (_statusCts == null)
                return;

            try
            {
                _statusCts.Cancel();

                if (_statusLoopTask != null)
                    await Task.WhenAny(_statusLoopTask, Task.Delay(TimeSpan.FromSeconds(5)));

                await _statusSemaphore.WaitAsync(TimeSpan.FromSeconds(2));
                _statusSemaphore.Release();
            }
            catch (Exception ex)
            {
                LogManager.Warn($"Error stopping status loop: {ex}");
            }
            finally
            {
                _statusCts.Dispose();
                _statusCts = null;
                _statusLoopTask = null;
            }
        }

        private async Task UpdateBotStatusAsync()
        {
            if (_client?.CurrentUser == null)
                return;

            try
            {
                Dictionary<ulong, ServerPingInfo> kvp = await ConfigManager.LoadAsync<Dictionary<ulong, ServerPingInfo>>("PingInfo") ?? [];
                ulong id = kvp?.Keys.FirstOrDefault() ?? 123456789012345678;
                ServerInfo serverInfo = await ServerManager.QueryServer(id);

                string statusText;
                ActivityType activityType = ActivityType.Watching;
                UserStatus userStatus;

                if (serverInfo is not null)
                {
                    statusText = $"{serverInfo.PlayerCount}/{serverInfo.MaxPlayers} players";
                    userStatus = UserStatus.Online;
                    LogManager.Info($"[{DateTime.Now:HH:mm:ss}] Server: {serverInfo.Name} - {statusText}");
                }
                else
                {
                    statusText = "Server Offline";
                    activityType = ActivityType.CustomStatus;
                    userStatus = UserStatus.DoNotDisturb;
                    LogManager.Info($"[{DateTime.Now:HH:mm:ss}] Server appears to be offline");
                }

                if (_client?.CurrentUser != null)
                {
                    await _client.SetCustomStatusAsync(statusText);
                    await _client.SetStatusAsync(userStatus);
                }
            }
            catch (InvalidOperationException)
            {
                LogManager.Warn("Attempted to update status while client was not logged in.");
            }
            catch (Exception ex)
            {
                LogManager.Warn($"Error updating bot status: {ex.Message}");
                try
                {
                    if (_client?.CurrentUser != null)
                    {
                        await _client.SetCustomStatusAsync("Connection Error");
                        await _client.SetStatusAsync(UserStatus.DoNotDisturb);
                    }
                }
                catch { }
            }
        }

        public async Task ShutdownAsync()
        {
            LogManager.Info("Shutting down bot...");

            ConsoleCommandHandler.Stop();
            await StopStatusLoopAsync().ConfigureAwait(false);
            await UpdateManager.Stop().ConfigureAwait(false);
            LogManager.SaveLogs();
            await PlayerLeaveNotificationManager.StopCheckTimeLoop().ConfigureAwait(false);
            if (_mainCts != null)
            {
                try
                {
                    _mainCts.Cancel();
                    if (_mainLoopTask != null)
                        await Task.WhenAny(_mainLoopTask, Task.Delay(TimeSpan.FromSeconds(5)));
                }
                catch (Exception ex)
                {
                    LogManager.Warn($"Error stopping main loop: {ex}");
                }
                finally
                {
                    _mainCts.Dispose();
                    _mainCts = null;
                    _mainLoopTask = null;
                }
            }

            try { _httpClient?.Dispose(); } catch { }
            try
            {
                if (_client == null)
                {
                    LogManager.Debug("Discord client is null, skipping logout/stop.");
                }
                else
                {
                    if (_client is DiscordShardedClient sharded)
                    {
                        if (sharded.Shards != null && sharded.Shards.Any())
                        {
                            LogManager.Info($"Stopping {sharded.Shards.Count} shard(s)...");

                            IEnumerable<Task> logoutTasks = sharded.Shards.Select(async shardClient =>
                            {
                                try
                                {
                                    if (shardClient.CurrentUser != null)
                                        await shardClient.LogoutAsync().ConfigureAwait(false);
                                }
                                catch (Exception ex)
                                {
                                    LogManager.Warn($"Error logging out shard {shardClient.ShardId}: {ex.Message}");
                                }
                            });
                            await Task.WhenAll(logoutTasks).ConfigureAwait(false);

                            IEnumerable<Task> stopTasks = sharded.Shards.Select(async shardClient =>
                            {
                                try
                                {
                                    await shardClient.StopAsync().ConfigureAwait(false);
                                }
                                catch (Exception ex)
                                {
                                    LogManager.Warn($"Error stopping shard {shardClient.ShardId}: {ex.Message}");
                                }
                            });
                            await Task.WhenAll(stopTasks).ConfigureAwait(false);
                        }
                        else
                        {
                            LogManager.Warn("Sharded client has no shards attempting guarded StopAsync/LogoutAsync on sharded client.");
                            try { await _client.LogoutAsync().ConfigureAwait(false); } catch (Exception ex) { LogManager.Warn($"Logout on sharded client failed: {ex.Message}"); }
                            try { await _client.StopAsync().ConfigureAwait(false); } catch (Exception ex) { LogManager.Warn($"Stop on sharded client failed: {ex.Message}"); }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogManager.Warn($"Error while logging out Discord client: {ex}");
            }
            finally
            {
                try { _client?.Dispose(); } catch { }
            }
            LogManager.Info("Shutdown complete.");
        }

        private Task LogAsync(LogMessage msg)
        {
            if (string.IsNullOrWhiteSpace(msg.Message))
                return Task.CompletedTask;

            switch (msg.Severity)
            {
                case LogSeverity.Debug:
                    LogManager.Debug(msg.Message);
                    break;
                case LogSeverity.Info:
                    LogManager.Info(msg.Message);
                    break;
                case LogSeverity.Warning:
                    LogManager.Warn(msg.Message);
                    break;
                case LogSeverity.Error:
                    LogManager.Error(msg.Message);
                    break;
                case LogSeverity.Critical:
                    LogManager.Critical(msg.Message);
                    break;
                default:
                    LogManager.Info(msg.Message);
                    break;
            }

            return Task.CompletedTask;
        }


        private void OnCancelKeyPress(object sender, ConsoleCancelEventArgs e)
        {
            e.Cancel = true;
            ShutdownAsync().GetAwaiter().GetResult();

            Environment.Exit(0);
        }
    }
}