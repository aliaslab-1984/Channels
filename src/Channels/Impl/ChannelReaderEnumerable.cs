using Channels.Exceptions;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Channels.Impl
{
    public class ChannelReaderEnumerable<T> : IEnumerable<T>, IEnumerator<T>
    {
        protected IChannelReader<T> _ch;

        public ChannelReaderEnumerable(IChannelReader<T> ch)
        {
            _ch = ch;
        }

        #region IEnumerator
        public T Current
        {
            get; protected set;
        }

        object IEnumerator.Current
        {
            get
            {
                return Current;
            }
        }

        public bool MoveNext()
        {
            if (_ch.Drained)
                return false;
            else
            {
                try
                {
                    Current = _ch.Read();
                    return true;
                }
                catch (ChannelDrainedException e)
                {
                    return false;
                }
            }
        }

        public void Reset()
        {
            throw new InvalidOperationException();
        }

        public void Dispose()
        {
            _ch.Dispose();
        }
        #endregion

        #region IEnumerable
        public IEnumerator<T> GetEnumerator()
        {
            return this;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this;
        }
        #endregion
    }
}
