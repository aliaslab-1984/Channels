using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Channels
{
    public interface INamedChannelReader<T> : IChannelReader<T>
    {
        string Name { get; }
    }
}
