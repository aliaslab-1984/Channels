using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Channels.Exceptions
{
    public class ChannelDrainedException : InvalidOperationException
    {
        public ChannelDrainedException()
            :base()
        { }

        public ChannelDrainedException(string msg)
            :base(msg)
        { }
    }
}
