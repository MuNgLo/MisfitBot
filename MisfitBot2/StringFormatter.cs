using System;
using System.Collections.Generic;
using System.Text;

namespace MisfitBot2
{
    public static class StringFormatter
    {
        public static string ConvertMessage(string message)
        {
            var parts = GetAllParts(message);

            foreach (var part in parts)
            {
                message = message.Replace(part, GetReplacementValue(part));
            }

            return message;
        }

        private static string GetReplacementValue(string command)
        {
            var value = string.Empty;

            switch (command)
            {
                case "[User]":
                    value = "CurrentUser";
                    break;
                case "[RandomUser]":
                    value = GetRandomUser();
                    break;
                case "[TargetUser]":
                    value = "TargetUser";
                    break;
            }

            return value;
        }

        private static string GetRandomUser()
        {

            return "testUser";
        }

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
