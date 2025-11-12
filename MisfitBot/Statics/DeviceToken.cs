

using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace MisfitBot_MKII.Statics;

public static class DeviceToken
{
    private static string scopes =
        "moderation:read moderator:read:followers moderator:read:chat_messages moderator:manage:chat_messages moderator:read:chatters " +
        "moderator:read:shoutouts moderator:manage:shoutouts " +
        "user:bot user:read:chat user:write:chat " +
        "channel:bot channel:read:subscriptions";


    /// <summary>
    /// Read existing info from DB
    /// </summary>
    public static async Task Initialize()
    {

    }
    /// <summary>
    /// Validates the existing token and return true if OK
    /// </summary>
    /// <returns></returns>
    public static async Task<bool> Validate()
    {
        return false;
    }
    /// <summary>
    /// Gets a device code from twitch that we can sue to direct user<br/>
    /// to an authorization page
    /// </summary>
    /// <returns></returns>
    public static async Task<string> GetDeviceCode()
    {
        using (HttpClient client = new System.Net.Http.HttpClient())
        {
            // Go get device code
            FormUrlEncodedContent requestRefresh = new FormUrlEncodedContent(new[] {
            new KeyValuePair<string, string>("client_id", Secrets.ClientID),
            new KeyValuePair<string, string>("scopes", scopes)
            });
            // Get the response.
            HttpResponseMessage response = await client.PostAsync("https://id.twitch.tv/oauth2/device", requestRefresh);

            // Get the response content.
            HttpContent responseContent = response.Content;
            StreamReader reader = new StreamReader(await responseContent.ReadAsStreamAsync());
            string output = await reader.ReadToEndAsync();
            AuthPageResponse args = JsonSerializer.Deserialize<AuthPageResponse>(output);

            Secrets.SetDeviceCode(args.device_code);
            return args.user_code;
        }
    }
    /// <summary>
    /// Go to twitch and ask for status on the device code
    /// </summary>
    /// <returns></returns>
    private static async Task<bool> CheckResponse()
    {
        using (HttpClient client = new System.Net.Http.HttpClient())
        {
            // Go get device code
            FormUrlEncodedContent requestRefresh = new FormUrlEncodedContent(new[] {
            new KeyValuePair<string, string>("client_id", Secrets.ClientID),
            new KeyValuePair<string, string>("scopes", Uri.EscapeDataString(scopes)),
            new KeyValuePair<string, string>("device_code", Secrets.DeviceCode),
            new KeyValuePair<string, string>("grant_type", "urn:ietf:params:oauth:grant-type:device_code")
            });
            // Get the response.
            HttpResponseMessage response = await client.PostAsync("https://id.twitch.tv/oauth2/token", requestRefresh);

            // Get the response content.
            HttpContent responseContent = response.Content;
            StreamReader reader = new StreamReader(await responseContent.ReadAsStreamAsync());
            string output = await reader.ReadToEndAsync();
            AuthResponse args = JsonSerializer.Deserialize<AuthResponse>(output);

            if (args.access_token != string.Empty)
            {
                Secrets.SetAuthToken(args.access_token);
                Secrets.SetRefreshToken(args.refresh_token);
                return true;
            }
            else
            {
                Console.WriteLine($"DeviceToken::CheckResponse() output[{output}]");
            }
            return false;
        }
    }

    internal static async Task WaitForAuthentication(float timeToWait = 60.0f)
    {
        while (timeToWait > 0.0f)
        {
            Console.WriteLine($"Waiting for authentication result [{timeToWait.ToString("00")}]");
            timeToWait -= 5.0f;
            if (await CheckResponse())
            {
                Console.WriteLine($"Authentication successful!");
                return;
            }
            await Task.Delay(5000);
        }
        await CheckResponse();
    }



    private class AuthPageResponse
    {
        [JsonInclude]
        public string device_code;
        [JsonInclude]
        public int expires_in;
        [JsonInclude]
        public int interval;
        [JsonInclude]
        public string user_code;
        [JsonInclude]
        public string verification_uri;
        public override string ToString()
        {
            return $"device_code:{device_code} expires_in:{expires_in} interval:{interval} user_code:{user_code} verification_uri:{verification_uri}";
        }
    }
    private class AuthResponse
    {
        [JsonInclude]
        public string access_token = string.Empty;
        [JsonInclude]
        public int expires_in;
        [JsonInclude]
        public string refresh_token;
        [JsonInclude]
        public string[] scopes;
        [JsonInclude]
        public string token_type;
    }
    private class ValidationResponse
    {
        [JsonInclude]
        public string client_id;
        [JsonInclude]
        public string login;
        [JsonInclude]
        public string[] scopes;
        [JsonInclude]
        public string user_id;
        [JsonInclude]
        public int expires_in;
    }

}// EOF CLASS
