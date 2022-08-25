using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Discord.Commands;
using Microsoft.Extensions.DependencyInjection;
using System.Data.SQLite;
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
using MisfitBot_MKII.Extensions.UserManager;
using MisfitBot_MKII.Extensions.PubSub;
using MisfitBot_MKII.Extensions.CommandInterpreter;
using System.Linq;
using MisfitBot_MKII.Statics;
using MisfitBot_MKII.Twitch;

namespace MisfitBot_MKII
{
    public class Program
    {
        public const int _version = 3;
        private static DiscordSocketClient _DiscordClient;
        private static EventCatcherDiscord _DiscordEvents;
        private static IServiceProvider _services;
        private static IServiceCollection _map = new ServiceCollection();
        private static CommandService _commands = new CommandService();

        private static ITwitchClient _TwitchClient;
        private static EventCatcherTwitch _TwitchEvents;
        private static EventCatcherTwitchServices _TwitchServiceEvents;
        private static ITwitchAPI _TwitchAPI;
        private static PubSubManager _PubSubs;
        private static TwitchChannelWatcher _TwitchChannelWatcher;

        private static ChannelManager _Channels;
        private static UserManagerService _Users;
        private static BotwideEvents _BotEvents;
        private static CommandInterpreter commands;

        private static MainConfig config;
        private static bool _Debugmode = false;
        private static bool _LogTwitch = false;

        internal static DiscordSocketClient DiscordClient { get => _DiscordClient; private set => _DiscordClient = value; }
        internal static ITwitchClient TwitchClient { get => _TwitchClient; private set => _TwitchClient = value; }
        public static ITwitchAPI TwitchAPI { get => _TwitchAPI; private set => _TwitchAPI = value; }
        internal static PubSubManager PubSubs { get => _PubSubs; private set => _PubSubs = value; }
        public static char CommandCharacter { get => config.CMDCharacter; private set => config.CMDCharacter = value; }
        public static string BotNameTwitch { get => Cipher.Decrypt(config.TwitchUser); private set {} }
        public static string BotNameDiscord { get => DiscordClient.CurrentUser.Username; private set {} }
        public static ChannelManager Channels { get => _Channels; private set => _Channels = value; }
        public static UserManagerService Users { get => _Users; private set => _Users = value; }
        public static CommandInterpreter Commands { get => commands; private set => commands = value; }
        public static BotwideEvents BotEvents { get => _BotEvents; set => _BotEvents = value; }
        public static bool Debugmode { get => _Debugmode; private set => _Debugmode = value; }
        public static bool TwitchConnected { get => _TwitchClient.IsConnected; private set {} }
        public static int Version { get => _version; private set{} }

        private static List<PluginBase> plugins;
        public static List<PluginBase> Plugins { get => plugins; }
        public static int PluginCount { get => plugins.Count; private set { } }


        static void Main(string[] args)
        {
            List<string> arguments = new List<string>();
            arguments.AddRange(args);
            new Program().MainAsync(arguments).GetAwaiter().GetResult();
        }

        public async Task MainAsync(List<string> args)
        {
            _BotEvents = new BotwideEvents();

            ReactToStartupArguments(args);
            InitCore();
            CreateNewDatabase();
            ConnectToDatabase();

            _Channels = new ChannelManager();
            _Users = new UserManagerService();

            await VerifyFoldersAndStartupFiles();

            LoadPlugins();
            InitCommandInterpreter();
            _TwitchServiceEvents = new EventCatcherTwitchServices();
            StartTwitchAPI(_TwitchServiceEvents);
            StartTwitchClient();
            await StartDiscordClient();

            await StartPubSubManager();
            // Block the program until it is closed.
            await Task.Delay(Timeout.Infinite);
        }

        private void InitCommandInterpreter()
        {
            commands = new CommandInterpreter();
            foreach(PluginBase pl in plugins)
            {
                commands.ProcessPlugin(pl);
            }
        }

        private void LoadPlugins()
        {
            plugins = new List<PluginBase>();
            string PluginFolder = "Plugins";
            string[] FolderContent = Directory.GetDirectories(PluginFolder);
            foreach (string fileName in FolderContent)
            {
                if (Directory.Exists(fileName))
                {
                    if (File.Exists($"{fileName}/{fileName.Remove(0, 7)}.dll"))
                    {
                        string directoryPath = System.IO.Directory.GetCurrentDirectory();
                        Assembly a = Assembly.LoadFile($"{directoryPath}/{fileName}/{fileName.Remove(0, 7)}.dll");
                        string nameClass = $"{fileName.Remove(0, 8)}.{fileName.Remove(0, 8)}";
                        Type myType = a.GetType(nameClass, true);
                        object obj = Activator.CreateInstance(myType);
                        var plugin = obj as PluginBase;
                        plugins.Add(plugin);
                    }
                }
            }
        }

        private void StartTwitchAPI(EventCatcherTwitchServices arg)
        {
            if (!config.UseTwitch) { return; }
            Core.LOG(new LogEntry(LOGSEVERITY.INFO, "Bot", $"Starting up Twitch API"));
            KSLogger logger = new KSLogger();
            if (_LogTwitch) { TwitchAPI = new TwitchAPI(logger); }
            else
            { TwitchAPI = new TwitchAPI(); }
            TwitchAPI.Settings.SkipDynamicScopeValidation = true;
            TwitchAPI.Settings.ClientId = TwitchClientID();
            TwitchAPI.Settings.AccessToken = TwitchOAUTHToken();
            StartTwitchChannelWatcher(arg);
        }

        private void StartTwitchClient()
        {
            if (!config.UseTwitch) { return; }
            Core.LOG(new LogEntry(LOGSEVERITY.INFO, "Bot", $"Starting up Twitch"));
            ConnectionCredentials cred = new ConnectionCredentials(TwitchUserName(), TwitchOAUTHToken());
            _TwitchClient = new TwitchClient();
            _TwitchClient.Initialize(cred, TwitchUserName());
            _TwitchClient.RemoveChatCommandIdentifier('!');
            _TwitchClient.AddChatCommandIdentifier(Program.CommandCharacter);
            _TwitchEvents = new EventCatcherTwitch(_TwitchClient, _LogTwitch);
            _TwitchClient.Connect();
        }

        private void StartTwitchChannelWatcher(EventCatcherTwitchServices arg)
        {
            _TwitchChannelWatcher = new TwitchChannelWatcher(arg);
        }

        private async Task StartPubSubManager()
        {
            _PubSubs = new PubSubManager();
            await _PubSubs.LaunchAllPubSubs();
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
                Debugmode = true;
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

        public static async void DiscordRemoveMessage(ulong channelID, ulong messageID)
        {
            await (DiscordClient.GetChannel(channelID) as  Discord.WebSocket.ISocketMessageChannel).DeleteMessageAsync(messageID);
        }

        // This runs verification and loading of existing stuff or a first start query session
        private async Task VerifyFoldersAndStartupFiles()
        {

            await Task.Run(async () =>
            {
                if (!Directory.Exists("Config"))
                {
                    if (Debugmode) { Console.WriteLine("Creating config folder!"); }
                    Directory.CreateDirectory("Config");
                }
                if (!Directory.Exists("Plugins"))
                {
                    if (Debugmode) { Console.WriteLine("Creating plugins folder!"); }
                    Directory.CreateDirectory("Plugins");
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
            await Core.LOG(new LogEntry(LOGSEVERITY.INFO, "Bot", $"Starting up Discord"));

            DiscordSocketConfig dConfig = new DiscordSocketConfig()
            {
                GatewayIntents = GatewayIntents.GuildMembers | GatewayIntents.GuildPresences | GatewayIntents.DirectMessages | GatewayIntents.DirectMessageReactions | GatewayIntents.Guilds | GatewayIntents.GuildBans | GatewayIntents.GuildEmojis | GatewayIntents.GuildMessages | GatewayIntents.GuildMessageReactions
            };

            _DiscordClient = new DiscordSocketClient(dConfig);
            _DiscordEvents = new EventCatcherDiscord(_DiscordClient);
            

            // Tokens should be considered secret data, and never hard-coded.
            await _DiscordClient.LoginAsync(TokenType.Bot, DiscordToken());
            await _DiscordClient.StartAsync();
        }

        public void InitCommands()
        {
            _services = _map.BuildServiceProvider(true);
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

            Console.WriteLine(System.Environment.NewLine + "Enter your Discord Token.");
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
            
            Console.WriteLine(System.Environment.NewLine + $"To use PubSub events like subscriber, bits and such you need a token from this link https://twitchtokengenerator.com/quick/YfuRoOx9WW " +
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
            Console.WriteLine("Enter the Bot Twitch Client ID.");
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
                Core.LOG(new LogEntry(LOGSEVERITY.INFO, "Misfitbot", "Database found."));
            }
            else
            {
                Core.LOG(new LogEntry(LOGSEVERITY.INFO, "Misfitbot", "Database not found. Creating a fresh victim."));
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

        public static void TwitchSayMessage(string channel, string message)
        {
            TwitchClient.SendMessage(channel, message);
        }
        public static void TwitchResponse(BotWideResponseArguments args)
        {
            TwitchClient.SendMessage(args.twitchChannel, args.message);
        }
        public static void PubSubStart(BotChannel bChan){
            PubSubs.StartPubSub(bChan, true);
        }
        public static void PubSubStop(BotChannel bChan){
            PubSubs.PubSubStop(bChan);
        }
        public static string PubSubStatus(BotChannel bChan){
            return PubSubs.PubSubStatus(bChan);
        }
    }
}
