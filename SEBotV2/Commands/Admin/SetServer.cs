using Discord;
using Discord.WebSocket;
using SEBotV2;
using SEBotV2.API.Interfaces;
using SEBotV2.API.Services;

namespace SpaceEngineersDiscordBot.Commands
{
    public class SetServerCommand : ICommand
    {
        private readonly ConfigService _configService;
        private readonly ServerService _serverService;
        private Config _config;

        public string Name => "setserver";

        public SetServerCommand(ConfigService configService, ServerService serverService)
        {
            _configService = configService;
            _serverService = serverService;
        }

        public SlashCommandProperties Build()
        {
            return new SlashCommandBuilder()
                .WithName(Name)
                .WithDescription("Set the Space Engineers server to monitor")
                .AddOption("ip", ApplicationCommandOptionType.String, "Server IP address", isRequired: true)
                .AddOption("port", ApplicationCommandOptionType.Integer, "Server port (default: 27016)", isRequired: false)
                .Build();
        }

        public async Task ExecuteAsync(SocketSlashCommand command)
        {
            if (command.User is not SocketGuildUser guildUser || !guildUser.GuildPermissions.Administrator)
            {
                await command.RespondAsync("You need Administrator permissions to change server settings.", ephemeral: true);
                return;
            }

            var ip = command.Data.Options.FirstOrDefault(x => x.Name == "ip")?.Value?.ToString();
            var portOption = command.Data.Options.FirstOrDefault(x => x.Name == "port")?.Value;
            int port = portOption != null ? Convert.ToInt32(portOption) : 27016;

            if (string.IsNullOrWhiteSpace(ip))
            {
                await command.RespondAsync("Please provide a valid IP address.", ephemeral: true);
                return;
            }

            if (!System.Net.IPAddress.TryParse(ip, out _) && !Uri.CheckHostName(ip).Equals(UriHostNameType.Dns))
            {
                await command.RespondAsync("Please provide a valid IP address or hostname.", ephemeral: true);
                return;
            }

            if (port < 1 || port > 65535)
            {
                await command.RespondAsync("Port must be between 1 and 65535.", ephemeral: true);
                return;
            }

            await command.DeferAsync(ephemeral: true);

            try
            {
                _config = await _configService.LoadOrCreateConfigAsync();

                _config.ServerIp = ip;
                _config.ServerPort = port;

                await _configService.SaveConfigAsync(_config);

                var serverInfo = await _serverService.GetServerInfoAsync();

                string response;
                if (serverInfo.IsOnline)
                {
                    response = $"Server updated successfully!\n" +
                              $"**Server:** {serverInfo.Name}\n" +
                              $"**Address:** {ip}:{port}\n" +
                              $"**Players:** {serverInfo.Players}/{serverInfo.MaxPlayers}";
                }
                else
                {
                    response = $"Server updated, but connection test failed.\n" +
                              $"**Address:** {ip}:{port}\n" +
                              $"The server may be offline or the address may be incorrect.";
                }

                await command.FollowupAsync(response, ephemeral: true);

                Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Server configuration updated by {command.User.Username} to {ip}:{port}");

                string exePath = Environment.ProcessPath;
                System.Diagnostics.Process.Start(exePath);
                Environment.Exit(0);
            }
            catch (Exception ex)
            {
                await command.FollowupAsync($"Failed to save configuration: {ex.Message}", ephemeral: true);
            }
        }
    }
}