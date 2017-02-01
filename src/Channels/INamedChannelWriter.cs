using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Channels
{
    public interface INamedChannelWriter<T> : IChannelWriter<T>
    {
        string Name { get; }
    }
}
