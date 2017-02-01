using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Channels.Impl
{
    public class CompositeChannel<T> : IChannel<T>
    {
        protected IChannelWriter<T> _source;
        protected IChannelReader<T> _destiantion;

        public CompositeChannel(IChannelWriter<T> source, IChannelReader<T> destiantion)
        {
            _source = source;
            _destiantion = destiantion;
        }

        public bool Drained
        {
            get
            {
                return _destiantion.Drained;
            }
        }

        public WaitHandle WaitReadable
        {
            get
            {
                return _destiantion.WaitReadable;
            }
        }

        public bool Closed
        {
            get
            {
                return _source.Closed;
            }
        }

        public WaitHandle WaitWriteable
        {
            get
            {
                return _source.WaitWriteable;
            }
        }
        
        public T Read()
        {
            return _destiantion.Read();
        }

        public T Read(int timeoutMillis)
        {
            return _destiantion.Read(timeoutMillis);
        }

        public void Consume(Action<T> action)
        {
            _destiantion.Consume(action);
        }
        public void Consume(Action<T> action, int timeoutMillis)
        {
            _destiantion.Consume(action, timeoutMillis);
        }
        public IAbortableOperation<T> Consume()
        {
            return _destiantion.Consume();
        }
        public IAbortableOperation<T> Consume(int timeoutMillis)
        {
            return _destiantion.Consume(timeoutMillis);
        }

        public void Write(T value)
        {
            _source.Write(value);
        }
        public void Write(T value, int timeoutMillis)
        {
            _source.Write(value, timeoutMillis);
        }

        public void Close()
        {
            _source.Close();
        }

        public void Dispose()
        {
            _source.Dispose();
            _destiantion.Dispose();
        }
    }
}
