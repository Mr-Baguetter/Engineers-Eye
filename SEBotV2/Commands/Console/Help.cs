using System.Text;

namespace SEBotV2.Commands.Console
{
    public class Help : ConsoleCommandBase
    {
        public override string Name => "Help";
        public override string Description => "Prints all the console commands";
        public override async Task<CommandResult> ExecuteAsync(List<string> arguments, CancellationToken ct = default)
        {
            StringBuilder sb = new();
            foreach (ConsoleCommandBase command in Bot.Instance.ConsoleCommandHandler.commands)
                sb.AppendLine($"{command.Name} - {command.Description}");

            return CommandResult.From(true, sb.ToString());
        }
    }
}