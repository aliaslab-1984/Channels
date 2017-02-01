using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Channels.Impl
{
    public static class ChannelWriterSync
    {
        public static IChannelWriter<T> Select<T>(params IChannelWriter<T>[] channels)
        {
            return Select<T>(Timeout.Infinite, channels);
        }
        public static IChannelWriter<T> Select<T>(int timeoutMillis, params IChannelWriter<T>[] channels)
        {
            if (channels.Length > 0)
            {
                int idx = WaitHandle.WaitAny(channels.Select(p => p.WaitWriteable).ToArray(), timeoutMillis);

                return idx==WaitHandle.WaitTimeout?null:channels[idx];
            }
            else
            {
                return null;
            }
        }

        public static IEnumerable<IChannelWriter<T>> Barrier<T>(params IChannelWriter<T>[] channels)
        {
            return Barrier<T>(Timeout.Infinite, channels);
        }

        public static IEnumerable<IChannelWriter<T>> Barrier<T>(int timeoutMillis, params IChannelWriter<T>[] channels)
        {
            using (TimeoutManager timeout = TimeoutManager.Start(timeoutMillis))
            {
                if (channels.Length > 0)
                {
                    foreach (WaitHandle item in channels.Select(p => p.WaitWriteable))
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
