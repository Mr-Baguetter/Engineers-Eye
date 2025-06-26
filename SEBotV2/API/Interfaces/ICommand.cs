using Discord;
using Discord.WebSocket;

namespace SEBotV2.API.Interfaces
{
    public interface ICommand
    {
        string Name { get; }
        SlashCommandProperties Build();
        Task ExecuteAsync(SocketSlashCommand command);
    }
}