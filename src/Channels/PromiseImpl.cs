using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Channels
{
    internal class PromiseImpl<T> : IPromise<T>
    {
        internal event Action AlwaysEvent;
        internal event Action<T> DoneEvent;
        internal event Action<Exception> FailEvent;
        
        internal bool OnAlways()
        {
            AlwaysEvent?.Invoke();
            return AlwaysEvent != null;
        }

        internal bool OnDone(T value)
        {
            DoneEvent?.Invoke(value);
            OnAlways();
            return DoneEvent != null;
        }

        internal bool OnFail(Exception e)
        {
            FailEvent?.Invoke(e);
            OnAlways();
            return FailEvent != null;
        }

        public IPromise<T> Done(Action<T> action)
        {
            DoneEvent += action;
            return this;
        }

        public IPromise<T> Fail(Action<Exception> action)
        {
            FailEvent += action;
            return this;
        }

        public IPromise<T> Always(Action action)
        {
            AlwaysEvent += action;
            return this;
        }
    }
}
