using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace MisfitBot_MKII
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
        public static string ConvertMessage(ref BotWideResponseArguments args)
        {
            List<string> parts = GetAllParts(args.message);
            foreach (var part in parts)
            {
                args.message = args.message.Replace(part, GetReplacementValue(args, part));
            }
            return args.message;
        }
        /// <summary>
        /// Checks for patterns to replace. Returns input or replacement value.
        /// </summary>
        /// <param name="pattern"></param>
        /// <returns></returns>
        private static string GetReplacementValue(BotWideResponseArguments args, string pattern)
        {
            string value = string.Empty;
            switch (pattern)
            {
                case "[USER]":
                    if(args.source == MESSAGESOURCE.TWITCH) {
                        value = args.user._twitchDisplayname;
                    }else{
                        value = args.user._discordUsername;
                    }
                    break;
                /*case "[RandomUser]":
                    if (twitchChannel != null)
                    {
                        value = GetRandomUser(twitchChannel);
                    }
                    break;*/
                case "[VICTIM]":
                    if(args.source == MESSAGESOURCE.TWITCH) {
                        value = args.victim._twitchDisplayname;
                    }else{
                        value = args.victim._discordUsername;
                    }
                    break;
            }
            return value;
        }

        /*private static string GetRandomUser(string twitchChannel)
        {
            // TODO MAKE THIS SILLY!!!
            Program.Users._twitchUsers.GetRandomUserInChannel(twitchChannel);
            return "testUser";
        }*/

        /// <summary>
        /// Breaks up the string to a List<string>. Each replacement pattern has their own index.
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
