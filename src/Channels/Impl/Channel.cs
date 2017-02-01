using Channels.Exceptions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Channels.Impl
{
    public class Channel<T> : IChannel<T>
    {
        protected Queue<T> _buffer;
        protected int _bufferSize;

        protected ManualResetEvent _waitReadable;
        protected ManualResetEvent _waitWriteable;

        protected bool _closed = false;

        protected bool _disposed = false;

        public Channel()
            :this(1)
        {}

        public Channel(int bufferSize)
        {
            if (bufferSize <= 0)
                throw new ArgumentException(string.Format("bufferSize must be greater than 0. bufferSize={0}", bufferSize));

            _buffer = new Queue<T>(bufferSize);
            _bufferSize = bufferSize;
            
            _waitReadable = new ManualResetEvent(false);
            _waitWriteable = new ManualResetEvent(true);
        }

        public void Dispose()
        {
            lock (_buffer)
            {
                if (!_disposed)
                {
                    _buffer.Clear();
                    _waitReadable.Dispose();
                    _waitWriteable.Dispose();

                    _disposed = true;
                }
            }
        }

        #region IChannelReader
        public bool Drained
        {
            get
            {
                return _closed && _buffer.Count==0;
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

        #region IChannelWriter

        public void Write(T value)
        {
            Write(value, Timeout.Infinite);
        }

        public void Write(T value, int timeoutMillis)
        {
            using (TimeoutManager timeout = TimeoutManager.Start(timeoutMillis))
            {
                try
                {
                    if (!Monitor.TryEnter(_buffer, timeout))
                        throw new TimeoutException("Timeout locking queue.");

                    if (Closed)
                        throw new ChannelClosedException("Impossible to send on a colsed channel.");

                    while (_buffer.Count == _bufferSize)
                    {
                        _waitWriteable.Reset();
                        if (!Monitor.Wait(_buffer, timeout))
                            throw new TimeoutException("Timeout waiting writeable state.");
                    }

                    _buffer.Enqueue(value);

                    Monitor.PulseAll(_buffer);
                    _waitReadable.Set();

                    if (_buffer.Count == _bufferSize)
                        _waitWriteable.Reset();
                }
                finally
                {
                    Monitor.Exit(_buffer);
                }
            }
        }

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
                return _waitWriteable;
            }
        }
        public void Close()
        {
            lock(_buffer)
            {
                _closed = true;

                Monitor.PulseAll(_buffer);
                _waitReadable.Set();
            }
        }

        #endregion
    }
}
