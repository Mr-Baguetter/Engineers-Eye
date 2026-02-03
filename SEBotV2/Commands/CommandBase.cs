using System.Reflection;
using SEBotV2.API.Helpers;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using static SEBotV2.Commands.Choice;

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

        public virtual List<Option> Options => [];

        public virtual List<Choice> Choices => [];

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

        public virtual bool AllowMentions => false;

        public virtual bool OnlyAllowInThaumiel => false;

        public virtual Response Execute(List<string> arguments, ICommandSender sender, Dictionary<string, string> optionValues)
        {
            return Response.Failed("Command not implemented.");
        }

        public virtual async Task<Response> ExecuteAsync(List<string> arguments, ICommandSender sender, Dictionary<string, string> optionValues, CancellationToken ct = default)
        {
            await Task.Yield();
            return Response.Failed("Command not implemented.");
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

        public async Task<SocketGuildUser?> ResolveMentionedUserAsync(ICommandSender sender, List<string> arguments, int userarg)
        {
            if (arguments?.Count > 0)
            {
                string userMention = arguments[userarg];
                return sender.GuildUser.Guild.Users.FirstOrDefault(u => string.Equals(u.Username, userMention, StringComparison.OrdinalIgnoreCase));
            }

            return null;
        }

        public virtual bool IsAsyncImplementation()
        {
            MethodInfo? method = GetType().GetMethod(nameof(ExecuteAsync));
            return method?.DeclaringType != typeof(CommandBase);
        }

        public static Dictionary<uint, Choices> ParseEnumToChoices(Type enumType)
        {
            if (!enumType.IsEnum)
            {
                LogManager.Error($"Type {enumType.Name} is not an enum");
                throw new ArgumentException("Type provided must be an Enum.", nameof(enumType));
            }

            Dictionary<uint, Choices> result = [];

            Array enumValues = Enum.GetValues(enumType);
            string[] enumNames = Enum.GetNames(enumType);

            for (uint i = 0; i < enumValues.Length; i++)
            {
                result[i] = new Choices
                {
                    Name = enumNames[i],
                    Response = enumValues.GetValue(i)?.ToString() ?? "NULL"
                };
            }

            return result;
        }
    }

    public sealed class Response
    {
        public bool Success { get; }
        public string? Content { get; }
        public MessageComponent? Components { get; }

        private Response(bool success, string? content, MessageComponent? components)
        {
            Success = success;
            Content = content;
            Components = components;
        }

        public static Response Succeed(string content)
            => new(true, content, null);

        public static Response Succeed(MessageComponent components)
            => new(true, string.Empty, components);

        public static Response Succeed(string content, MessageComponent components)
            => new(true, content, components);

        public static Response Failed(string content)
            => new(false, content, null);

        public static Response Failed(MessageComponent components)
            => new(false, string.Empty, components);

        public static Response Failed(string content, MessageComponent components)
            => new(false, content, components);
    }

    public sealed class Option
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public bool Required { get; set; }
        public ApplicationCommandOptionType Type { get; set; }
        public string Response { get; set; } = string.Empty;

        public override string ToString()
        {
            return $"{Name}";
        }
    }

    public sealed class Choice
    {
        public class Choices
        {
            public string Name { get; set; } = string.Empty;
            public string Response { get; set; } = string.Empty;
        }

        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public bool Required { get; set; }
        public ApplicationCommandOptionType Type { get; set; }

        public Dictionary<uint, Choices> Values = [];

        public override string ToString()
        {
            return $"{Name}";
        }
    }
}