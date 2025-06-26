using Discord;
using Discord.WebSocket;
using SEBotV2.API.Interfaces;
using SEBotV2.API.Services;
using SEBotV2.Commands.User;
using SpaceEngineersDiscordBot.Commands;

namespace SEBotV2
{
    public class Program
    {
        private const string CONFIG_FILE = "config.json";

        private DiscordSocketClient _client;
        private HttpClient _httpClient;
        private Timer _updateTimer;
        private Config _config;
        private ConfigService _configService;
        private ServerService _serverService;
        private List<ICommand> _commands;
        internal Version version { get; set; } = new(0, 2);

        public static async Task Main(string[] args)
        {
            var program = new Program();
            await program.RunAsync();
        }

        public async Task RunAsync()
        {
            _configService = new ConfigService(CONFIG_FILE);
            _httpClient = new HttpClient();

            _config = await _configService.LoadOrCreateConfigAsync();

            if (!_config.IsValid())
            {
                Console.WriteLine("Please fill out the configuration file (config.json) and restart the bot.");
                Console.WriteLine("Press any key to exit...");
                Console.ReadKey();
                return;
            }

            _serverService = new ServerService(_httpClient, _config);

            var discordConfig = new DiscordSocketConfig
            {
                GatewayIntents = GatewayIntents.Guilds
            };

            _client = new DiscordSocketClient(discordConfig);

            InitializeCommands();

            _client.Log += LogAsync;
            _client.Ready += ReadyAsync;
            _client.SlashCommandExecuted += SlashCommandHandler;

            try
            {
                await _client.LoginAsync(TokenType.Bot, _config.DiscordToken);
                await _client.StartAsync();

                Console.CancelKeyPress += OnCancelKeyPress;
                Console.WriteLine("Bot is running. Press Ctrl+C to stop.");
                await Task.Delay(-1);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to start bot: {ex.Message}");
                Console.WriteLine("Please check your Discord token in config.json");
                Console.WriteLine("Press any key to exit...");
                Console.ReadKey();
            }
        }

        private void InitializeCommands()
        {
            _commands = new List<ICommand>
            {
                new SetServerCommand(_configService, _serverService),
                new StatusCommand(_serverService, _config),
            };
        }

        private async Task ReadyAsync()
        {
            Console.WriteLine($"Bot logged in as {_client.CurrentUser.Username}#{_client.CurrentUser.Discriminator}");
            Console.WriteLine($"Monitoring Space Engineers server at {_config.ServerIp}:{_config.ServerPort}");

            await RegisterSlashCommandsAsync();
            await UpdateBotStatusAsync();

            _updateTimer = new Timer(async _ => await UpdateBotStatusAsync(), null, TimeSpan.Zero, TimeSpan.FromMilliseconds(_config.UpdateIntervalMs));
        }

        private async Task RegisterSlashCommandsAsync()
        {
            try
            {
                foreach (var command in _commands)
                {
                    await _client.CreateGlobalApplicationCommandAsync(command.Build());
                }

                Console.WriteLine("Slash commands registered successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to register slash commands: {ex.Message}");
            }
        }

        private async Task SlashCommandHandler(SocketSlashCommand command)
        {
            try
            {
                var slashCommand = _commands.Find(c => c.Name == command.Data.Name);
                if (slashCommand != null)
                {
                    await slashCommand.ExecuteAsync(command);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error handling slash command: {ex.Message}");

                if (!command.HasResponded)
                {
                    await command.RespondAsync("An error occurred while processing the command.", ephemeral: true);
                }
            }
        }

        private async Task UpdateBotStatusAsync()
        {
            try
            {
                var serverInfo = await _serverService.GetServerInfoAsync();

                string statusText;
                ActivityType activityType = ActivityType.Watching;
                UserStatus userStatus;

                if (serverInfo.IsOnline)
                {
                    statusText = $"{serverInfo.Players}/{serverInfo.MaxPlayers} players";
                    userStatus = UserStatus.Online;
                    Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Server: {serverInfo.Name} - {statusText}");
                }
                else
                {
                    statusText = "Server Offline";
                    activityType = ActivityType.CustomStatus;
                    userStatus = UserStatus.DoNotDisturb;
                    Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Server appears to be offline");
                }

                await _client.SetActivityAsync(new Game(statusText, activityType));
                await _client.SetStatusAsync(userStatus);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating bot status: {ex.Message}");

                await _client.SetActivityAsync(new Game("Connection Error", ActivityType.CustomStatus));
                await _client.SetStatusAsync(UserStatus.DoNotDisturb);
            }
        }

        private static Task LogAsync(LogMessage log)
        {
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] [{log.Severity}] {log.Source}: {log.Message}");
            return Task.CompletedTask;
        }

        private void OnCancelKeyPress(object sender, ConsoleCancelEventArgs e)
        {
            Console.WriteLine("\nShutting down bot...");

            _updateTimer?.Dispose();
            _httpClient?.Dispose();
            _client?.LogoutAsync().GetAwaiter().GetResult();
            _client?.Dispose();

            Environment.Exit(0);
        }
    }
}