

using System;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using MisfitBot_MKII.Statics;

namespace MisfitBot_MKII.Twitch;

public class TwitchAPICalls : IDisposable
{

    /// <summary>
    /// returns [user_id, login]
    /// </summary>
    /// <param name="token"></param>
    /// <returns></returns>
    public async Task<string[]> GetUserFromToken(string token)
    {
        using (HttpClient client = new())
        {

            HttpRequestMessage request = new(HttpMethod.Get, "https://id.twitch.tv/oauth2/validate");
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("OAuth", Secrets.TwitchAuthToken);

            HttpResponseMessage response = await client.SendAsync(request);
            if (response.StatusCode == System.Net.HttpStatusCode.OK)
            {
                string output = await response.Content.ReadAsStringAsync();
                ValidationResponse user = JsonSerializer.Deserialize<ValidationResponse>(output);
                return [user.user_id, user.login];
            }
        }
        return null;
    }



    #region IDisposable stuff
    private bool disposed;
    private protected virtual void Dispose(bool disposing)
    {
        if (!disposed)
        {
            if (disposing)
            {
                //dispose managed resources
            }
        }
        //dispose unmanaged resources
        disposed = true;
    }
	private class ValidationResponse
	{
		[JsonInclude]
		public string client_id = string.Empty;
		[JsonInclude]
		public string login = string.Empty;
		[JsonInclude]
		public string[] scopes = null;
		[JsonInclude]
		public string user_id = string.Empty;
		[JsonInclude]
		public int expires_in = 0;
	}
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
    #endregion
}// EOF CLASS