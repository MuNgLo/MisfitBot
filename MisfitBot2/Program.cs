using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using MisfitBot2.Modules;
using MisfitBot2.Services;
using System;
using System.Data.SQLite;
using System.Reflection;
using System.Threading.Tasks;
using MisfitBot2.Extensions.ChannelManager;

namespace MisfitBot2
{
    public class Program
    {
        public static readonly char _commandCharacter = '!';

        private static IServiceProvider _services;
        // Keep the CommandService and IServiceCollection around for use with commands.
        // These two types require you install the Discord.Net.Commands package.
        private static IServiceCollection _map = new ServiceCollection();
        public static CommandService _commands = new CommandService();

        private static DiscordSocketClient _DiscordClient;
        public static void Main(string[] args=null)
            => new Program().MainAsync().GetAwaiter().GetResult();

        public async Task MainAsync()
        {
            Core.Configs = new ConfigurationHandler();
            Core.LastLaunchTime = Core.CurrentTime;
            Core.Timers = new TimerStuff();
            Core.LOGGER = new JuansLog();
            Core.LOG = Core.LOGGER.LogThis;
            TimerStuff.OnSecondTick += Core.LOGGER.UpdateScreen;
            Core.serializer.Formatting = Newtonsoft.Json.Formatting.Indented;

            CreateNewDatabase();
            ConnectToDatabase();
            //createTable();
            //fillTable();

            DiscordSocketConfig disConfig = new DiscordSocketConfig
            {
                LogLevel = LogSeverity.Info
            };

            _DiscordClient = new DiscordSocketClient(disConfig);
            _DiscordClient.Log += Core.LOGGER.LogThis;

            _DiscordClient.Ready += () => { Core.LOG(new LogMessage(LogSeverity.Info, "Misfitbot", "Connected and ready to be used.")); return Task.CompletedTask; };
            Core.Discord = _DiscordClient;
            string token = DiscordSettings.TOKEN; // Remember to keep this private!

            // Centralize the logic for commands into a seperate method.
            await InitCommands();

            await _DiscordClient.LoginAsync(TokenType.Bot, token);
            await _DiscordClient.StartAsync();

            // Block this task until the program is closed.
            await Task.Delay(-1);
        }

        #region SQLite stuff
        // Creates an empty database file
        void CreateNewDatabase()
        {
            if (System.IO.File.Exists("JuanMemory.sqlite")) {
                Core.LOG(new LogMessage(LogSeverity.Info, "Misfitbot", "Database found."));
            } else
            {
                Core.LOG(new LogMessage(LogSeverity.Warning, "Misfitbot", "Database not found. Creating a fresh victim."));
                SQLiteConnection.CreateFile("JuanMemory.sqlite");
            }
        }
        // Creates a connection with our database file.
        void ConnectToDatabase()
        {
            Core.Data = new SQLiteConnection("Data Source=JuanMemory.sqlite;Version=3;");
            Core.Data.Open();
        }
        #endregion

        public static async void DiscordReconnect()
        {
            await _DiscordClient.StartAsync();
        }

        public async Task InitCommands()
        {
            //Console.WriteLine($":DW:[InitCommands]");
            // Repeat this for all the service classes
            // and other dependencies that your commands might need.
            _map.AddSingleton(new TwitchService()); // Make sure this loads first
            _map.AddSingleton(new UserManagerService());
            _map.AddSingleton(new ChannelManager());
            _map.AddSingleton(new MyPickService());
            _map.AddSingleton(new TreasureService());
            _map.AddSingleton(new AdminService());
            _map.AddSingleton(new BettingService());
            _map.AddSingleton(new DeathCounterService());
            _map.AddSingleton(new VotingService());
            _map.AddSingleton(new RaffleService());
            _map.AddSingleton(new TwitchCommandsService());
            _map.AddSingleton(new PoorLifeChoicesService());
            _map.AddSingleton(new CouchService());
            _map.AddSingleton(new HelpService());
            _map.AddSingleton(new MatchMakingService());
            //_map.AddSingleton(new GreeterService()); 
            // When all your required services are in the collection, build the container.
            // Tip: There's an overload taking in a 'validateScopes' bool to make sure
            // you haven't made any mistakes in your dependency graph.
            _services = _map.BuildServiceProvider(true);
            // Either search the program and add all Module classes that can be found.
            // Module classes *must* be marked 'public' or they will be ignored.
            //await _commands.AddModulesAsync(Assembly.GetEntryAssembly(), _services);
            // Or add Modules manually if you prefer to be a little more explicit:
            //await _commands.AddModuleAsync<SomeModule>();
            await _commands.AddModuleAsync<TreasureModule>(_services);
            await _commands.AddModuleAsync<AdminModule>(_services);
            await _commands.AddModuleAsync<SimpleCommands>(_services);
            await _commands.AddModuleAsync<DeathCounterModule>(_services);
            await _commands.AddModuleAsync<BettingModule>(_services);
            await _commands.AddModuleAsync<VotingModule>(_services);
            await _commands.AddModuleAsync<RaffleModule>(_services);
            await _commands.AddModuleAsync<PoorLifeChoicesModule>(_services);
            await _commands.AddModuleAsync<MyPickModule>(_services);
            await _commands.AddModuleAsync<HelpModule>(_services);
            await _commands.AddModuleAsync<MatchMakingModule>(_services);
            //await _commands.AddModuleAsync<GreeterModule>(_services);

            // Note that the first one is 'Modules' (plural) and the second is 'Module' (singular).

            // Subscribe a handler to see if a message invokes a command.
            _DiscordClient.MessageReceived += HandleCommandAsync;

            _DiscordClient.Disconnected += ClientDisconnected;


        }

        private static async Task ClientDisconnected(Exception arg)
        {
            await Core.LOG(new LogMessage(LogSeverity.Error, "DiscordNET", $"ClientDisconnected:{arg.Message}"));
        }

        private static async Task HandleCommandAsync(SocketMessage arg)
        {
            //Console.WriteLine($":DW:[HandleCommandAsync]");
            // Bail out if it's a System Message.
            var msg = arg as SocketUserMessage;
            if (msg == null) return;

            // We don't want the bot to respond to itself or other bots.
            // NOTE: Selfbots should invert this first check and remove the second
            // as they should ONLY be allowed to respond to messages from the same account.
            if (msg.Author.Id ==  _DiscordClient.CurrentUser.Id || msg.Author.IsBot) return;

            // Create a number to track where the prefix ends and the command begins
            int pos = 0;
            // Replace the '!' with whatever character
            // you want to prefix your commands with.
            // Uncomment the second half if you also want
            // commands to be invoked by mentioning the bot instead.
            if (msg.HasCharPrefix(Program._commandCharacter, ref pos) /* || msg.HasMentionPrefix(_client.CurrentUser, ref pos) */)
            {
                // Create a Command Context.
                var context = new SocketCommandContext(_DiscordClient, msg);

                // Execute the command. (result does not indicate a return value, 
                // rather an object stating if the command executed succesfully).
                var result = await _commands.ExecuteAsync(context, pos, _services);

                // Uncomment the following lines if you want the bot
                // to send a message if it failed (not advised for most situations).
                //if (!result.IsSuccess && result.Error != CommandError.UnknownCommand)
                //    await msg.Channel.SendMessageAsync(result.ErrorReason);
            }
        }

    }
}
