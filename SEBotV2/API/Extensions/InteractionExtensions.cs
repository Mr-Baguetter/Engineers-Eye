using Discord;
using Discord.WebSocket;
using SEBotV2.Commands;

namespace SEBotV2.API.Extensions
{
    public static class InteractionExtensions
    {
        public static Task SendResultAsync(this SocketInteraction interaction, bool hasResponded, Response result, CommandBase commandBase)
        {
            AllowedMentions allowed = commandBase.AllowMentions ? AllowedMentions.All : AllowedMentions.None;
            if (!hasResponded)
            {
                return interaction.RespondAsync(result.Content, components: result.Components, ephemeral: !result.Success, allowedMentions: allowed);                
            }
            else
                return interaction.FollowupAsync(result.Content, components: result.Components, ephemeral: !result.Success, allowedMentions: allowed);
        }
    }
}