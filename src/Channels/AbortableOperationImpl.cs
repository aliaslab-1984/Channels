using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Channels
{
    public class AbortableOperationImpl<T> : IAbortableOperation<T>
    {
        protected Action _succed;
        protected Action _fail;

        protected bool _disposed = false;
        public AbortableOperationImpl(T value, Action succed, Action fail)
        {
            Value = value;
            _succed = succed;
            _fail = fail;
        }

        public T Value
        {
            get; protected set;
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                Commit();
                _disposed = true;
            }
        }

        public void Abort()
        {
            if (!_disposed)
            {
                _fail();
                _disposed = true;
            }
        }

        public void Commit()
        {
            if (!_disposed)
            {
                _succed();
                _disposed = true;
            }
        }
    }
}
