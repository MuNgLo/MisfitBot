using System;
using System.Collections.Generic;
using System.Text;

namespace MisfitBot2
{
    /// <summary>
    /// Static class to format strings. Replacing predefined fields. See the GetReplacementValues method.
    /// </summary>
    public static class StringFormatter
    {
        /// <summary>
        /// This should be the only public way of formatting a message.
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public static string ConvertMessage(string message, string user, string targetUser, string twitchChannel)
        {
            List<string> parts = GetAllParts(message);

            foreach (var part in parts)
            {
                message = message.Replace(part, GetReplacementValue(part, user, targetUser, twitchChannel));
            }

            return message;
        }
        /// <summary>
        /// Checks for patterns to replace. Returns input or replacement value.
        /// </summary>
        /// <param name="pattern"></param>
        /// <returns></returns>
        private static string GetReplacementValue(string pattern, string user, string targetUser, string twitchChannel)
        {
            string value = string.Empty;
            switch (pattern)
            {
                case "[User]":
                    if (user != null)
                    {
                        value = user;
                    }
                    break;
                case "[RandomUser]":
                    if (twitchChannel != null)
                    {
                        value = GetRandomUser(twitchChannel);
                    }
                    break;
                case "[TargetUser]":
                    if (targetUser != null)
                    {
                        value = targetUser;
                    }
                    break;
            }
            return value;
        }

        private static string GetRandomUser(string twitchChannel)
        {
            // TODO MAKE THIS SILLY!!!
            string rngUser = Core.Twitch._twitchUsers.GetRandomUserInChannel(twitchChannel);
            if(rngUser == null)
            {
                return "NULLUser";
            }
            if (rngUser == string.Empty)
            {
                return "EmptyUser";
            }
            return rngUser;
        }

        /// <summary>
        /// Breaks up the string to a List<string>. Each replacement pattern ahs their own index.
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        private static List<string> GetAllParts(string message)
        {
            var parts = new List<string>();
            var startpos = 0;
            startpos = message.IndexOf("[", startpos);
            while (startpos >= 0)
            {
                var endpos = message.IndexOf("]", startpos);
                var part = message.Substring(startpos, (endpos - startpos)+1);
                parts.Add(part);
                startpos = message.IndexOf("[", endpos);
            }
            return parts;
        }
    }
}
