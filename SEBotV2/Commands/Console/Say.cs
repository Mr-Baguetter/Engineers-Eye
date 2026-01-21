using System.Text;
using Discord.WebSocket;
using SEBotV2.API.Helpers;

namespace SEBotV2.Commands.Console
{
    public class Say : ConsoleCommandBase
    {
        public override string Name => "Say";
        public override string Description => "Prints the inputed text in a Guild Channel";
        public override async Task<CommandResult> ExecuteAsync(List<string> arguments, CancellationToken ct = default)
        {
            if (arguments.Count < 3)
                return CommandResult.From(false, "Usage: say <guildId> <channelId> <message>");

            StringBuilder sb = new();
            if (!ulong.TryParse(arguments[0], out ulong guildId))
                return CommandResult.From(false, "Invalid guild ID");
            
            if (!ulong.TryParse(arguments[1], out ulong channelId))
                return CommandResult.From(false, "Invalid channel ID");
            
            string message = string.Join(" ", arguments.Skip(2));
            try
            {
                SocketGuild guild = Bot.Instance._client.GetGuild(guildId);
                if (guild == null)
                    return CommandResult.From(false, $"Guild {guildId} not found");
                
                SocketTextChannel channel = guild.GetTextChannel(channelId);
                if (channel == null)
                    return CommandResult.From(false, $"Channel {channelId} not found in guild {guild.Name}");
                
                await channel.SendMessageAsync(message);
                sb.AppendLine($"Message sent to #{channel.Name} in {guild.Name}");
                sb.AppendLine($"Content: {message}");
                
                return CommandResult.From(true, sb.ToString());
            }
            catch (Exception ex)
            {
                LogManager.Error($"Error sending message: {ex.Message}");
                return CommandResult.From(false, $"Error: {ex.Message}");
            }
        }
    }
}