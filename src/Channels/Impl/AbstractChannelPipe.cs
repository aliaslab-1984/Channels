using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Channels.Impl
{
    public abstract class AbstractChannelPipe<T> : IChannel<T>
    {
        protected IChannel<T> _source;
        protected IChannel<T> _destiantion;
        public AbstractChannelPipe(IChannel<T> source, IChannel<T> destination)
        {
            _source = source;
            _destiantion = destination;
        }

        public bool Closed
        {
            get
            {
                return _source.Closed;
            }
        }

        public virtual bool Drained
        {
            get
            {
                return _destiantion.Drained;
            }
        }

        public virtual WaitHandle WaitReadable
        {
            get
            {
                return _destiantion.WaitReadable;
            }
        }

        public WaitHandle WaitWriteable
        {
            get
            {
                return _source.WaitWriteable;
            }
        }

        public void Close()
        {
            _source.Close();
            _destiantion.Close();
        }

        public virtual void Dispose()
        {
            _source.Dispose();
            _destiantion.Close();
        }

        public abstract T Pipe(T value);

        public T Read()
        {
            return Read(Timeout.Infinite);
        }

        public T Read(int timeoutMillis)
        {
            using (TimeoutManager timeout = TimeoutManager.Start(timeoutMillis))
            {
                T temp;
                _source.Consume(t =>
                {
                    temp = Pipe(t);

                    _destiantion.Write(temp, timeout);
                }, timeout);

                return _destiantion.Read(timeout);
            }
        }
        public void Consume(Action<T> action)
        {
            Consume(action, Timeout.Infinite);
        }
        public void Consume(Action<T> action, int timeoutMillis)
        {
            using (TimeoutManager timeout = TimeoutManager.Start(timeoutMillis))
            {
                _source.Consume(t => 
                {
                    T temp = Pipe(t);
                    
                    _destiantion.Write(temp, timeout);
                }, timeout);
                
                _destiantion.Consume(action, timeout);
            }
        }
        public IAbortableOperation<T> Consume()
        {
            return Consume(Timeout.Infinite);
        }
        public IAbortableOperation<T> Consume(int timeoutMillis)
        {
            using (TimeoutManager timeout = TimeoutManager.Start(timeoutMillis))
            {
                IAbortableOperation<T> rd = _source.Consume(timeout);

                _destiantion.Write(Pipe(rd.Value), timeout);

                IAbortableOperation<T> wr = _destiantion.Consume(timeout); 

                return new AbortableOperationImpl<T>(wr.Value, ()=> { rd.Commit(); wr.Commit(); }, ()=> { wr.Abort(); rd.Abort(); });
            }
        }

        public void Write(T value)
        {
            _source.Write(value);
        }

        public void Write(T value, int timeoutMillis)
        {
            _source.Write(value, timeoutMillis);
        }
    }
}
