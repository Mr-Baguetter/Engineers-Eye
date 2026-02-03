using SEBotV2.API.Extensions;
using SEBotV2.API.Helpers;
using Discord;
using Discord.WebSocket;
using HarmonyLib;
using System.Reflection;
using Discord.Commands;

namespace SEBotV2.Commands
{
    /// <summary>
    /// Manages and executes commands
    /// </summary>
    public class CommandHandler
    {
        public static readonly Dictionary<string, CommandBase> Commands = [];
        private readonly Func<SocketSlashCommand, Dictionary<string, bool>> _permissionProvider;
        private const ulong _thaumielGuildID = 1169664869746880682;

        public CommandHandler(Func<SocketSlashCommand, Dictionary<string, bool>> permissionProvider = null)
        {
            _permissionProvider = permissionProvider;
        }

        /// <summary>
        /// Register a command
        /// </summary>
        public void RegisterCommand(CommandBase command) =>
            Commands[command.Name.ToLowerInvariant()] = command;

        /// <summary>
        /// Auto-discover and register all commands in the assembly
        /// </summary>
        public void RegisterAllCommands()
        {
            List<Type> commands = Assembly.GetExecutingAssembly().GetTypes().Where(t => t.IsClass && !t.IsAbstract && typeof(CommandBase).IsAssignableFrom(t)).ToList();
            LogManager.Info($"Found {commands.Count} command types to register");

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
                    RegisterCommand(instance);
                    registered++;
                    LogManager.Debug($"Registered command: {instance.Name}");
                }
                catch (Exception ex)
                {
                    LogManager.Error($"Failed to register command {type.Name}: {ex.Message}");
                    skipped++;
                }
            }

            LogManager.Info($"Command registration complete: {registered} registered, {skipped} skipped");
        }

        public static CommandBase BuildCommand(Type commandType)
        {
            ConstructorInfo ctor = AccessTools.Constructor(commandType, Type.EmptyTypes);

            if (ctor == null)
                LogManager.Warn($"Skipping {commandType.Name}: no parameterless constructor found.");

            CommandBase instance = (CommandBase)ctor.Invoke([]);
            return instance;
        }

        /// <summary>
        /// Execute a command from a Discord slash command
        /// </summary>
        public async Task<bool> ExecuteAsync(SocketSlashCommand command)
        {
            string commandName = command.Data.Name;
            if (!Commands.TryGetValue(commandName.ToLowerInvariant(), out CommandBase? cmd))
            {
                await command.RespondAsync($"Unknown command: `{commandName}`", ephemeral: true);
                return false;
            }

            if (command.GuildId == null && cmd.ContextType == ContextType.Guild)
            {
                await command.RespondAsync("This command can only be used in Guilds.", ephemeral: true);
                return false;
            }

            if (command.GuildId != null && (cmd.ContextType == ContextType.DM || cmd.ContextType == ContextType.Group))
            {
                await command.RespondAsync("This command can only be used in DMs.", ephemeral: true);
                return false;
            }

            var permissions = _permissionProvider?.Invoke(command) ?? [];
            DiscordCommandSender sender = new(command, permissions);

            CommandBase targetCommand = cmd;
            List<string> arguments = [];
            Dictionary<string, string> optionValues = [];

            if (command.Data.Options.Count > 0)
            {
                foreach (SocketSlashCommandDataOption opt in command.Data.Options)
                {
                    string value = GetOptionValue(opt);
                    arguments.Add(value);
                    optionValues[opt.Name] = value;
                }
                
                foreach (Option option in targetCommand.Options)
                {
                    if (optionValues.TryGetValue(option.Name, out string? value))
                        option.Response = value;
                }
            }

            if (!sender.HasPermission(targetCommand.RequiredPermission))
            {
                await command.RespondAsync($"You don't have permission to use this command.", ephemeral: true);
                return false;
            }

            if (arguments.Count < targetCommand.RequiredArgsCount)
            {
                await command.RespondAsync($"Missing arguments. Required: {targetCommand.RequiredArgsCount}, Provided: {arguments.Count}\n**Expected:** {string.Join(" ,", targetCommand.Options)} {string.Join(" ,", targetCommand.Choices)}", ephemeral: true);
                return false;
            }

            if (targetCommand.ShouldDefer)
            {
                await command.DeferAsync(ephemeral: targetCommand.DeferEphemeral);
                LogManager.Debug($"Deferred response for command: {commandName}");
            }

            try
            {
                Response result;

                if (targetCommand.IsAsyncImplementation())
                {
                    result = await targetCommand.ExecuteAsync(arguments, sender, optionValues, CancellationToken.None);
                }
                else
                    result = targetCommand.Execute(arguments, sender, optionValues);

                await command.SendResultAsync(command.HasResponded, result, Commands[command.CommandName.ToLower()]);
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

        public static void ParseOptionsAndChoices(CommandBase command, SlashCommandBuilder builder)
        {
            foreach (Option option in command.Options)
            {
                if (option.Type is ApplicationCommandOptionType.SubCommand or ApplicationCommandOptionType.SubCommandGroup)
                {
                    LogManager.Warn($"SubCommand or SubCommandGroup OptionTypes are not supported!");
                    continue;
                }

                builder.AddOption(option.Name.ToLowerInvariant().Replace(' ', '_'), option.Type, option.Description, option.Required);
            }

            foreach (Choice choice in command.Choices)
            {
                if (choice.Type is ApplicationCommandOptionType.SubCommand or ApplicationCommandOptionType.SubCommandGroup)
                {
                    LogManager.Warn($"SubCommand or SubCommandGroup OptionTypes are not supported!");
                    continue;
                }

                SlashCommandOptionBuilder choicebuilder = new()
                {
                    Name = choice.Name.ToLowerInvariant().Replace(' ', '_'),
                    Description = choice.Description,
                    IsRequired = choice.Required,
                    Type = choice.Type
                };

                foreach (var kvp in choice.Values)
                    choicebuilder.AddChoice(kvp.Value.Name.ToLowerInvariant().Replace(' ', '_'), kvp.Key.ToString());

                builder.AddOption(choicebuilder);
                    
            }
        }

        /// <summary>
        /// Build slash command properties for Discord registration
        /// </summary>
        public SlashCommandProperties BuildSlashCommand(CommandBase command)
        {
            SlashCommandBuilder builder = new SlashCommandBuilder().WithName(command.Name.ToLowerInvariant()).WithDescription(command.Description);
            ParseOptionsAndChoices(command, builder);
            return builder.Build();
        }

        public async Task RegisterCommandsWithDiscordAsync()
        {
            LogManager.Info($"Registering {Commands.Count} commands with Discord...");

            foreach (SocketGuild guild in Bot.Instance._client.Guilds)
            {
                try
                {
                    LogManager.Debug($"Processing guild {guild.Name} ({guild.Id})");
                    SlashCommandProperties[] propsForGuild = Commands.Values.Where(c => !c.OnlyAllowInThaumiel || guild.Id == _thaumielGuildID).Select(BuildSlashCommand).ToArray();
                    LogManager.Debug($"Registering {propsForGuild.Length} commands for {guild.Name}");
                    await Bot.Instance._client.Rest.BulkOverwriteGuildCommands(propsForGuild, guild.Id);
                    LogManager.Info($"Successfully registered {propsForGuild.Length} slash commands for guild {guild.Name}");
                }
                catch (Exception ex)
                {
                    LogManager.Error($"Failed to register commands for guild {guild.Name}: {ex}");
                }
            }
        }

        /// <summary>
        /// Get all registered commands
        /// </summary>
        public IEnumerable<CommandBase> GetCommands() => Commands.Values;
    }
}