using Channels.Exceptions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Channels.Impl
{
    public class SubscribableChannel<T> : ISubscribableChannel<T>
    {
        protected bool _closed = false;

        protected bool _disposed = false;

        protected List<IChannel<T>> _subscribers = new List<IChannel<T>>();
        
        public bool Closed
        {
            get
            {
                return _closed;
            }
        }

        public WaitHandle WaitWriteable
        {
            get
            {
                throw new NotSupportedException("Impossible to reliably determine if a write operation will block or not.");
            }
        }

        public void Close()
        {
            lock (_subscribers)
            {
                _closed = true;
                foreach (IChannel<T> item in _subscribers)
                {
                    if(!item.Closed)
                        item.Close();
                }
            }
        }

        public void Dispose()
        {
            lock (_subscribers)
            {
                if (!_disposed)
                {
                    foreach (IChannel<T> item in _subscribers)
                    {
                        item.Dispose();
                    }
                    _subscribers.Clear();
                    _subscribers = null;

                    _disposed = true;
                }
            }
        }

        public IChannelReader<T> Subscribe()
        {
            return Subscribe(1);
        }
        
        public IChannelReader<T> Subscribe(int bufferSize)
        {
            lock(_subscribers)
            {
                if (_closed)
                    throw new ChannelClosedException("Write end closed. Impossible to subscribe.");
                IChannel<T> ch = new Channel<T>(bufferSize);
                _subscribers.Add(ch);

                return ch;
            }
        }

        public void Write(T value)
        {
            lock(_subscribers)
            {
                if (_closed)
                    throw new ChannelClosedException("Write end closed. Impossible to write.");
                foreach (IChannelWriter<T> item in _subscribers.WriteBarrierWith())
                {
                    if(!item.Closed)
                        item.Write(value);
                }
            }
        }

        public void Write(T value, int timeoutMillis)
        {
            using (TimeoutManager timeout = TimeoutManager.Start(timeoutMillis))
            {
                try
                {
                    if (!Monitor.TryEnter(_subscribers, timeout))
                        throw new TimeoutException("Timeout locking queue.");

                    if (_closed)
                        throw new ChannelClosedException("Write end closed. Impossible to write.");
                    foreach (IChannelWriter<T> item in _subscribers.WriteBarrierWith(timeout))
                    {
                        if (!item.Closed)
                            item.Write(value, timeout);
                    }
                }
                finally
                {
                    Monitor.Exit(_subscribers);
                }
            }
        }
    }
}
