using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Channels
{
    public interface IChannel<T> : IChannelReader<T>, IChannelWriter<T>
    {
    }
}
