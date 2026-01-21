using Discord;
using Discord.Commands;
using SEBotV2.API.Helpers;

namespace SEBotV2.Commands
{
    public abstract class CommandBase
    {
        /// <summary>
        /// Gets the name of the command
        /// </summary>
        public abstract string Name { get; }

        /// <summary>
        /// Gets the description of the command
        /// </summary>
        public abstract string Description { get; }

        /// <summary>
        /// Gets the visible arguments for the command
        /// </summary>
        public virtual string VisibleArgs => string.Empty;

        /// <summary>
        /// Gets the required arguments for the command
        /// </summary>
        public virtual int RequiredArgsCount => 0;

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

        /// <summary>
        /// Subcommands registered under this command. Key is the subcommand name (lowercase).
        /// </summary>
        public Dictionary<string, CommandBase> Subcommands { get; } = new();

        /// <summary>
        /// The parent command if this is a subcommand, null otherwise.
        /// </summary>
        public CommandBase? Parent { get; private set; }

        /// <summary>
        /// The subcommand group name if this subcommand belongs to a group, null otherwise.
        /// </summary>
        public virtual string? SubcommandGroup => null;

        /// <summary>
        /// Register a subcommand under this command
        /// </summary>
        /// <param name="subcommand">The subcommand to register</param>
        public void RegisterSubcommand(CommandBase subcommand)
        {
            string key = subcommand.Name.ToLowerInvariant();
            
            if (Subcommands.ContainsKey(key))
            {
                LogManager.Warn($"Subcommand '{key}' is already registered under '{Name}'. Overwriting.");
            }
            
            subcommand.Parent = this;
            Subcommands[key] = subcommand;
            LogManager.Debug($"Registered subcommand '{subcommand.Name}' under '{Name}'");
        }

        /// <summary>
        /// Register multiple subcommands at once
        /// </summary>
        public void RegisterSubcommands(params CommandBase[] subcommands)
        {
            foreach (CommandBase subcommand in subcommands)
            {
                RegisterSubcommand(subcommand);
            }
        }

        /// <summary>
        /// Get a subcommand by name (case-insensitive)
        /// </summary>
        public CommandBase? GetSubcommand(string name) =>
            Subcommands.TryGetValue(name.ToLowerInvariant(), out CommandBase? subcommand) ? subcommand : null;

        /// <summary>
        /// Check if this command has subcommands
        /// </summary>
        public bool HasSubcommands => Subcommands.Count > 0;

        /// <summary>
        /// Check if this command is a subcommand
        /// </summary>
        public bool IsSubcommand => Parent != null;

        public virtual bool Execute(List<string> arguments, ICommandSender sender, out string response, out Embed embed, out MessageComponent componentv2)
        {
            response = null!;
            embed = null!;
            componentv2 = null!;
            return false;
        }
        
        public virtual Task<CommandResult> ExecuteAsync(List<string> arguments, ICommandSender sender, CancellationToken ct = default)
        {
            bool success = Execute(arguments, sender, out string response, out Embed embed, out MessageComponent componentv2);
            return Task.FromResult(CommandResult.From(success, response, embed, componentv2));
        }
        
        /// <summary>
        /// Helper method for commands that need to manually defer
        /// </summary>
        protected async Task DeferIfNeeded(ICommandSender sender, bool ephemeral = false)
        {
            if (!sender.HasDeferred)
            {
                await sender.DeferAsync(ephemeral);
            }
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