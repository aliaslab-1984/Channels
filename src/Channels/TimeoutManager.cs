using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Channels
{
    public class TimeoutManager : IDisposable
    {
        protected int _timeout;
        protected Stopwatch _wc;

        public static implicit operator int(TimeoutManager obj)
        {
            return obj == null ? Timeout.Infinite : obj.TimeoutMillis;
        }

        public static TimeoutManager Start(int timeoutMillis)
        {
            return new TimeoutManager(timeoutMillis);
        }

        protected TimeoutManager(int timeoutMillis)
        {
            _wc = Stopwatch.StartNew();
            _timeout = timeoutMillis;
        }

        public int TimeoutMillis
        {
            get
            {
                if (_timeout != Timeout.Infinite)
                {
                    int elapsed = (int)_wc.ElapsedMilliseconds;
                    _timeout = _timeout > elapsed ? _timeout - elapsed : 1;
                    _wc.Reset();
                }
                return _timeout;
            }
        }

        public void Dispose()
        {
            _wc.Stop();
            _timeout = 0;
        }
    }
}
