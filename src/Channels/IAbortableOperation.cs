using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Channels
{
    public interface IAbortableOperation<T> : IDisposable
    {
        T Value { get; }
        void Abort();
        void Commit();
    }
}
