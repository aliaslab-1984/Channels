using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Channels.Impl
{
    public class FuncChannelOutputAdapter<S, D> : AbstractChannelOutputAdapter<S, D>
    {
        protected Func<S, D> _func;
        public FuncChannelOutputAdapter(IChannelReader<S> channel, Func<S,D> transform) 
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
