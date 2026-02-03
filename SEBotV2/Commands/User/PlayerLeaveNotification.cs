using Discord;
using Discord.Commands;
using SEBotV2.API.Helpers;

namespace SEBotV2.Commands.User
{
    public class PlayerLeaveNotification : CommandBase
    {
        public override string Name => "PlayerLeaveNotification";
        public override ContextType ContextType => ContextType.Guild;
        public override string Description => "Notifies you if a player leaves the server this Guild is monitoring";
        public override GuildPermission RequiredPermission => GuildPermission.SendMessages;
        public override bool ShouldDefer => true;

        public override async Task<Response> ExecuteAsync(List<string> arguments, ICommandSender sender, Dictionary<string, string> optionValues, CancellationToken ct = default)
        {
            ContainerBuilder container = new();
            if (PlayerLeaveNotificationManager.ActiveUsers.ContainsKey(sender.GuildUser.Id))
            {
                if (PlayerLeaveNotificationManager.ActiveUsers[sender.GuildUser.Id].Item1 == sender.GuildUser.Guild.Id)
                {
                    PlayerLeaveNotificationManager.ActiveUsers.Remove(sender.GuildUser.Id);

                    container.AddComponent(new TextDisplayBuilder(TranslationManager.Get("Player Leave Notification Header")));
                    container.AddComponent(new TextDisplayBuilder(TranslationManager.Get("Player Leave Notification Stop")));

                    return Response.Succeed(new ComponentBuilderV2(container).Build());
                }
                else
                {
                    PlayerLeaveNotificationManager.ActiveUsers[sender.GuildUser.Id] = (sender.GuildUser.Guild.Id, DateTime.Now);

                    container.AddComponent(new TextDisplayBuilder(TranslationManager.Get("Player Leave Notification Header")));
                    container.AddComponent(new TextDisplayBuilder(TranslationManager.Get("Player Leave Notification Update").Replace("{ServerName}", ServerManager.ServerInfoByGuildId[sender.GuildUser.Guild.Id].Name)));

                    return Response.Succeed(new ComponentBuilderV2(container).Build());
                }
            }
            else
            {
                PlayerLeaveNotificationManager.ActiveUsers[sender.GuildUser.Id] = (sender.GuildUser.Guild.Id, DateTime.Now);

                container.AddComponent(new TextDisplayBuilder(TranslationManager.Get("Player Leave Notification Header")));
                container.AddComponent(new TextDisplayBuilder(TranslationManager.Get("Player Leave Notification Start").Replace("{ServerName}", ServerManager.ServerInfoByGuildId[sender.GuildUser.Guild.Id].Name)));

                return Response.Succeed(new ComponentBuilderV2(container).Build());
            }
        }
    }
}