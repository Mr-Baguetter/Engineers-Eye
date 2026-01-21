using Discord;
using Discord.WebSocket;

namespace SEBotV2.Commands
{
    /// <summary>
    /// Wrapper interface for Discord users as command senders
    /// </summary>
    public interface ICommandSender
    {
        /// <summary>
        /// The Discord socket user
        /// </summary>
        SocketUser User { get; }

        /// <summary>
        /// The guild member (null if in DM)
        /// </summary>
        SocketGuildUser GuildUser { get; }

        /// <summary>
        /// Check if the sender has a specific permission
        /// </summary>
        bool HasPermission(GuildPermission permission);

        /// <summary>
        /// Send a response message
        /// </summary>
        Task RespondAsync(string message, bool ephemeral = false);

        /// <summary>
        /// The channel where the command was executed
        /// </summary>
        ISocketMessageChannel Channel { get; }

        /// <summary>
        /// Whether the command has been deferred
        /// </summary>
        bool HasDeferred { get; }

        /// <summary>
        /// Defer the command response
        /// </summary>
        Task DeferAsync(bool ephemeral = false);
    }
}