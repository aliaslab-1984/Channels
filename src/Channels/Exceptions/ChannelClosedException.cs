using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Channels.Exceptions
{
    public class ChannelClosedException : InvalidOperationException
    {
        public ChannelClosedException()
            :base()
        {}
        public ChannelClosedException(string msg)
            :base(msg)
        {}
    }
}
