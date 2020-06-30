using Discord;
using Discord.WebSocket;
using System.Threading.Tasks;

namespace MisfitBot_MKII
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
                await (Program.DiscordClient.GetChannel(bChan.discordDefaultBotChannel) as ISocketMessageChannel).SendMessageAsync(message);
                return;
            }
        }
        public async Task SayOnDiscord(string message, ulong discordChannel)
        {
            await (Program.DiscordClient.GetChannel(discordChannel) as ISocketMessageChannel).SendMessageAsync(message);
        }
        public async Task SayOnDiscordAdmin(BotChannel bChan, string message)
        {
            // check if we are connected to discord first
            if(Program.DiscordClient.Status != UserStatus.Online)
            {
                await Core.LOG(new LogMessage(LogSeverity.Error, "ServiceBase", $"SayOnDiscordAdmin({bChan.discordAdminChannel},  {message}) failed because online check failed."));
                return;
            }

            if (bChan.discordAdminChannel != 0)
            {
                await (Program.DiscordClient.GetChannel(bChan.discordAdminChannel) as ISocketMessageChannel).SendMessageAsync(message);
                return;
            }
        }
        public async Task SayEmbedOnDiscordAdmin(BotChannel bChan, Embed obj)
        {
            if (bChan.discordAdminChannel != 0)
            {
                await (Program.DiscordClient.GetChannel(bChan.discordAdminChannel) as ISocketMessageChannel).SendMessageAsync("", false, obj);
                return;
            }
        }
    }
}
