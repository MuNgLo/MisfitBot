using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Discord.Commands;
using MisfitBot_MKII.Crypto;
using Microsoft.Extensions.DependencyInjection;
using System.Data.SQLite;
using MisfitBot_MKII.Services;
using MisfitBot_MKII.Extensions.ChannelManager;
using TwitchLib.Client.Interfaces;
using TwitchLib.Client;
using TwitchLib.Client.Models;
using TwitchLib.Client.Events;
using TwitchLib.Api.Interfaces;
using TwitchLib.Api;
using Newtonsoft.Json;
using MisfitBot_MKII.MisfitBotEvents;
using System.Reflection;

namespace MisfitBot_MKII
{
    public class Program
    {
        private static DiscordSocketClient _DiscordClient;
        private static EventCatcherDiscord _DiscordEvents;
        private static IServiceProvider _services;
        private static IServiceCollection _map = new ServiceCollection();
        private static CommandService _commands = new CommandService();

        private static ITwitchClient _TwitchClient;
        private static EventCatcherTwitch _TwitchEvents;
        private static ITwitchAPI _TwitchAPI;

        private static ChannelManager _Channels;
        private static UserManagerService _Users;
        private static BotwideEvents _BotEvents;

        private static MainConfig config;
        private static bool _Debugmode = false;
        private static bool _LogTwitch = false;

        public static DiscordSocketClient DiscordClient { get => _DiscordClient; private set => _DiscordClient = value; }
        public static ITwitchClient TwitchClient { get => _TwitchClient; private set => _TwitchClient = value; }
        public static ITwitchAPI TwitchAPI { get => _TwitchAPI; private set => _TwitchAPI = value; }
        public static char CommandCharacter { get => config.CMDCharacter; private set => config.CMDCharacter = value; }
        public static ChannelManager Channels { get => _Channels; private set => _Channels = value; }
        public static UserManagerService Users { get => _Users; private set => _Users = value; }
        public static BotwideEvents BotEvents { get => _BotEvents; set => _BotEvents = value; }

        public List<ServiceBase> _plugins;

        static void Main(string[] args)
        {
            List<string> arguments = new List<string>();
            arguments.AddRange(args);
            new Program().MainAsync(arguments).GetAwaiter().GetResult();
        }

        public async Task MainAsync(List<string> args)
        {
            _BotEvents = new BotwideEvents();

            LoadPlugins();
            ReactToStartupArguments(args);
            InitCore();
            CreateNewDatabase();
            ConnectToDatabase();

            _Channels = new ChannelManager();
            _Users = new UserManagerService();

            await VerifyFoldersAndStartupFiles();
            StartTwitchClient();
            await StartDiscordClient();
            // Block the program until it is closed.
            await Task.Delay(Timeout.Infinite);
        }

        private void LoadPlugins()
        {
            _plugins = new List<ServiceBase>();

            Assembly a = Assembly.LoadFile(@"D:\Dev\_Munglo\MisfitBot\Plugins\Juan\bin\juanplugins\netcoreapp3.1\ExamplePlugin");
            // Get the type to use.
            Type myType = a.GetType("ExamplePlugin.ExamplePlugin", true);
            // Create an instance.
            object obj = Activator.CreateInstance(myType);


            // Execute the method.
            //MethodInfo myMethod = myType.GetMethod("MethodA");
            // Create an instance.
            //object obj = Activator.CreateInstance(myType);
            // Execute the method.
            //myMethod.Invoke(obj, null);


            var plugin = obj as ServiceBase;

            _plugins.Add(plugin);
        }

        private void StartTwitchClient()
        {
            if (!config.UseTwitch) { return; }
            KSLogger logger = new KSLogger();
            if (_LogTwitch) { TwitchAPI = new TwitchAPI(logger); }
            else
            { TwitchAPI = new TwitchAPI(); }
            TwitchAPI.Settings.SkipDynamicScopeValidation = true;
            TwitchAPI.Settings.ClientId = TwitchClientID();
            TwitchAPI.Settings.AccessToken = "";

            ConnectionCredentials cred = new ConnectionCredentials(TwitchUserName(), TwitchOAUTHToken());
            _TwitchClient = new TwitchClient();
            _TwitchClient.Initialize(cred, TwitchUserName());
            _TwitchClient.RemoveChatCommandIdentifier('!');
            _TwitchClient.AddChatCommandIdentifier(Program.CommandCharacter);
            _TwitchEvents = new EventCatcherTwitch(_TwitchClient, _LogTwitch);
            _TwitchClient.Connect();
        }

        

        /// Makes sure Core is setup and have what it needs
        private void InitCore()
        {
            Core.Configs = new ConfigurationHandler();
            Core.LastLaunchTime = Core.CurrentTime;
            Core.Timers = new TimerStuff();
            Core.LOGGER = new JuansLog();
            Core.LOG = Core.LOGGER.LogThis;
            Core.serializer.Formatting = Newtonsoft.Json.Formatting.Indented;
        }
        private void ReactToStartupArguments(List<string> args)
        {
            if (args.Exists(p => p.Trim().ToLower() == "debug"))
            {
                Console.WriteLine("!!!!RUNNING IN DEBUGMODE!!!!");
                _Debugmode = true;
            }
            if (args.Exists(p => p.Trim().ToLower() == "logtwitch"))
            {
                Console.WriteLine("!!!!LOGGING TWITCH OUTPUT!!!!");
                _LogTwitch = true;
            }
            if (args.Exists(p => p.Trim().ToLower() == "dev"))
            {
                if (Directory.Exists("Config"))
                {
                    Console.WriteLine("!!!!RESETTING CONFIG!!!!");
                    Directory.Delete("Config", true);
                }
            }
            if (args.Exists(p => p.Trim().ToLower() == "cleardb"))
            {
                if (File.Exists("DATABASE.sqlite"))
                {
                    Console.WriteLine("!!!!DELETING EXISTING DATABASE!!!!");
                    File.Delete("DATABASE.sqlite");
                }
            }

        }
        // This runs verification and loading of existing stuff or a first start query session
        private async Task VerifyFoldersAndStartupFiles()
        {
            
            await Task.Run(async () =>
            {
                if (!Directory.Exists("Config"))
                {
                    if (_Debugmode) { Console.WriteLine("Creating config folder!"); }
                    Directory.CreateDirectory("Config");
                }
                if (!LoadConfig())
                {
                    CreateConfig();
                    PromtForCommandCharacter();

                    await PromtForDiscordToken();
                    PromtForTwitchChannel();
                    SaveConfig();
                }


            });
        }
        private bool LoadConfig()
        {
            if (File.Exists("Config/Main.json"))
            {
                string inCFG = File.ReadAllText("Config/Main.json");
                config = JsonConvert.DeserializeObject<MainConfig>(inCFG);
                return true;
            }
            return false;
        }
        private void CreateConfig()
        {
            config = new MainConfig();
            File.WriteAllText("Config/Main.json", JsonConvert.SerializeObject(config, Formatting.Indented));
        }
        private void SaveConfig()
        {
            File.WriteAllText("Config/Main.json", JsonConvert.SerializeObject(config, Formatting.Indented));
        }
        private async Task StartDiscordClient()
        {
            if (!config.UseDiscord) { return; }
            _DiscordClient = new DiscordSocketClient();
            _DiscordEvents = new EventCatcherDiscord(_DiscordClient);
            
            InitCommands();
            // Tokens should be considered secret data, and never hard-coded.
            await _DiscordClient.LoginAsync(TokenType.Bot, DiscordToken());
            await _DiscordClient.StartAsync();
        }

        public void InitCommands()
        {
            // Repeat this for all the service classes
            // and other dependencies that your commands might need.
            //_map.AddSingleton(new TwitchService()); // Make sure this loads first
            _map.AddSingleton(new UserManagerService());
            //_map.AddSingleton(new ChannelManager());
            /*_map.AddSingleton(new AdminService());
            _map.AddSingleton(new TwitchCommandsService());
            _map.AddSingleton(new TreasureService());
            _map.AddSingleton(new MyPickService());
            _map.AddSingleton(new BettingService());
            _map.AddSingleton(new DeathCounterService());
            _map.AddSingleton(new VotingService());
            _map.AddSingleton(new RaffleService());
            _map.AddSingleton(new PoorLifeChoicesService());
            _map.AddSingleton(new CouchService());
            _map.AddSingleton(new HelpService());
            _map.AddSingleton(new MatchMakingService());
            _map.AddSingleton(new QueueService());
*/
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
            /*await _commands.AddModuleAsync<AdminModule>(_services);
            await _commands.AddModuleAsync<TreasureModule>(_services);
            await _commands.AddModuleAsync<MyPickModule>(_services);
            await _commands.AddModuleAsync<BettingModule>(_services);
            await _commands.AddModuleAsync<SimpleCommands>(_services);
            await _commands.AddModuleAsync<DeathCounterModule>(_services);
            await _commands.AddModuleAsync<VotingModule>(_services);
            await _commands.AddModuleAsync<RaffleModule>(_services);
            await _commands.AddModuleAsync<PoorLifeChoicesModule>(_services);
            await _commands.AddModuleAsync<CouchModule>(_services);
            await _commands.AddModuleAsync<HelpModule>(_services);
            await _commands.AddModuleAsync<MatchMakingModule>(_services);
            //await _commands.AddModuleAsync<GreeterModule>(_services);
*/
            // Note that the first one is 'Modules' (plural) and the second is 'Module' (singular).
        }

        #region First launch and console setup stuff
        // decrypt it and returns the actual token as a string
        private string DiscordToken()
        {
            return Cipher.Decrypt(config.DiscordToken);
        }
        private void PromtForCommandCharacter()
        {
            Console.WriteLine("What character do you want as the command prefix?");
            string inCMDChar;
            do
            {
                inCMDChar = Console.ReadLine();
                if (inCMDChar != null)
                    Console.WriteLine("      " + inCMDChar);
            } while (inCMDChar == null || inCMDChar.Length > 1);
            inCMDChar = inCMDChar.Trim();
            config.CMDCharacter = inCMDChar[0];
            Console.WriteLine($"Command character now set to \"{CommandCharacter}\".");

        }
        private async Task PromtForDiscordToken()
        {
            Console.WriteLine("Do you want a Discord connection? Y/N");
            bool discordQuery = false;
            ConsoleKeyInfo inKey;
            do
            {
                inKey = Console.ReadKey();
                if (inKey.Key == ConsoleKey.Y || inKey.Key == ConsoleKey.N)
                {
                    discordQuery = inKey.Key == ConsoleKey.Y;
                }
            } while (inKey.Key != ConsoleKey.Y && inKey.Key != ConsoleKey.N);

            config.UseDiscord = discordQuery;

            if (!config.UseDiscord) { return; }

            Console.WriteLine("Enter your Discord Token.");
            string inToken;
            do
            {
                inToken = Console.ReadLine();
                if (inToken != null)
                    Console.WriteLine("      " + inToken);
            } while (inToken == null);
            inToken = inToken.Trim();
            await Task.Run(() =>
            {
                string outToken = Cipher.Encrypt(inToken);
                if (inToken == Cipher.Decrypt(outToken))
                {
                    config.DiscordToken = outToken;
                }
            });
        }

        private void PromtForTwitchChannel()
        {
            Console.WriteLine("Do you want a Twitch connection? Y/N");
            bool twitchQuery = false;
            ConsoleKeyInfo inKey;
            do
            {
                inKey = Console.ReadKey();
                if (inKey.Key == ConsoleKey.Y || inKey.Key == ConsoleKey.N)
                {
                    twitchQuery = inKey.Key == ConsoleKey.Y;
                }
            } while (inKey.Key != ConsoleKey.Y && inKey.Key != ConsoleKey.N);
            config.UseTwitch = inKey.Key == ConsoleKey.Y;
            if (!config.UseTwitch) { return; }

            Console.WriteLine($"To use PubSub events like subscriber, bits and such you need a token from this link https://twitchtokengenerator.com/quick/YfuRoOx9WW " +
                        $"It is to generate a token specific for your Twitch channel. To later remove access through this token you remove it on Twitch under " +
                        $"settings>Connections. It will be called \"Twitch Token Generator by swiftyspiffy\".");
            Console.WriteLine("");
            Console.WriteLine("Enter the Twitch OAUTH Token.");
            string inOATHToken;
            do
            {
                inOATHToken = Console.ReadLine();
                if (inOATHToken != null)
                {
                    string outOATHToken = Cipher.Encrypt(inOATHToken.Trim());
                    if (inOATHToken.Trim() == Cipher.Decrypt(outOATHToken))
                    {
                        config.TwitchToken = outOATHToken;
                    }
                }
            } while (inOATHToken == null);
            Console.WriteLine("");
            Console.WriteLine("Enter the Bot Client ID.");
            string inClientID;
            do
            {
                inClientID = Console.ReadLine();
                if (inClientID != null)
                {
                    inClientID = inClientID.Trim();
                    string outClientID = Cipher.Encrypt(inClientID);
                    if (inClientID == Cipher.Decrypt(outClientID))
                    {
                        config.TwitchClientID = outClientID;
                    }
                }
            } while (inClientID == null);
            Console.WriteLine("");
            Console.WriteLine("Enter the Twitch username of this bot. This channel will also be autojoined always when bot connects to Twitch.");
            string inUserName;
            do
            {
                inUserName = Console.ReadLine();
                if (inUserName != null)
                {
                    inUserName = inUserName.Trim();
                    string outUserName = Cipher.Encrypt(inUserName);
                    if (inUserName == Cipher.Decrypt(outUserName))
                    {
                        config.TwitchUser = outUserName;
                    }
                }
            } while (inUserName == null);
        }
        private string TwitchOAUTHToken()
        {
            return Cipher.Decrypt(config.TwitchToken);
        }
        private string TwitchClientID()
        {
            return Cipher.Decrypt(config.TwitchClientID);
        }
        private string TwitchUserName()
        {
            return Cipher.Decrypt(config.TwitchUser);
        }
        #endregion
        #region Discord stuff
        
        
        
        public static async void DiscordReconnect()
        {
            await _DiscordClient.StartAsync();
        }
        #endregion
        #region SQLite stuff
        // Creates an empty database file
        void CreateNewDatabase()
        {
            if (System.IO.File.Exists("DATABASE.sqlite"))
            {
                Core.LOG(new LogMessage(LogSeverity.Info, "Misfitbot", "Database found."));
            }
            else
            {
                Core.LOG(new LogMessage(LogSeverity.Warning, "Misfitbot", "Database not found. Creating a fresh victim."));
                SQLiteConnection.CreateFile("DATABASE.sqlite");
            }
        }
        // Creates a connection with our database file.
        void ConnectToDatabase()
        {
            Core.Data = new SQLiteConnection("Data Source=DATABASE.sqlite;Version=3;");
            Core.Data.Open();
        }
        #endregion
        
        public static void TwitchSayMessage(string channel, string message){
            TwitchClient.SendMessage(channel, message);
        }

        public static async Task DiscordSayMessage(string channel, string message)
        {
            await (DiscordClient.GetChannel(Core.StringToUlong(channel)) as ISocketMessageChannel).SendMessageAsync(message);
        }

    }
}
