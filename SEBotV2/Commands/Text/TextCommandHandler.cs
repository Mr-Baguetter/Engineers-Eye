using System.Reflection;
using System.Text.RegularExpressions;
using Discord.WebSocket;
using HarmonyLib;
using SEBotV2.API.Helpers;

namespace SEBotV2.Commands.Text
{
    public class TextCommandHandler
    {
        private static ulong OwnerID;
        private static List<TextCommandBase> _commands = [];

        public static async Task Register()
        {
            Bot.Instance._client.MessageReceived += OnMessageReceived;
            OwnerID = await ConfigManager.LoadAsync<ulong?>("OwnerUserId") ?? 1111111111;
        }

        private static async Task OnMessageReceived(SocketMessage msg)
        {
            _ = Task.Run(async () => await HandleMessageRecieved(msg));
            await Task.CompletedTask;
        }

        private static async Task HandleMessageRecieved(SocketMessage msg)
        {
            if (_commands.Count == 0)
                return;

            foreach (TextCommandBase command in _commands.ToArray())
            {
                if (!msg.Content.StartsWith(command.Prefix))
                    continue;

                string withoutPrefix = msg.Content.Substring(command.Prefix.Length).Trim();
                if (string.IsNullOrEmpty(withoutPrefix))
                    continue;

                int firstSpace = withoutPrefix.IndexOf(' ');
                string invokedName = firstSpace == -1  ? withoutPrefix  : withoutPrefix.Substring(0, firstSpace);
                string argsString = firstSpace == -1  ? string.Empty  : withoutPrefix.Substring(firstSpace + 1).Trim();

                if (!string.Equals(invokedName, command.Name, StringComparison.OrdinalIgnoreCase))
                    continue;

                if (command.OnlyAllowOwner && OwnerID != msg.Author.Id)
                    continue;

                List<string> args = ParseArguments(argsString);
                CommandResult result = await command.ExecuteAsync(args, msg.Author);

                if (!string.IsNullOrEmpty(result.Response) || result.Embed != null || result.Component != null)
                    await msg.Channel.SendMessageAsync(result.Response, embed: result.Embed, components: result.Component);

                break;
            }
        }

        private static List<string> ParseArguments(string input)
        {
            List<string> list = [];
            if (string.IsNullOrWhiteSpace(input))
                return list;

            MatchCollection matches = Regex.Matches(input, @"""([^""]+)""|(\S+)");

            foreach (Match m in matches)
            {
                if (m.Groups[1].Success)
                    list.Add(m.Groups[1].Value);
                else if (m.Groups[2].Success)
                    list.Add(m.Groups[2].Value);
            }

            return list;
        }

        /// <summary>
        /// Register a command
        /// </summary>
        public static void RegisterCommand(TextCommandBase command) =>
            _commands.Add(command);

        /// <summary>
        /// Auto-discover and register all commands in the assembly
        /// </summary>
        public static void RegisterAllCommands()
        {
            List<Type> commands = Assembly.GetExecutingAssembly().GetTypes().Where(t => t.IsClass && !t.IsAbstract && typeof(TextCommandBase).IsAssignableFrom(t)).ToList();
            LogManager.Info($"Found {commands.Count} text command types to register");

            int registered = 0;
            int skipped = 0;
            foreach (Type type in commands)
            {
                try
                {
                    ConstructorInfo ctor = AccessTools.Constructor(type, Type.EmptyTypes);

                    if (ctor == null)
                    {
                        LogManager.Warn($"Skipping {type.Name}: no parameterless constructor found.");
                        skipped++;
                        continue;
                    }

                    TextCommandBase instance = (TextCommandBase)ctor.Invoke([]);
                    RegisterCommand(instance);
                    registered++;
                    LogManager.Debug($"Registered text command: {instance.Name}");
                }
                catch (Exception ex)
                {
                    LogManager.Error($"Failed to register text command {type.Name}: {ex.Message}");
                    skipped++;
                }
            }

            LogManager.Info($"Text command registration complete: {registered} registered, {skipped} skipped");
        }
    }
}