

using System;
using System.Data;
using System.Data.SQLite;

namespace MisfitBot_MKII.Statics;

public static class Secrets
{
    private readonly static string clientID = "cbFHPhWCx5eiI6w6b9HX477YIFQnJykT0ruZGvK2VuI="; // check
    private readonly static string secret = "LtsN8mudm4CMPu7bHDX5PCniPw+mziXtJLcQ4zrnfBY="; // check
    private static string userID = string.Empty; // check
    private static string userLogin = string.Empty; // check
    private static string deviceCode = string.Empty;
    private static string twitchAuthToken = string.Empty;
    private static string refreshToken = string.Empty;
    private static string commandCharacter = string.Empty;
    private static bool useDiscord = false;
    private static bool useTwitch = true;

    public static string ClientID => Cipher.Decrypt(clientID);
    public static string UserID => Cipher.Decrypt(userID);
    public static string UserLogin => Cipher.Decrypt(userLogin);
    public static string ClientSecret => Cipher.Decrypt(secret);
    public static string DeviceCode => Cipher.Decrypt(deviceCode);
    public static string TwitchAuthToken => Cipher.Decrypt(twitchAuthToken);
    public static string RefreshToken => Cipher.Decrypt(refreshToken);
    public static string CommandCharacter => commandCharacter;
    public static bool UseDiscord => useDiscord;
    public static bool UseTwitch => useTwitch;


    public static void SetUserID(string newID)
    {
        userID = Cipher.Encrypt(newID);
    }
    public static void SetUserLogin(string newUserLogin)
    {
        userLogin = Cipher.Encrypt(newUserLogin);
    }
    public static void SetDeviceCode(string newCode)
    {
        deviceCode = Cipher.Encrypt(newCode);
    }
    public static void SetAuthToken(string newToken)
    {
        twitchAuthToken = Cipher.Encrypt(newToken);
    }
    public static void SetRefreshToken(string newToken)
    {
        refreshToken = Cipher.Encrypt(newToken);
    }
    public static void SetCommandCharacter(string newCommandCharacter)
    {
        commandCharacter = newCommandCharacter;
    }
    public static void SetUseDiscord(bool newBool)
    {
        useDiscord = newBool;
    }
    public static void SetUseTwitch(bool newBool)
    {
        useTwitch = newBool;
    }
    internal static async void Load()
    {
        using (SQLiteCommand cmd = new SQLiteCommand())
        {
            cmd.CommandType = CommandType.Text;
            cmd.Connection = Core.Data;
            cmd.CommandText = $"SELECT * FROM botConfig WHERE ClientID IS @clientID";
            cmd.Parameters.AddWithValue("@clientID", clientID);
            SQLiteDataReader result;
            try
            {
                result = (SQLiteDataReader)await cmd.ExecuteReaderAsync();
            }
            catch (Exception)
            {
                throw;
            }
            if (result.Read())
            {
                //clientID = result.GetString(0);
                //secret = result.GetString(1);
                useDiscord = result.GetBoolean(2);
                useTwitch = result.GetBoolean(3);
                commandCharacter = result.GetString(4);
                twitchAuthToken = result.GetString(6);
                userLogin = result.GetString(7);
                userID = result.GetString(8);
            }
        }
    }
    internal static async void Save()
    {
        bool haveEntry = false;
        // Go look for old entry
        using (SQLiteCommand cmd = new SQLiteCommand())
        {
            cmd.CommandType = CommandType.Text;
            cmd.Connection = Core.Data;
            cmd.CommandText = $"SELECT * FROM botConfig WHERE UserID IS @UserID";
            cmd.Parameters.AddWithValue("@UserID", userID);

            if (await cmd.ExecuteScalarAsync() != null)
            {
                haveEntry = true;
            }
        }
        // Update existing entry
        if (haveEntry)
        {
            using (SQLiteCommand cmd = new SQLiteCommand())
            {
                cmd.CommandType = CommandType.Text;
                cmd.Connection = Core.Data;
                cmd.CommandText = $"UPDATE botConfig SET " +
                    $"UseDiscord = @UseDiscord, " +
                    $"UseTwitch = @UseTwitch, " +
                    $"CMDCharacter = @CMDCharacter, " +
                    $"TwitchToken = @TwitchToken " +
                    $" WHERE UserID is @UserID";
                cmd.Parameters.AddWithValue("@UserID", userID);
                cmd.Parameters.AddWithValue("@UseDiscord", useDiscord);
                cmd.Parameters.AddWithValue("@UseTwitch", useTwitch);
                cmd.Parameters.AddWithValue("@CMDCharacter", commandCharacter);
                cmd.Parameters.AddWithValue("@TwitchToken", twitchAuthToken);
                try
                {
                    cmd.ExecuteNonQuery();
                }
                catch (Exception)
                {
                    await Core.LOG(new LogEntry(LOGSEVERITY.ERROR, "MisfitBot Main Config", $"Database query failed hard. ({cmd.CommandText})"));
                    throw;
                }
            }
            return;
        }
        // Insert as new entry
        using (SQLiteCommand cmd = new SQLiteCommand())
        {
            cmd.CommandType = CommandType.Text;
            cmd.Connection = Core.Data;
            cmd.CommandText = $"INSERT INTO botConfig VALUES (" +
                $"@ClientID, " +
                $"@ClientSecret, " +
                $"@UseDiscord, " +
                $"@UseTwitch, " +
                $"@CMDCharacter, " +
                $"@DiscordToken, " +
                $"@TwitchToken, " +
                $"@UserLogin, " +
                $"@UserID)";

            cmd.Parameters.AddWithValue("@ClientID", clientID);
            cmd.Parameters.AddWithValue("@ClientSecret", secret);
            cmd.Parameters.AddWithValue("@UseDiscord", false);
            cmd.Parameters.AddWithValue("@UseTwitch", true);
            cmd.Parameters.AddWithValue("@CMDCharacter", commandCharacter);
            cmd.Parameters.AddWithValue("@DiscordToken", string.Empty);
            cmd.Parameters.AddWithValue("@TwitchToken", twitchAuthToken);
            cmd.Parameters.AddWithValue("@UserLogin", userLogin);
            cmd.Parameters.AddWithValue("@UserID", userID);

            try
            {
                cmd.ExecuteNonQuery();
            }
            catch (Exception)
            {
                await Core.LOG(new LogEntry(LOGSEVERITY.ERROR, "MisfitBot Main Config", $"Database query failed hard. ({cmd.CommandText})"));
                throw;
            }
        }
    }
}// EOF CLASS