using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace Channels.Impl
{
    public abstract class AbstractShovelThread<S,D> : AbstractActiveEnumerator<S>
    {
        protected IChannelWriter<D> _destination;
        protected bool _closeOnDrain = false;

        public AbstractShovelThread(IChannelReader<S> source, IChannelWriter<D> destination, TimeSpan lifeTime, bool closeOnDrain)
            :base(source, lifeTime)
        {
            if (destination == null)
                throw new ArgumentNullException("'destination' cannot be null.");
            _destination = destination;
            _closeOnDrain = closeOnDrain;

            _th = new Thread(Run);

            StoppedEvent += ChannelDrainedHandling;
        }

        private void ChannelDrainedHandling(Exception obj)
        {
            if (obj is Exceptions.ChannelDrainedException)
            {
                if (_closeOnDrain)
                    _destination.Close();
            }
        }

        public abstract D Transform(S value);

        public override void Handle(S value)
        {
            _destination.Write(Transform(value));
        }
        
        public override void Dispose()
        {
            StoppedEvent -= ChannelDrainedHandling;
            base.Dispose();
            _destination.Dispose();
        }
    }
}
