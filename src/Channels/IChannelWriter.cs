using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Channels
{
    public interface IChannelWriter<T> : IDisposable
    {
        void Write(T value);
        void Write(T value, int timeoutMillis);
        void Close();
        bool Closed { get; }
        WaitHandle WaitWriteable { get; }
    }
}
