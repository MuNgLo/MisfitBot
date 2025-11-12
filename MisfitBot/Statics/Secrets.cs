

namespace MisfitBot_MKII.Statics;

public static class Secrets
{
    private readonly static string clientID = "cbFHPhWCx5eiI6w6b9HX477YIFQnJykT0ruZGvK2VuI="; // check
    private readonly static string secret = "LtsN8mudm4CMPu7bHDX5PCniPw+mziXtJLcQ4zrnfBY="; // check
    private static string userID = "";
    private static string userName = ""; // check
    private static string deviceCode = "";
    private static string authToken = "";
    private static string refreshToken = "";



    public static string ClientID => Cipher.Decrypt(clientID);
    public static string UserID => Cipher.Decrypt(userID);
    public static string UserName => Cipher.Decrypt(userName);
    public static string ClientSecret => Cipher.Decrypt(secret);
    public static string DeviceCode => Cipher.Decrypt(deviceCode);
    public static string AuthToken => Cipher.Decrypt(authToken);
    public static string RefreshToken => Cipher.Decrypt(refreshToken);

    public static void SetUserID(string newID)
    {
        userID = Cipher.Encrypt(newID);
    }
    public static void SetUserName(string newName)
    {
        userName = Cipher.Encrypt(newName);
    }
    public static void SetDeviceCode(string newCode)
    {
        deviceCode = Cipher.Encrypt(newCode);
    }
    public static void SetAuthToken(string newToken)
    {
        authToken = Cipher.Encrypt(newToken);
    }
    public static void SetRefreshToken(string newToken)
    {
        refreshToken = Cipher.Encrypt(newToken);
    }
}// EOF CLASS