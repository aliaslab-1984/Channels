using Channels.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Channels.Impl
{
    internal class ChannelWriter<T> : IChannelWriter<T>
    {
        protected Queue<T> _buffer;
        protected int _bufferSize;

        protected ManualResetEvent _waitReadable;
        protected ManualResetEvent _waitWriteable;

        protected bool _closed = false;

        protected bool _disposed = false;
        
        public ChannelWriter(Queue<T> buffer, int bufferSize, ManualResetEvent waitReadable, ManualResetEvent waitWriteable)
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
                    _waitReadable.Dispose();
                    _waitWriteable.Dispose();

                    _disposed = true;
                }
            }
        }

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
            lock (_buffer)
            {
                _closed = true;

                Monitor.PulseAll(_buffer);
                _waitReadable.Set();
            }
        }

        #endregion
    }
}
