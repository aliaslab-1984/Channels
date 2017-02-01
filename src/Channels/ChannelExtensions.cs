using Channels.Exceptions;
using Channels.Impl;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Channels
{
    public static class ChannelExtensions
    {
        public static IEnumerable<T> Enumerate<T>(this IChannelReader<T> ext)
        {
            return new ChannelReaderEnumerable<T>(ext);
        }
        public static AbstractActiveEnumerator<T> ActiveEnumerate<T>(this IChannelReader<T> ext,Action<T> handle)
        {
            return ActiveEnumerate(ext, handle, TimeSpan.MaxValue);
        }
        public static AbstractActiveEnumerator<T> ActiveEnumerate<T>(this IChannelReader<T> ext, Action<T> handle, TimeSpan lifeTime)
        {
            AbstractActiveEnumerator<T> th = new FuncActiveEnumerator<T>(handle, ext, lifeTime);
            th.Start();
            return th;
        }

        public static IChannelWriter<S> AdaptedInput<S,D>(this IChannelWriter<D> ext, Func<S,D> transform)
        {
            return new FuncChannelInputAdapter<S, D>(ext, transform);
        }
        public static IChannelReader<D> AdaptedOutput<S, D>(this IChannelReader<S> ext, Func<S, D> transform)
        {
            return new FuncChannelOutputAdapter<S, D>(ext, transform);
        }

        public static IChannel<T> PipeTo<T>(this IChannel<T> ext, IChannel<T> destination, Func<T,T> pipe)
        {
            return new FuncChannelPipe<T>(ext, destination, pipe);
        }
        public static IChannel<T> PipeFrom<T>(this IChannel<T> ext, IChannel<T> source, Func<T, T> pipe)
        {
            return new FuncChannelPipe<T>(source, ext, pipe);
        }

        public static IChannel<T> Compose<T>(this IChannelReader<T> ext, IChannelWriter<T> source)
        {
            return new CompositeChannel<T>(source, ext);
        }
        public static IChannel<T> Compose<T>(this IChannelWriter<T> ext, IChannelReader<T> destination)
        {
            return new CompositeChannel<T>(ext, destination);
        }

        public static AbstractShovelThread<S, D> ShovelTo<S,D>(this IChannelReader<S> ext, IChannelWriter<D> destination, Func<S,D> transform, TimeSpan lifeTime, bool closeOnDrain)
        {
            AbstractShovelThread<S, D> th = new FuncShovelThread<S, D>(transform, ext, destination, lifeTime, closeOnDrain);
            th.Start();
            return th;
        }
        public static AbstractShovelThread<S, D> ShovelFrom<S, D>(this IChannelWriter<D> ext, IChannelReader<S> source, Func<S, D> transform, TimeSpan lifeTime, bool closeOnDrain)
        {
            AbstractShovelThread<S, D> th = new FuncShovelThread<S, D>(transform, source, ext, lifeTime, closeOnDrain);
            th.Start();
            return th;
        }

        public static IChannelReader<T> SelectWith<T>(this IChannelReader<T> ext,params IChannelReader<T>[] channels)
        {
            List<IChannelReader<T>> tmp = new List<IChannelReader<T>>(channels);
            tmp.Add(ext);
            return ChannelReaderSync.Select<T>(tmp.ToArray());
        }
        public static IChannelReader<T> SelectWith<T>(this IChannelReader<T> ext, int timeoutMillis, params IChannelReader<T>[] channels)
        {
            List<IChannelReader<T>> tmp = new List<IChannelReader<T>>(channels);
            tmp.Add(ext);
            return ChannelReaderSync.Select<T>(timeoutMillis, tmp.ToArray());
        }
        public static IChannelReader<T> SelectWith<T>(this IEnumerable<IChannelReader<T>> ext)
        {
            return ChannelReaderSync.Select<T>(ext.ToArray());
        }
        public static IChannelReader<T> SelectWith<T>(this IEnumerable<IChannelReader<T>> ext, int timeoutMillis)
        {
            return ChannelReaderSync.Select<T>(timeoutMillis, ext.ToArray());
        }
        public static IEnumerable<IChannelReader<T>> BarrierWith<T>(this IChannelReader<T> ext, params IChannelReader<T>[] channels)
        {
            List<IChannelReader<T>> tmp = new List<IChannelReader<T>>(channels);
            tmp.Add(ext);
            return ChannelReaderSync.Barrier<T>(tmp.ToArray());
        }
        public static IEnumerable<IChannelReader<T>> BarrierWith<T>(this IChannelReader<T> ext, int timeoutMillis, params IChannelReader<T>[] channels)
        {
            List<IChannelReader<T>> tmp = new List<IChannelReader<T>>(channels);
            tmp.Add(ext);
            return ChannelReaderSync.Barrier<T>(timeoutMillis, tmp.ToArray());
        }
        public static IEnumerable<IChannelReader<T>> BarrierWith<T>(this IEnumerable<IChannelReader<T>> ext)
        {
            return ChannelReaderSync.Barrier<T>(ext.ToArray());
        }
        public static IEnumerable<IChannelReader<T>> BarrierWith<T>(this IEnumerable<IChannelReader<T>> ext, int timeoutMillis)
        {
            return ChannelReaderSync.Barrier<T>(timeoutMillis, ext.ToArray());
        }
        public static IChannelWriter<T> WriteSelectWith<T>(this IChannelWriter<T> ext, params IChannelWriter<T>[] channels)
        {
            List<IChannelWriter<T>> tmp = new List<IChannelWriter<T>>(channels);
            tmp.Add(ext);
            return ChannelWriterSync.Select<T>(tmp.ToArray());
        }
        public static IChannelWriter<T> WriteSelectWith<T>(this IChannelWriter<T> ext, int timeoutMillis, params IChannelWriter<T>[] channels)
        {
            List<IChannelWriter<T>> tmp = new List<IChannelWriter<T>>(channels);
            tmp.Add(ext);
            return ChannelWriterSync.Select<T>(timeoutMillis, tmp.ToArray());
        }
        public static IChannelWriter<T> WriteSelectWith<T>(this IEnumerable<IChannelWriter<T>> ext)
        {
            return ChannelWriterSync.Select<T>(ext.ToArray());
        }
        public static IChannelWriter<T> WriteSelectWith<T>(this IEnumerable<IChannelWriter<T>> ext, int timeoutMillis)
        {
            return ChannelWriterSync.Select<T>(timeoutMillis, ext.ToArray());
        }
        public static IEnumerable<IChannelWriter<T>> WriteBarrierWith<T>(this IChannelWriter<T> ext, params IChannelWriter<T>[] channels)
        {
            List<IChannelWriter<T>> tmp = new List<IChannelWriter<T>>(channels);
            tmp.Add(ext);
            return ChannelWriterSync.Barrier<T>(tmp.ToArray());
        }
        public static IEnumerable<IChannelWriter<T>> WriteBarrierWith<T>(this IChannelWriter<T> ext, int timeoutMillis, params IChannelWriter<T>[] channels)
        {
            List<IChannelWriter<T>> tmp = new List<IChannelWriter<T>>(channels);
            tmp.Add(ext);
            return ChannelWriterSync.Barrier<T>(timeoutMillis, tmp.ToArray());
        }
        public static IEnumerable<IChannelWriter<T>> WriteBarrierWith<T>(this IEnumerable<IChannelWriter<T>> ext)
        {
            return ChannelWriterSync.Barrier<T>(ext.ToArray());
        }
        public static IEnumerable<IChannelWriter<T>> WriteBarrierWith<T>(this IEnumerable<IChannelWriter<T>> ext, int timeoutMillis)
        {
            return ChannelWriterSync.Barrier<T>(timeoutMillis, ext.ToArray());
        }
    }
}
