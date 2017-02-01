using mq = Channels.RabbitMQ.Impl;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Channels.Impl;

namespace Channels.RabbitMQ
{
    public class RabbitMQChannelsFactory : AbstractChannelsFactory
    {
        public override IChannel<T> GetChannel<T>()
        {
            return new Channel<T>();
        }

        public override INamedChannel<T> GetChannel<T>(string name)
        {
            return new mq.Channel<T>(name);
        }

        public override ISubscribableChannel<T> GetSubscribableChannel<T>()
        {
            return new SubscribableChannel<T>();
        }

        public override ISubscribableNamedChannel<T> GetSubscribableChannel<T>(string name)
        {
            return new mq.SubscribableChannel<T>(name);
        }
    }
}
