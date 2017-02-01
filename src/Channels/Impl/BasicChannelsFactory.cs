using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Channels.Impl
{
    public class BasicChannelsFactory : AbstractChannelsFactory
    {
        public override IChannel<T> GetChannel<T>()
        {
            return new Channel<T>();
        }
        public virtual IChannel<T> GetChannel<T>(int bufferSize)
        {
            return new Channel<T>(bufferSize);
        }

        public override INamedChannel<T> GetChannel<T>(string name)
        {
            throw new NotSupportedException();
        }

        public override ISubscribableChannel<T> GetSubscribableChannel<T>()
        {
            return new SubscribableChannel<T>();
        }

        public override ISubscribableNamedChannel<T> GetSubscribableChannel<T>(string name)
        {
            throw new NotSupportedException();
        }
    }
}
