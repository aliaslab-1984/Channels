using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Channels
{
    public static class PromisesExtensions
    {
        public static IPromise<T> Then<T>(this IPromise<T> ext, Func<T,IPromise<T>> success, Action<Exception> fail)
        {
            Deferred<T> def = new Deferred<T>();
            IPromise<T> res = def.Promise();
            
            if (fail != null)
                res.Fail(fail);

            ext.Done((value) => success(value).Done(p=>def.Resolve(p)).Fail(e=>def.Reject(e)));
            ext.Fail((e) => def.Reject(e));

            return res;
        }
        public static IPromise<D> Convert<S, D>(this IPromise<S> ext, Func<S, D> transform)
        {
            Deferred<D> def = new Deferred<D>();
            IPromise<D> res = new PromiseImpl<D>();
            
            ext.Done((value) => def.Resolve(transform(value)));
            ext.Fail((e) => def.Reject(e));

            return res;
        }
        public static IPromise<IEnumerable<T>> All<T>(this IEnumerable<IPromise<T>> ext)
        {
            Deferred<IEnumerable<T>> def = new Deferred<IEnumerable<T>>();
            IPromise<IEnumerable<T>> res = new PromiseImpl<IEnumerable<T>>();

            List<IPromise<T>> tmp = ext.ToList();
            List<T> values = new List<T>();

            foreach (IPromise<T> item in tmp)
            {
                item.Done((value) =>
                {
                    tmp.Remove(item);
                    values.Add(value);
                    if (tmp.Count == 0)
                        def.Resolve(values);
                });
                item.Fail((e) =>def.Reject(e));
            }

            return res;
        }
        public static IPromise<T> Any<T>(this IEnumerable<IPromise<T>> ext)
        {
            Deferred<T> def = new Deferred<T>();
            IPromise<T> res = new PromiseImpl<T>();

            List<IPromise<T>> tmp = ext.ToList();
            List<T> values = new List<T>();

            foreach (IPromise<T> item in tmp)
            {
                item.Done((value) => def.Resolve(value));
                item.Fail((e) => def.Reject(e));
            }

            return res;
        }
    }
}
