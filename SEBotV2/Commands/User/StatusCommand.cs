using Discord.WebSocket;
using Discord;
using SEBotV2.API.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SEBotV2.API.Interfaces;

namespace SEBotV2.Commands.User
{
    public class StatusCommand : ICommand
    {
        private readonly ServerService _serverService;
        private readonly Config _config;

        public string Name => "status";

        public StatusCommand(ServerService serverService, Config config)
        {
            _serverService = serverService;
            _config = config;
        }

        public SlashCommandProperties Build()
        {
            return new SlashCommandBuilder()
                .WithName(Name)
                .WithDescription("Get current server status")
                .Build();
        }

        public async Task ExecuteAsync(SocketSlashCommand command)
        {
            await command.DeferAsync();

            var serverInfo = await _serverService.GetServerInfoAsync();

            var embed = new EmbedBuilder()
                .WithTitle("Space Engineers Server Status")
                .WithColor(serverInfo.IsOnline ? Color.Green : Color.Red)
                .AddField("Server", serverInfo.Name, true)
                .AddField("Address", $"{_config.ServerIp}:{_config.ServerPort}", true)
                .AddField("Status", serverInfo.IsOnline ? "🟢 Online" : "🔴 Offline", true);

            if (serverInfo.IsOnline)
            {
                embed.AddField("Players", $"{serverInfo.Players}/{serverInfo.MaxPlayers}", true);
            }

            embed.WithTimestamp(DateTimeOffset.Now)
                 .WithFooter("Last updated");

            await command.FollowupAsync(embed: embed.Build());
        }
    }
}
