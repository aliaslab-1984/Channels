using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Channels
{
    public interface IChannelReader<T> : IDisposable
    {
        T Read();
        T Read(int timeoutMillis);
        void Consume(Action<T> action);
        void Consume(Action<T> action, int timeoutMillis);
        IAbortableOperation<T> Consume();
        IAbortableOperation<T> Consume(int timeoutMillis);
        bool Drained { get; }
        WaitHandle WaitReadable { get; }
    }
}
