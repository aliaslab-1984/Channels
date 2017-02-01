using Channels.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Channels.Impl
{
    internal class ChannelReader<T> : IChannelReader<T>
    {
        protected Queue<T> _buffer;
        protected int _bufferSize;

        protected ManualResetEvent _waitReadable;
        protected ManualResetEvent _waitWriteable;
        
        protected bool _disposed = false;
        
        public ChannelReader(Queue<T> buffer, int bufferSize, ManualResetEvent waitReadable, ManualResetEvent waitWriteable)
        {
            _buffer = buffer;
            _bufferSize = bufferSize;

            _waitReadable = waitReadable;
            _waitWriteable = waitWriteable;
        }

        public void Dispose()
        {
            lock (_buffer)
            {
                if (!_disposed)
                {
                    _buffer.Clear();

                    _disposed = true;
                }
            }
        }

        public bool WriterClosed { get; set; }

        #region IChannelReader
        public bool Drained
        {
            get
            {
                return WriterClosed && _buffer.Count == 0;
            }
        }
        public WaitHandle WaitReadable
        {
            get
            {
                return _waitReadable;
            }
        }
        public T Read()
        {
            return Read(Timeout.Infinite);
        }

        public T Read(int timeoutMillis)
        {
            IAbortableOperation<T> temp = Consume(timeoutMillis);

            try
            {
                return temp.Value;
            }
            finally
            {
                temp.Commit();
            }
        }

        public void Consume(Action<T> action)
        {
            Consume(action, Timeout.Infinite);
        }

        public void Consume(Action<T> action, int timeoutMillis)
        {
            IAbortableOperation<T> temp = Consume(timeoutMillis);
            try
            {
                action(temp.Value);
            }
            catch (Exception e)
            {
                temp.Abort();
                throw new OperationCanceledException("Consume cancelled by action exception.", e);
            }
            temp.Commit();
        }
        public IAbortableOperation<T> Consume()
        {
            return Consume(Timeout.Infinite);
        }

        public IAbortableOperation<T> Consume(int timeoutMillis)
        {
            try
            {
                using (TimeoutManager timeout = TimeoutManager.Start(timeoutMillis))
                {
                    if (!Monitor.TryEnter(_buffer, timeout))
                        throw new TimeoutException("Timeout locking queue.");

                    while (_buffer.Count == 0)
                    {
                        if (Drained)
                            throw new ChannelDrainedException("Impossible to read other data, channel drained.");
                        _waitReadable.Reset();
                        if (!Monitor.Wait(_buffer, timeout))
                            throw new TimeoutException("Timeout waiting readable state.");
                    }

                    return new AbortableOperationImpl<T>(_buffer.Peek(),
                        () =>
                        {
                            _buffer.Dequeue();
                            Monitor.PulseAll(_buffer);
                            _waitWriteable.Set();

                            if (_buffer.Count == 0)
                                _waitReadable.Reset();

                            Monitor.Exit(_buffer);
                        },
                        () =>
                        {
                            Monitor.Exit(_buffer);
                        });
                }
            }
            catch (Exception e)
            {
                if (Monitor.IsEntered(_buffer))
                    Monitor.Exit(_buffer);
                throw e;
            }
        }
        #endregion
    }
}
