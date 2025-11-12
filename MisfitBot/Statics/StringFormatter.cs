using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace MisfitBot_MKII.Statics
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
        public static string ConvertMessage(string message, Dictionary<string, string> filter)
        {
            List<string> parts = GetAllParts(message);
            foreach (var part in parts)
            {
                message = message.Replace(part, GetReplacementValue(part, filter));
            }
            return message;
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
                        value = args.user.twitchDisplayName;
                    }else{
                        value = args.user.discordUsername;
                    }
                    break;
                /*case "[RandomUser]":
                    if (twitchChannel != null)
                    {
                        value = GetRandomUser(twitchChannel);
                    }
                    break;*/
                case "[VICTIM]":
                        value = args.victim.ContextName(args.source);
                    break;
                /*case "[EVENT]":
                        value = args.victim.ContextName(args.source);
                    break;*/
                case "[EVENTUSER]":
                        value = args.victim.ContextName(args.source);
                    break;
            }
            return value;
        }
        private static string GetReplacementValue(string pattern, Dictionary<string, string> filter)
        {
            string value = string.Empty;
            if (filter.ContainsKey(pattern))
            {
                //Core.LOG($"GetReplacementValue matched pattern() with a key");
                value = filter[pattern];
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
            var startPosition = 0;
            startPosition = message.IndexOf("[", startPosition);
            while (startPosition >= 0)
            {
                var endPosition = message.IndexOf("]", startPosition);
                var part = message.Substring(startPosition, (endPosition - startPosition)+1);
                parts.Add(part);
                startPosition = message.IndexOf("[", endPosition);
            }
            return parts;
        }
    }
}
