using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Channels
{
    public abstract class AbstractChannelsFactory
    {
        public abstract IChannel<T> GetChannel<T>();
        public abstract INamedChannel<T> GetChannel<T>(string name);
        public abstract ISubscribableChannel<T> GetSubscribableChannel<T>();
        public abstract ISubscribableNamedChannel<T> GetSubscribableChannel<T>(string name);
    }
}
