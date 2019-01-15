using Discord.WebSocket;
using MisfitBot2.Extensions.ChannelManager;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MisfitBot2.Plugins.Greeter;
using Discord.Commands;

namespace MisfitBot2.Services
{
    public class GreeterService
    {
        public readonly string PLUGINNAME = "Greeter";
        public GreeterService()
        {
            Core.Discord.UserJoined += DiscordUserJoined;
        }

        private async Task DiscordUserJoined(Discord.WebSocket.SocketGuildUser arg)
        {
                BotChannel bChan = await Core.Channels.GetDiscordGuildbyID(arg.Guild.Id);
                if (bChan == null)
                {
                    return;
                }
                GreeterSettings settings = await Settings(bChan);
                ulong chanID = settings._greetChannel;
                if(chanID != 0)
                {
                    await (Core.Discord.GetChannel(chanID) as ISocketMessageChannel).SendMessageAsync($"{arg.Username} {settings._greetMessage}");
                }
        }

        internal Task DebugAssignGreetChannel(ICommandContext context)
        {
            throw new NotImplementedException();
        }

        internal Task AssignGreetChannel(ICommandContext context)
        {
            throw new NotImplementedException();
        }

        private async Task<GreeterSettings> Settings(BotChannel bChan)
        {
            GreeterSettings settings = new GreeterSettings();
            return await Core.Configs.GetConfig(bChan, PLUGINNAME, settings) as GreeterSettings;
        }
    }
}
