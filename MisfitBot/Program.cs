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
using MisfitBot_MKII.Extensions.CommandInterpreter;
using System.Linq;
using MisfitBot_MKII.Statics;
using MisfitBot_MKII.Twitch;
using System.Data;

namespace MisfitBot_MKII;

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
    private static TwitchChannelWatcher _TwitchChannelWatcher;
    private static ChannelManager _Channels;
    private static UserManagerService _Users;
    private static BotWideEvents _BotEvents;
    private static CommandInterpreter commands;

    private static bool _DebugMode = false;
    private static bool _LogTwitch = false;

    internal static DiscordSocketClient DiscordClient { get => _DiscordClient; private set => _DiscordClient = value; }
    internal static ITwitchClient TwitchClient { get => _TwitchClient; private set => _TwitchClient = value; }
    public static ITwitchAPI TwitchAPI { get => _TwitchAPI; private set => _TwitchAPI = value; }
    public static ChannelManager Channels { get => _Channels; private set => _Channels = value; }
    public static UserManagerService Users { get => _Users; private set => _Users = value; }
    public static CommandInterpreter Commands { get => commands; private set => commands = value; }
    public static BotWideEvents BotEvents { get => _BotEvents; set => _BotEvents = value; }
    public static bool DebugMode { get => _DebugMode; private set => _DebugMode = value; }
    public static bool TwitchConnected { get => _TwitchClient.IsConnected; private set { } }
    public static int Version { get => _version; private set { } }



    private static List<PluginBase> plugins;
    public static List<PluginBase> Plugins { get => plugins; }
    public static int PluginCount { get => plugins.Count; private set { } }

    #region Main config values
    static bool useDiscord = false;
    public static bool UseDiscord => useDiscord;
    static bool useTwitch = false;
    public static bool UseTwitch => useTwitch;
    static string twitchAccessToken = string.Empty;
    public static string TwitchAccessToken => twitchAccessToken;
    static string discordToken = string.Empty;
    public static string DiscordToken => discordToken;
    static char commandCharacter = '!';
    public static char CommandCharacter => commandCharacter;
    static string botNameTwitch = "";
    public static string BotNameTwitch => botNameTwitch;
    static string botNameDiscord = "";
    public static string BotNameDiscord => botNameDiscord;


    static void Main(string[] args)
    {
        List<string> arguments = new List<string>();
        arguments.AddRange(args);
        new Program().MainAsync(arguments).GetAwaiter().GetResult();
    }

    public async Task MainAsync(List<string> args)
    {
        _BotEvents = new BotWideEvents();

        await ReactToStartupArguments(args);
        Core.Init();
        CreateNewDatabase();
        ConnectToDatabase();

        // Remember to init these after db stuff setup
        _Channels = new ChannelManager();
        _Users = new UserManagerService();

        await VerifyFoldersAndStartupFiles();


        // Check that we have a config table for bot if not create one
        VerifyConfigTableExists();

        // start authentication things
        Console.WriteLine("Checking authentication....");
        await DeviceToken.Initialize();
        if(!await DeviceToken.Validate())
        {
            Console.WriteLine("Validation failed");
            string userCode = await DeviceToken.GetDeviceCode();
            Console.WriteLine("Validation failed");
            Console.WriteLine("//////////////////////////////////////////////////////////");
            Console.WriteLine("       Please go to https://www.twitch.tv/activate");
            Console.WriteLine($"               Enter the code \"{userCode}\"");
            Console.WriteLine("           Accept the authorization to run this");
            Console.WriteLine("//////////////////////////////////////////////////////////");
            await DeviceToken.WaitForAuthentication(60.0f);
        }

        PromptForCommandCharacter();


        /*
        await PromptForDis
        cordToken();
        PromptForTwitchChannel();

        LoadPlugins();
        InitCommandInterpreter();
        _TwitchServiceEvents = new EventCatcherTwitchServices();
        StartTwitchAPI(_TwitchServiceEvents);
        StartTwitchClient();
        await StartDiscordClient();
*/
        // Block the program until it is closed.
        await Task.Delay(Timeout.Infinite);
    }

    private void InitCommandInterpreter()
    {
        commands = new CommandInterpreter();
        foreach (PluginBase pl in plugins)
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
        if (!UseTwitch) { return; }
        Core.LOG(new LogEntry(LOGSEVERITY.INFO, "Bot", $"Starting up Twitch API"));
        KSLogger logger = new KSLogger();
        if (_LogTwitch) { TwitchAPI = new TwitchAPI(logger); }
        else
        { TwitchAPI = new TwitchAPI(); }
        TwitchAPI.Settings.SkipDynamicScopeValidation = true;
        TwitchAPI.Settings.ClientId = Secrets.ClientID;
        TwitchAPI.Settings.AccessToken = Secrets.AuthToken;
        StartTwitchChannelWatcher(arg);
    }

    private void StartTwitchClient()
    {
        if (!UseTwitch) { return; }
        Core.LOG(new LogEntry(LOGSEVERITY.INFO, "Bot", $"Starting up Twitch"));
        ConnectionCredentials cred = new ConnectionCredentials(BotNameTwitch, TwitchAccessToken);
        _TwitchClient = new TwitchClient();
        _TwitchClient.Initialize(cred, BotNameTwitch);
        _TwitchClient.RemoveChatCommandIdentifier('!');
        _TwitchClient.AddChatCommandIdentifier(Program.CommandCharacter);
        _TwitchEvents = new EventCatcherTwitch(_TwitchClient, _LogTwitch);
        _TwitchClient.Connect();
    }

    private void StartTwitchChannelWatcher(EventCatcherTwitchServices arg)
    {
        _TwitchChannelWatcher = new TwitchChannelWatcher(arg);
    }

    
    private async Task ReactToStartupArguments(List<string> args)
    {
        foreach (string arg in args)
        {
            if (arg == "debug")
            {
                Console.WriteLine("!!!!RUNNING IN DEBUGMODE!!!!");
                DebugMode = true;
            }
            if (arg == "logtwitch")
            {
                Console.WriteLine("!!!!LOGGING TWITCH OUTPUT!!!!");
                _LogTwitch = true;
            }
            if (arg == "cleanlaunch")
            {
                if (File.Exists("DATABASE.sqlite"))
                {
                    Console.WriteLine("!!!! CLEAN FRESH START !!!!");
                    File.Delete("DATABASE.sqlite");
                    await Task.Delay(100);
                }
            }
        }

    }

    public static async void DiscordRemoveMessage(ulong channelID, ulong messageID)
    {
        await (DiscordClient.GetChannel(channelID) as Discord.WebSocket.ISocketMessageChannel).DeleteMessageAsync(messageID);
    }

    /// <summary>
    /// This verifies there is a plugin folder
    /// </summary>
    /// <returns></returns>
    private async Task VerifyFoldersAndStartupFiles()
    {

        await Task.Run(async () =>
        {
            if (!Directory.Exists("Plugins"))
            {
                if (DebugMode) { Console.WriteLine("Creating plugins folder!"); }
                Directory.CreateDirectory("Plugins");
            }
        });
    }

    private async Task StartDiscordClient()
    {
        if (!UseDiscord) { return; }
        await Core.LOG(new LogEntry(LOGSEVERITY.INFO, "Bot", $"Starting up Discord"));

        DiscordSocketConfig dConfig = new DiscordSocketConfig()
        {
            GatewayIntents = GatewayIntents.GuildMembers | GatewayIntents.GuildPresences | GatewayIntents.DirectMessages | GatewayIntents.DirectMessageReactions | GatewayIntents.Guilds | GatewayIntents.GuildBans | GatewayIntents.GuildEmojis | GatewayIntents.GuildMessages | GatewayIntents.GuildMessageReactions
        };

        _DiscordClient = new DiscordSocketClient(dConfig);
        _DiscordEvents = new EventCatcherDiscord(_DiscordClient);


        // Tokens should be considered secret data, and never hard-coded.
        await _DiscordClient.LoginAsync(TokenType.Bot, DiscordToken);
        await _DiscordClient.StartAsync();
    }

    public void InitCommands()
    {
        _services = _map.BuildServiceProvider(true);
    }



    /// <summary>
    /// Prompts the user to input a single valid commandCharacter<br/>
    /// re-prompts until a valid one is entered
    /// </summary>
    private void PromptForCommandCharacter()
    {
        string inCMDChar;
        do
        {
            Console.WriteLine("What character do you want as the command prefix? valid are(!,?,&,%)");
            inCMDChar = Console.ReadLine();
            if (inCMDChar != null)
            {
                inCMDChar = inCMDChar.Trim();
                string character = inCMDChar[0].ToString();
                string valids = "!?&%";
                if (valids.Contains(character))
                {
                    inCMDChar = character.ToString();
                }
                else
                {
                    inCMDChar = string.Empty;
                }
            }
        } while (inCMDChar == null || inCMDChar == string.Empty || inCMDChar.Length > 1);
        Console.WriteLine($"Command character now set to \"{inCMDChar}\".");
        SetCommandCharacter(inCMDChar);
    }



    private async Task PromptForDiscordToken()
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

        useDiscord = discordQuery;

        if (!UseDiscord) { return; }

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
                //DiscordToken = outToken;
            }
        });
    }

    private void PromptForTwitchChannel()
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
        useTwitch = inKey.Key == ConsoleKey.Y;
        if (!UseTwitch) { return; }

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
                    //config.TwitchToken = outOATHToken;
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
                    //config.TwitchClientID = outClientID;
                }
            }
        } while (inClientID == null);
        Console.WriteLine("");
        Console.WriteLine("Enter the Twitch username of this bot. This channel will also be auto joined always when bot connects to Twitch.");
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
                    //config.TwitchUser = outUserName;
                }
            }
        } while (inUserName == null);
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
    /// <summary>
    /// Creates a connection with our database file.<br/>
    /// Also makes sure there is a table for the bot config.
    /// </summary>
    void ConnectToDatabase()
    {
        Core.Data = new SQLiteConnection("Data Source=DATABASE.sqlite;Version=3;");
        Core.Data.Open();


    }
    void VerifyConfigTableExists()
    {
        string tableName = "botConfig";
        bool tableExists = false;

        // Check if table exists
        using (SQLiteCommand cmd = new SQLiteCommand())
        {
            cmd.CommandType = CommandType.Text;
            cmd.Connection = Core.Data;
            cmd.CommandText = $"SELECT COUNT(*) AS QtRecords FROM sqlite_master WHERE type = 'table' AND name = @name";
            cmd.Parameters.AddWithValue("@name", tableName);
            if (Convert.ToInt32(cmd.ExecuteScalar()) != 0)
            {
                tableExists = true;
            }
        }
        // Create table if needed
        if (!tableExists)
        {
            using (SQLiteCommand cmd = new SQLiteCommand())
            {
                cmd.CommandType = CommandType.Text;
                cmd.Connection = Core.Data;
                //ID int primary key IDENTITY(1,1) NOT NULL
                cmd.CommandText = $"CREATE TABLE \"{tableName}\" (" +
                    $"ROWID INTEGER PRIMARY KEY AUTOINCREMENT," +
                    $"UseDiscord BOOLEAN, " +
                    $"UseTwitch BOOLEAN, " +
                    $"CMDCharacter VACHAR(1), " +
                    $"DiscordToken VACHAR(255), " +
                    $"TwitchToken VACHAR(255)" +
                    $")";
                cmd.ExecuteNonQuery();
            }
        }
    }
    private void SetCommandCharacter(string inCMDChar)
    {
        throw new NotImplementedException();
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



}// EOF CLASS