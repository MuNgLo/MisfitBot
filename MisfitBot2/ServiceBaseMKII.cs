using Discord;
using Discord.WebSocket;
using System.Threading.Tasks;
using MisfitBot2.Components;

namespace MisfitBot2
{
    /// <summary>
    /// Any service class should inherit from this class
    /// UPDATED version
    /// </summary>
    public abstract class ServiceBaseMKII : ServiceBaseDB
    {
        public DatabaseStrings DBStrings;

        public ServiceBaseMKII(string pluginName)
        {
            DBStrings = new DatabaseStrings(pluginName);
        }

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
        public async Task SayEmbedOnDiscordAdmin(BotChannel bChan, Embed obj)
        {
            if (bChan.discordAdminChannel != 0)
            {
                await (Core.Discord.GetChannel(bChan.discordAdminChannel) as ISocketMessageChannel).SendMessageAsync("", false, obj);
                return;
            }
        }

        public abstract Task<object> Settings(BotChannel bChan);
        public abstract void RegisterEventListeners();
    }
}
