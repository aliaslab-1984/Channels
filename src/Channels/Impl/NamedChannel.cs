using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Channels.Impl
{
    public class NamedChannel<T> : INamedChannel<T>
    {
        protected Queue<T> _buffer;
        protected int _bufferSize;

        protected ManualResetEvent _waitReadable;
        protected ManualResetEvent _waitWriteable;

        protected bool _disposed = false;

        private ChannelReader<T> _reader;
        private ChannelWriter<T> _writer;

        public NamedChannel(string name)
            :this(name, 1)
        { }

        public NamedChannel(string name,int bufferSize)
        {
            if (bufferSize <= 0)
                throw new ArgumentException(string.Format("bufferSize must be greater than 0. bufferSize={0}", bufferSize));

            _buffer = new Queue<T>(bufferSize);
            _bufferSize = bufferSize;

            _waitReadable = new ManualResetEvent(false);
            _waitWriteable = new ManualResetEvent(true);

            _reader = new Impl.ChannelReader<T>(_buffer, _bufferSize, _waitReadable, _waitWriteable);
            _writer = new ChannelWriter<T>(_buffer, bufferSize, _waitReadable, _waitWriteable);

            Name = name;

            ChannelsRepository.AddNamedReader<T>(name, _reader, _buffer);
            ChannelsRepository.AddNamedWriter<T>(name, _writer, _buffer);
        }

        public void Dispose()
        {
            lock (_buffer)
            {
                if (!_disposed)
                {
                    _waitReadable.Dispose();
                    _waitWriteable.Dispose();

                    _reader.Dispose();
                    _writer.Dispose();

                    ChannelsRepository.RemoveNamedReader<T>(Name,_reader);
                    ChannelsRepository.RemoveNamedWriter<T>(Name,_writer);

                    _disposed = true;
                }
            }
        }

        #region IChannelReader
        public bool Drained
        {
            get
            {
                return _writer.Closed && _reader.Drained;
            }
        }
        public WaitHandle WaitReadable
        {
            get
            {
                return _reader.WaitReadable;
            }
        }
        public T Read()
        {
            return _reader.Read(Timeout.Infinite);
        }

        public T Read(int timeoutMillis)
        {
            return _reader.Read(timeoutMillis);
        }

        public void Consume(Action<T> action)
        {
            _reader.Consume(action, Timeout.Infinite);
        }

        public void Consume(Action<T> action, int timeoutMillis)
        {
            _reader.Consume(action, timeoutMillis);
        }
        public IAbortableOperation<T> Consume()
        {
            return _reader.Consume(Timeout.Infinite);
        }

        public IAbortableOperation<T> Consume(int timeoutMillis)
        {
            return _reader.Consume(timeoutMillis);
        }
        #endregion

        #region IChannelWriter

        public void Write(T value)
        {
            _writer.Write(value, Timeout.Infinite);
        }

        public void Write(T value, int timeoutMillis)
        {
            _writer.Write(value, timeoutMillis);
        }

        public bool Closed
        {
            get
            {
                return _writer.Closed;
            }
        }
        public WaitHandle WaitWriteable
        {
            get
            {
                return _writer.WaitWriteable;
            }
        }
        public void Close()
        {
            lock (_buffer)
            {
                _reader.WriterClosed = true;
                _writer.Close();
            }
        }

        #endregion

        public string Name
        {
            get; protected set;
        }
    }
}
