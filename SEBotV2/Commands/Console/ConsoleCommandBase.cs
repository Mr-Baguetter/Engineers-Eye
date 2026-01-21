namespace SEBotV2.Commands.Console
{
    public abstract class ConsoleCommandBase
    {
        /// <summary>
        /// Gets the name of the command
        /// </summary>
        public abstract string Name { get; }

        /// <summary>
        /// Gets the description of the command
        /// </summary>
        public virtual string Description { get; } = string.Empty;

        /// <summary>
        /// Gets the visible arguments for the command
        /// </summary>
        public virtual string VisibleArgs => string.Empty;

        /// <summary>
        /// Gets the required arguments for the command
        /// </summary>
        public virtual int RequiredArgsCount => 0;

        public virtual bool Execute(List<string> arguments, out string response)
        {
            response = null!;
            return false;
        }

        public virtual Task<CommandResult> ExecuteAsync(List<string> arguments, CancellationToken ct = default)
        {
            bool success = Execute(arguments, out string response);
            return Task.FromResult(CommandResult.From(success, response));
        }
    }

    public sealed class CommandResult
    {
        public bool Success { get; }
        public string Response { get; }

        public CommandResult(bool success, string response)
        {
            Success = success;
            Response = response ?? string.Empty;
        }

        public static CommandResult From(bool success, string response) => new(success, response);
    }
}