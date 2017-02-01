using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Channels.Impl
{
    public abstract class AbstractChannelInputAdapter<S, D> : IChannelWriter<S>
    {
        protected IChannelWriter<D> _channel;
        public AbstractChannelInputAdapter(IChannelWriter<D> channel)
        {
            _channel = channel;
        }

        public bool Closed
        {
            get
            {
                return _channel.Closed;
            }
        }
        
        public WaitHandle WaitWriteable
        {
            get
            {
                return _channel.WaitWriteable;
            }
        }

        public void Close()
        {
            _channel.Close();
        }

        public virtual void Dispose()
        {
            _channel.Dispose();
        }
        
        public abstract D Transform(S value);

        public void Write(S value)
        {
            _channel.Write(Transform(value));
        }
        public void Write(S value, int timeoutMillis)
        {
            _channel.Write(Transform(value), timeoutMillis);
        }
    }
}
