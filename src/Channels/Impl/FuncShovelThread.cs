using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Channels.Impl
{
    public class FuncShovelThread<S, D> : AbstractShovelThread<S, D>
    {
        protected Func<S, D> _func;
        public FuncShovelThread(Func<S, D> transform, IChannelReader<S> source, IChannelWriter<D> destination, TimeSpan lifeTime, bool closeOnDrain) 
            : base(source, destination, lifeTime, closeOnDrain)
        {
            _func = transform;
        }
        
        public override D Transform(S value)
        {
            return _func(value);
        }
    }
}
