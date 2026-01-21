using SEBotV2.API.Helpers;
using Discord;
using Discord.WebSocket;
using System.Reflection;
using HarmonyLib;

namespace SEBotV2.Commands
{
    /// <summary>
    /// Manages and executes commands
    /// </summary>
    public class CommandHandler
    {
        private readonly Dictionary<string, CommandBase> _commands = [];
        private readonly Func<SocketSlashCommand, Dictionary<string, bool>> _permissionProvider;

        public CommandHandler(Func<SocketSlashCommand, Dictionary<string, bool>> permissionProvider = null)
        {
            _permissionProvider = permissionProvider;
        }

        /// <summary>
        /// Register a command
        /// </summary>
        public void AddCommand(CommandBase command) =>
            _commands[command.Name.ToLowerInvariant()] = command;

        /// <summary>
        /// Auto-discover and register all commands in the assembly
        /// </summary>
        public void GetAndAddAllCommands()
        {
            List<Type> commands = Assembly.GetExecutingAssembly().GetTypes().Where(t => t.IsClass && !t.IsAbstract && typeof(CommandBase).IsAssignableFrom(t)).ToList();
            LogManager.Info($"Found {commands.Count} slash command types to add");

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

                    CommandBase instance = (CommandBase)ctor.Invoke([]);
                    AddCommand(instance);
                    registered++;
                    LogManager.Debug($"Registered slash command: {instance.Name}");
                }
                catch (Exception ex)
                {
                    LogManager.Error($"Failed to register coslash mmand {type.Name}: {ex.Message}");
                    skipped++;
                }
            }

            LogManager.Info($"Slash command registration complete: {registered} added, {skipped} skipped");
        }

        /// <summary>
        /// Execute a command from a Discord slash command
        /// </summary>
        public async Task<bool> ExecuteAsync(SocketSlashCommand command)
        {
            string commandName = command.Data.Name;
            if (!_commands.TryGetValue(commandName.ToLowerInvariant(), out CommandBase? cmd))
            {
                await command.RespondAsync($"Unknown command: `{commandName}`", ephemeral: true);
                return false;
            }

            if (command.GuildId == null && cmd.ContextType == Discord.Commands.ContextType.Guild)
            {
                await command.RespondAsync("This command can only be used in Guilds.", ephemeral: true);
                return false;
            }

            if (command.GuildId != null && (cmd.ContextType == Discord.Commands.ContextType.DM || cmd.ContextType == Discord.Commands.ContextType.Group))
            {
                await command.RespondAsync("This command can only be used in DMs.", ephemeral: true);
                return false;
            }

            var permissions = _permissionProvider?.Invoke(command) ?? [];
            DiscordCommandSender sender = new(command, permissions);

            CommandBase targetCommand = cmd;
            List<string> arguments = [];

            if (command.Data.Options.Count > 0)
            {
                SocketSlashCommandDataOption topOption = command.Data.Options.First();

                if (topOption.Type == ApplicationCommandOptionType.SubCommand)
                {
                    if (cmd.Subcommands.TryGetValue(topOption.Name, out CommandBase? sub))
                        targetCommand = sub;
                    else
                    {
                        await command.RespondAsync($"Unknown subcommand: `{topOption.Name}`", ephemeral: true);
                        return false;
                    }

                    foreach (SocketSlashCommandDataOption opt in topOption.Options)
                        arguments.Add(GetOptionValue(opt));
                }
                else if (topOption.Type == ApplicationCommandOptionType.SubCommandGroup)
                {
                    if (topOption.Options.Count == 0)
                    {
                        await command.RespondAsync($"No subcommand provided for group: `{topOption.Name}`", ephemeral: true);
                        return false;
                    }

                    SocketSlashCommandDataOption subOption = topOption.Options.First();
                    if (cmd.Subcommands.TryGetValue(subOption.Name, out CommandBase? sub))
                        targetCommand = sub;
                    else
                    {
                        await command.RespondAsync($"Unknown subcommand: `{subOption.Name}`", ephemeral: true);
                        return false;
                    }

                    foreach (SocketSlashCommandDataOption opt in subOption.Options)
                        arguments.Add(GetOptionValue(opt));
                }
                else
                {
                    foreach (SocketSlashCommandDataOption opt in command.Data.Options)
                        arguments.Add(GetOptionValue(opt));
                }
            }

            if (!sender.HasPermission(targetCommand.RequiredPermission))
            {
                await command.RespondAsync($"You don't have permission to use this command.", ephemeral: true);
                return false;
            }

            if (arguments.Count < targetCommand.RequiredArgsCount)
            {
                await command.RespondAsync($"Missing arguments. Required: {targetCommand.RequiredArgsCount}, Provided: {arguments.Count}\n**Expected:** {targetCommand.VisibleArgs}", ephemeral: true);
                return false;
            }

            if (targetCommand.ShouldDefer)
            {
                await command.DeferAsync(ephemeral: targetCommand.DeferEphemeral);
                LogManager.Debug($"Deferred response for command: {commandName}");
            }

            try
            {
                CommandResult result = await targetCommand.ExecuteAsync(arguments, sender, CancellationToken.None);

                if (!command.HasResponded)
                {
                    if (result.Embed != null)
                        await command.RespondAsync(result.Response, embed: result.Embed, ephemeral: !result.Success);
                    else if (result.Component != null)
                        await command.RespondAsync(result.Response, components: result.Component, ephemeral: !result.Success);
                    else
                        await command.RespondAsync(result.Response, ephemeral: !result.Success);
                }
                else
                {
                    if (result.Embed != null)
                        await command.FollowupAsync(result.Response, embed: result.Embed, ephemeral: !result.Success);
                    else if (result.Component != null)
                        await command.FollowupAsync(result.Response, components: result.Component, ephemeral: !result.Success);
                    else
                        await command.FollowupAsync(result.Response, ephemeral: !result.Success);
                }

                return result.Success;
            }
            catch (Exception ex)
            {
                LogManager.Error($"Error executing command {commandName}: {ex.Message}");
                string errorMsg = $"Error executing command: {ex.Message}";

                if (!command.HasResponded)
                    await command.RespondAsync(errorMsg, ephemeral: true);
                else
                    await command.FollowupAsync(errorMsg, ephemeral: true);

                return false;
            }
        }

        /// <summary>
        /// Helper to extract option value as string (supports attachment URL)
        /// </summary>
        private string GetOptionValue(SocketSlashCommandDataOption option)
        {
            try
            {
                if (option.Type == ApplicationCommandOptionType.Attachment && option.Value != null)
                {
                    Type? valType = option.Value.GetType();
                    PropertyInfo? urlProp = valType.GetProperty("Url") ?? valType.GetProperty("url");
                    if (urlProp != null)
                        return urlProp.GetValue(option.Value)?.ToString() ?? option.Value.ToString();
                }

                return option.Value?.ToString() ?? string.Empty;
            }
            catch
            {
                return option.Value?.ToString() ?? string.Empty;
            }
        }


        /// <summary>
        /// Parse argument type from format string (e.g., "%image%argname" or "argname")
        /// Provides sensible default when name is omitted (e.g. "%attachment%" -> "file")
        /// </summary>
        private (ApplicationCommandOptionType type, string name) ParseArgumentType(string arg)
        {
            arg = (arg ?? string.Empty).Trim();
            if (arg.StartsWith("%") && arg.IndexOf('%', 1) != -1)
            {
                int endIndex = arg.IndexOf('%', 1);
                string typeStr = arg.Substring(1, endIndex - 1).ToLowerInvariant();
                string argName = (endIndex + 1 < arg.Length) ? arg.Substring(endIndex + 1).Trim() : string.Empty;
                if (string.IsNullOrWhiteSpace(argName))
                {
                    argName = typeStr switch
                    {
                        "image" or "attachment" or "file" => "file",
                        "user" or "member" => "user",
                        "channel" => "channel",
                        "role" => "role",
                        "integer" or "int" or "number" => "number",
                        "boolean" or "bool" => "flag",
                        "mentionable" => "mentionable",
                        _ => "arg"
                    };
                }

                return typeStr switch
                {
                    "image" or "attachment" or "file" => (ApplicationCommandOptionType.Attachment, argName),
                    "user" or "member" => (ApplicationCommandOptionType.User, argName),
                    "channel" => (ApplicationCommandOptionType.Channel, argName),
                    "role" => (ApplicationCommandOptionType.Role, argName),
                    "integer" or "int" or "number" => (ApplicationCommandOptionType.Integer, argName),
                    "boolean" or "bool" => (ApplicationCommandOptionType.Boolean, argName),
                    "mentionable" => (ApplicationCommandOptionType.Mentionable, argName),
                    _ => (ApplicationCommandOptionType.String, argName)
                };
            }

            if (string.IsNullOrWhiteSpace(arg))
                arg = "arg";

            return (ApplicationCommandOptionType.String, arg);
        }

        /// <summary>
        /// Build slash command properties for Discord registration
        /// </summary>
        public SlashCommandProperties BuildSlashCommand(CommandBase command)
        {
            SlashCommandBuilder builder = new SlashCommandBuilder().WithName(command.Name.ToLowerInvariant()).WithDescription(command.Description);
            if (command.Subcommands.Count > 0)
            {
                foreach (CommandBase sub in command.Subcommands.Values)
                {
                    SlashCommandOptionBuilder subBuilder = new SlashCommandOptionBuilder().WithName(sub.Name.ToLowerInvariant()).WithType(ApplicationCommandOptionType.SubCommand).WithDescription(sub.Description);

                    if (!string.IsNullOrWhiteSpace(sub.VisibleArgs))
                    {
                        string[]? args = sub.VisibleArgs.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(a => a.Trim()).ToArray();
                        for (int i = 0; i < args.Length; i++)
                        {
                            (ApplicationCommandOptionType optionType, string argName) = ParseArgumentType(args[i]);
                            string optionName = (argName ?? string.Empty).ToLowerInvariant();
                            if (string.IsNullOrWhiteSpace(optionName)) optionName = $"arg{i + 1}";
                            subBuilder.AddOption(optionName, optionType, $"The {argName}", isRequired: i < sub.RequiredArgsCount);
                        }
                    }

                    builder.AddOption(subBuilder);
                }
            }
            else
            {
                if (!string.IsNullOrWhiteSpace(command.VisibleArgs))
                {
                    string[] args = command.VisibleArgs.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(a => a.Trim()).ToArray();
                    for (int i = 0; i < args.Length; i++)
                    {
                        (ApplicationCommandOptionType optionType, string argName) = ParseArgumentType(args[i]);
                        string optionName = (argName ?? string.Empty).ToLowerInvariant();
                        if (string.IsNullOrWhiteSpace(optionName)) optionName = $"arg{i + 1}";
                        builder.AddOption(optionName, optionType, $"The {argName}", isRequired: i < command.RequiredArgsCount);
                    }
                }
            }

            return builder.Build();
        }

        public async Task RegisterCommandsWithDiscordAsync(ulong guildID)
        {
            LogManager.Info($"Registering {_commands.Count} commands with Discord...");
            ApplicationCommandProperties[] commandProps = _commands.Values.Select(cmd => BuildSlashCommand(cmd)).ToArray();

            foreach (DiscordSocketClient shard in Bot.Instance._client.Shards)
            {
                try
                {
                    var registered = await shard.Rest.BulkOverwriteGuildCommands(commandProps, guildID);
                    LogManager.Info($"Shard {shard.ShardId}: Registered {registered.Count} commands.");
                }
                catch (Exception ex)
                {
                    LogManager.Error($"Shard {shard.ShardId}: Failed to register commands: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Get all registered commands
        /// </summary>
        public IEnumerable<CommandBase> GetCommands() => _commands.Values;
    }
}