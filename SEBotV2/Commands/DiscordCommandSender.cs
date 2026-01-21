using Discord;
using Discord.WebSocket;

namespace SEBotV2.Commands
{
    public class DiscordCommandSender : ICommandSender
    {
        private readonly SocketSlashCommand _command;

        public SocketUser User => _command.User;
        public SocketGuildUser GuildUser => _command.User as SocketGuildUser;
        public ISocketMessageChannel Channel => _command.Channel;
        public bool HasDeferred => _command.HasResponded;

        public DiscordCommandSender(SocketSlashCommand command, Dictionary<string, bool> permissions = null)
        {
            _command = command;
        }

        public bool HasPermission(GuildPermission permission)
        {
            if (GuildUser?.GuildPermissions.Has(permission) == true)
                return true;

            return false;
        }

        public async Task DeferAsync(bool ephemeral = false)
        {
            if (!_command.HasResponded)
            {
                await _command.DeferAsync(ephemeral: ephemeral);
            }
        }

        public async Task RespondAsync(string message, bool ephemeral = false)
        {
            if (_command.HasResponded)
                await _command.FollowupAsync(message, ephemeral: ephemeral);
            else
                await _command.RespondAsync(message, ephemeral: ephemeral);
        }

        public async Task RespondAsync(string message, Embed embed, bool ephemeral = false)
        {
            if (_command.HasResponded)
                await _command.FollowupAsync(message, embeds: [embed], ephemeral: ephemeral);
            else
                await _command.RespondAsync(message, embeds: [embed], ephemeral: ephemeral);
        }
        
        public async Task RespondAsync(string message, MessageComponent component, bool ephemeral = false)
        {
            if (_command.HasResponded)
                await _command.FollowupAsync(message, components: component, ephemeral: ephemeral);
            else
                await _command.RespondAsync(message, components: component, ephemeral: ephemeral);
        }
    }
}