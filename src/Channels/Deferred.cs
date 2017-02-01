using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Channels
{
    public class Deferred<T>
    {
        private PromiseImpl<T> _promise;

        public bool Resolved { get; private set; }
        public T ResolvedValue { get; private set; }
        public bool Rejected { get; private set; }
        public Exception RejectedException { get; private set; }

        public bool Active { get { return !Resolved && !Rejected; } }

        public Deferred()
        {
            _promise = new PromiseImpl<T>();
        }

        public IPromise<T> Promise()
        {
            return new PromiseImpl<T>();
        }

        public void Resolve(T value)
        {
            if (Active)
            {
                ResolvedValue = value;
                Resolved = _promise.OnDone(ResolvedValue);
            }
        }

        public void Reject(Exception e)
        {
            if (Active)
            {
                RejectedException = e;
                Rejected = _promise.OnFail(RejectedException);
            }
        }
    }
}
