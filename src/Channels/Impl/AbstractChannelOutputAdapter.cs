using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Channels.Impl
{
    public abstract class AbstractChannelOutputAdapter<S, D> : IChannelReader<D>
    {
        protected IChannelReader<S> _channel;
        public AbstractChannelOutputAdapter(IChannelReader<S> channel)
        {
            _channel = channel;
        }
        
        public virtual bool Drained
        {
            get
            {
                return _channel.Drained;
            }
        }

        public virtual WaitHandle WaitReadable
        {
            get
            {
                return _channel.WaitReadable;
            }
        }
        
        public virtual void Dispose()
        {
            _channel.Dispose();
        }

        public D Read()
        {
            return Transform(_channel.Read());
        }

        public D Read(int timeoutMillis)
        {
            return Transform(_channel.Read(timeoutMillis));
        }

        public void Consume(Action<D> action)
        {
            _channel.Consume(t=>action(Transform(t)));
        }

        public void Consume(Action<D> action, int timeoutMillis)
        {
            _channel.Consume(t => action(Transform(t)), timeoutMillis);
        }

        public IAbortableOperation<D> Consume()
        {
            IAbortableOperation<S> temp = _channel.Consume();
            return new AbortableOperationImpl<D>(Transform(temp.Value),temp.Commit,temp.Abort);
        }

        public IAbortableOperation<D> Consume(int timeoutMillis)
        {
            IAbortableOperation<S> temp = _channel.Consume(timeoutMillis);
            return new AbortableOperationImpl<D>(Transform(temp.Value), temp.Commit, temp.Abort);
        }

        public abstract D Transform(S value);
    }
}
