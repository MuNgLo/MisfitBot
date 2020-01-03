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
        public static string ConvertMessage(StringFormatterArguments args)
        {
            if(args.message == null)
            {
                Core.LOG(new Discord.LogMessage(Discord.LogSeverity.Error, "STRINGFORMATTER", "CovertMessage() Message is NULL"));
                return "NULL Message";
            }
            List<string> parts = GetAllParts(args.message);
            string outMsg = args.message;
            foreach (var part in parts)
            {
                outMsg = args.message.Replace(part, GetReplacementValue(part, args.user, args.targetUser, args.twitchChannel));
            }

            return outMsg;
        }
        /// <summary>
        /// Checks for patterns to replace. Returns input or replacement value.
        /// </summary>
        /// <param name="pattern"></param>
        /// <returns></returns>
        private static string GetReplacementValue(string pattern, string user, string targetUser, string twitchChannel)
        {
            string value = string.Empty;
            switch (pattern.ToLower())
            {
                case "[user]":
                    if (user != null)
                    {
                        value = user;
                    }
                    break;
                case "[randomuser]":
                    if (twitchChannel != null)
                    {
                        value = GetRandomUser(twitchChannel);
                    }
                    break;
                case "[targetuser]":
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
