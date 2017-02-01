using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Channels.Impl
{
    public abstract class AbstractActiveEnumerator<S> : IDisposable
    {
        protected bool _stopped = false;
        protected Thread _th;

        protected TimeSpan _lifeTime;
        protected DateTime _startDate;

        protected IChannelReader<S> _source;

        protected Exception _stopException;
        protected event Action<Exception> _stoppedEvent;
        public event Action<Exception> StoppedEvent
        {
            add
            {
                _stoppedEvent += value;
                if (_stopped)
                    value(_stopException);
            }
            remove
            {
                _stoppedEvent -= value;
            }
        }

        public void OnStoppedEvent(Exception e)
        {
            _stoppedEvent?.Invoke(e);
        }

        public AbstractActiveEnumerator(IChannelReader<S> source, TimeSpan lifeTime)
        {
            if (source == null)
                throw new ArgumentNullException("'source' cannot be null.");
            _source = source;
            _lifeTime = lifeTime;

            _th = new Thread(Run);
        }

        public abstract void Handle(S value);

        protected virtual void Run()
        {
            try
            {
                for (;;)
                {
                    if (_stopped)
                        throw new Exception("Stopped by application");
                    if (DateTime.Now - _startDate > _lifeTime)
                        throw new Exception("Lifetime expired.");

                    _source.Consume(value =>
                    {
                        Handle(value);
                    });
                }
            }
            catch (ThreadAbortException e)
            {
                OnStoppedEvent(e);
                _stopException = e;
                _stopped = true;
            }
            catch (Exceptions.ChannelDrainedException e)
            {
                OnStoppedEvent(e);
                _stopException = e;
                _stopped = true;
            }
            catch (Exception e)
            {
                OnStoppedEvent(e);
                _stopException = e;
                _stopped = true;
            }
        }

        public void Start()
        {
            _stopException = null;
            _stopped = false;
            _startDate = DateTime.Now;
            _th.Start();
        }

        public void Stop()
        {
            _stopped = true;
            if (!_th.Join(1000))
                _th.Abort();
        }

        public virtual void Dispose()
        {
            Stop();

            _source.Dispose();

            _stoppedEvent = null;
        }
    }
}
