using Discord.WebSocket;
using System.Threading.Tasks;

namespace MisfitBot2
{
    /// <summary>
    /// Any service class should inherit from this class
    /// </summary>
    public class ServiceBase : ServiceBaseDB
    {
        public async Task SayOnDiscord(BotChannel bChan, string message)
        {
            if (bChan.discordDefaultBotChannel != 0)
            {
                await (Core.Discord.GetChannel(bChan.discordDefaultBotChannel) as ISocketMessageChannel).SendMessageAsync(message);
                return;
            }
        }
        public async Task SayOnDiscord(string message, ulong discordChannel)
        {
            await (Core.Discord.GetChannel(discordChannel) as ISocketMessageChannel).SendMessageAsync(message);
        }
        public async Task SayOnDiscordAdmin(BotChannel bChan, string message)
        {
            if (bChan.discordAdminChannel != 0)
            {
                await (Core.Discord.GetChannel(bChan.discordAdminChannel) as ISocketMessageChannel).SendMessageAsync(message);
                return;
            }
        }
    }
}
