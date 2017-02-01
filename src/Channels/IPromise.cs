using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Channels
{
    public interface IPromise<T>
    {
        IPromise<T> Done(Action<T> action);
        IPromise<T> Fail(Action<Exception> action);
        IPromise<T> Always(Action action);
    }
}
