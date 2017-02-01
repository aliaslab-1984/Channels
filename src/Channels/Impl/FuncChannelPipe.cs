using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Channels.Impl
{
    public class FuncChannelPipe<T> : AbstractChannelPipe<T>
    {
        protected Func<T, T> _func;
        public FuncChannelPipe(IChannel<T> source, IChannel<T> destination, Func<T,T> transform)
            : base(source, destination)
        {
            _func = transform;
        }

        public override T Pipe(T value)
        {
            return _func(value);
        }
    }
}
