using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Channels.Impl
{
    public class FuncActiveEnumerator<S> : AbstractActiveEnumerator<S>
    {
        protected Action<S> _func;
        public FuncActiveEnumerator(Action<S> handle, IChannelReader<S> source, TimeSpan lifeTime) 
            : base(source, lifeTime)
        {
            _func = handle;
        }

        public override void Handle(S value)
        {
            _func(value);
        }
    }
}
