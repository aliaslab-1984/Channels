using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Channels.Impl
{
    public class FuncChannelInputAdapter<S, D> : AbstractChannelInputAdapter<S, D>
    {
        protected Func<S, D> _func;
        public FuncChannelInputAdapter(IChannelWriter<D> channel, Func<S,D> transform) 
            : base(channel)
        {
            _func = transform;
        }

        public override D Transform(S value)
        {
            return _func(value);
        }
    }
}
