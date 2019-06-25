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
        public static string ConvertMessage(string message)
        {
            List<string> parts = GetAllParts(message);

            foreach (var part in parts)
            {
                message = message.Replace(part, GetReplacementValue(part));
            }

            return message;
        }
        /// <summary>
        /// Checks for patterns to replace. Returns input or replacement value.
        /// </summary>
        /// <param name="pattern"></param>
        /// <returns></returns>
        private static string GetReplacementValue(string pattern, string twitchChannel=null)
        {
            string value = string.Empty;
            switch (pattern)
            {
                case "[User]":
                    value = "CurrentUser";
                    break;
                case "[RandomUser]":
                    if (twitchChannel != null)
                    {
                        value = GetRandomUser(twitchChannel);
                    }
                    break;
                case "[TargetUser]":
                    value = "TargetUser";
                    break;
            }
            return value;
        }

        private static string GetRandomUser(string twitchChannel)
        {
            // TODO MAKE THIS SILLY!!!
            Core.Twitch._twitchUsers.GetRandomUserInChannel(twitchChannel);
            return "testUser";
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
