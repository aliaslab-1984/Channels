using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Channels
{
    public interface ISubscribableChannel<S>: IChannelWriter<S>
    {
        IChannelReader<S> Subscribe();
    }
}
