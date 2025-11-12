using System;
using System.Collections.Generic;
using System.Text;

namespace ShoutOut
{
    public class ShoutOutArguments
    {
        readonly public string ChannelName;
        public int viewers = 0;
        public string game = string.Empty;

        public ShoutOutArguments (string channelname)
        {
            ChannelName = channelname;
        }

    }// EOF CLASS
}
