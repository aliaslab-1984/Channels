using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Channels
{
    public interface INamedChannel<T> : INamedChannelReader<T>, INamedChannelWriter<T>, IChannel<T>
    {
        new string Name { get; }
    }
}
