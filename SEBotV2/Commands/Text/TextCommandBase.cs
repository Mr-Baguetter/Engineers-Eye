using Discord;
using Discord.Commands;
using Discord.WebSocket;

namespace SEBotV2.Commands.Text
{
    public abstract class TextCommandBase
    {
        /// <summary>
        /// Gets the name of the command
        /// </summary>
        public abstract string Name { get; }

        /// <summary>
        /// If true only the bot owner can run this
        /// </summary>
        public abstract bool OnlyAllowOwner { get; }

        /// <summary>
        /// The prefix to use for this command. Base is !
        /// </summary>
        public virtual string Prefix => "!";

        /// <summary>
        /// Gets the required permission (<see cref="GuildPermission"/>) for the command
        /// </summary>
        public virtual GuildPermission RequiredPermission => GuildPermission.SendMessages;

        /// <summary>
        /// Gets the <see cref="Discord.Commands.ContextType"/> for the command
        /// </summary>
        public virtual ContextType ContextType => ContextType.Guild;

        /// <summary>
        /// Whether this command should automatically defer its response (for long-running operations)
        /// </summary>
        public virtual bool ShouldDefer => false;

        /// <summary>
        /// Whether the deferred response should be ephemeral
        /// </summary>
        public virtual bool DeferEphemeral => false;

        public virtual bool Execute(List<string> arguments, SocketUser sender, out string response, out Embed embed, out MessageComponent componentv2)
        {
            response = null!;
            embed = null!;
            componentv2 = null!;
            return false;
        }

        public virtual Task<CommandResult> ExecuteAsync(List<string> arguments, SocketUser sender, CancellationToken ct = default)
        {
            bool success = Execute(arguments, sender, out string response, out Embed embed, out MessageComponent componentv2);
            return Task.FromResult(CommandResult.From(success, response, embed, componentv2));
        }
    }

    public sealed class CommandResult
    {
        public bool Success { get; }
        public string Response { get; }
        public Embed? Embed { get; }
        public MessageComponent? Component { get; }

        public CommandResult(bool success, string response, Embed? embed = null, MessageComponent? component = null)
        {
            Success = success;
            Response = response ?? string.Empty;
            Embed = embed;
            Component = component;
        }

        public static CommandResult From(bool success, string response, Embed? embed = null, MessageComponent? component = null) => new(success, response, embed, component);
    }
}