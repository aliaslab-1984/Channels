using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Channels.Impl
{
    public static class ChannelReaderSync
    {
        public static IChannelReader<T> Select<T>(params IChannelReader<T>[] channels)
        {
            return Select<T>(Timeout.Infinite, channels);
        }
        public static IChannelReader<T> Select<T>(int timeoutMillis, params IChannelReader<T>[] channels)
        {
            if (channels.Length > 0)
            {
                int idx = WaitHandle.WaitAny(channels.Select(p => p.WaitReadable).ToArray(), timeoutMillis);

                return idx == WaitHandle.WaitTimeout ? null : channels[idx];
            }
            else
            {
                return null;
            }
        }

        public static IEnumerable<IChannelReader<T>> Barrier<T>(params IChannelReader<T>[] channels)
        {
            return Barrier<T>(Timeout.Infinite, channels);
        }
        public static IEnumerable<IChannelReader<T>> Barrier<T>(int timeoutMillis, params IChannelReader<T>[] channels)
        {
            using (TimeoutManager timeout = TimeoutManager.Start(timeoutMillis))
            {
                if (channels.Length > 0)
                {
                    foreach (WaitHandle item in channels.Select(p => p.WaitReadable))
                    {
                        if (!item.WaitOne(timeout))
                            return null;
                    }
                }

                return channels;
            }
        }
    }
}
