using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Channels.Impl
{
    internal static class ChannelsRepository
    {
        private static Dictionary<object, object> _readers = new Dictionary<object, object>();
        private static Dictionary<object, object> _writers = new Dictionary<object, object>();

        private static Dictionary<string, Tuple<List<object>, List<object>>> _namedChannels = new Dictionary<string, Tuple<List<object>, List<object>>>();

        internal static Queue<T> AddReader<T>(IChannelReader<T> reader, Queue<T> queue)
        {
            if (_readers.ContainsKey(reader))
            {
                if (_readers[reader] != queue)
                    throw new InvalidOperationException("Impossible to associate a different queue to the already created channel end.");
                return (Queue<T>)_readers[reader];
            }
            else
            {
                Queue<T> qu = queue == null ? new Queue<T>() : queue;
                _readers.Add(reader, qu);
                return qu;
            }
        }
        internal static Queue<T> AddWriter<T>(IChannelWriter<T> writer, Queue<T> queue)
        {
            if (_writers.ContainsKey(writer))
            {
                if (_writers[writer] != queue)
                    throw new InvalidOperationException("Impossible to associate a different queue to the already created channel end.");
                return (Queue<T>)_writers[writer];
            }
            else
            {
                Queue<T> qu = queue == null ? new Queue<T>() : queue;
                _writers.Add(writer, qu);
                return qu;
            }
        }
        internal static void RemoveReader<T>(IChannelReader<T> reader)
        {
            _readers.Remove(reader);
        }
        internal static void RemoveWriter<T>(IChannelWriter<T> writer)
        {
            _writers.Remove(writer);
        }
        internal static Queue<T> AddNamedReader<T>(string name, IChannelReader<T> reader, Queue<T> queue)
        {
            queue = AddReader<T>(reader,queue);

            if (!_namedChannels.ContainsKey(name))
                _namedChannels.Add(name, new Tuple<List<object>, List<object>>(null, null));
            if (_namedChannels[name].Item1.Contains(reader))
                throw new Exception("Cannot attach mutiple times the same channel");
            else
                _namedChannels[name].Item1.Add(reader);

            return queue;
        }
        internal static Queue<T> AddNamedWriter<T>(string name, IChannelWriter<T> writer, Queue<T> queue)
        {
            queue = AddWriter<T>(writer, queue);

            if (!_namedChannels.ContainsKey(name))
                _namedChannels.Add(name, new Tuple<List<object>, List<object>>(null, null));
            if (_namedChannels[name].Item2.Contains(writer))
                throw new Exception("Cannot attach mutiple times the same channel");
            else
                _namedChannels[name].Item2.Add(writer);

            return queue;
        }
        internal static void RemoveNamedReader<T>(string name, IChannelReader<T> reader)
        {
            if(_namedChannels.ContainsKey(name))
            {
                RemoveReader<T>(reader);
                _namedChannels[name].Item1.Remove(reader);
                if (_namedChannels[name].Item1.Count == 0 && _namedChannels[name].Item2.Count == 0)
                    _namedChannels.Remove(name);
            }
        }
        internal static void RemoveNamedWriter<T>(string name, IChannelWriter<T> writer)
        {
            if (_namedChannels.ContainsKey(name))
            {
                RemoveWriter<T>(writer);
                _namedChannels[name].Item2.Remove(writer);
                if (_namedChannels[name].Item1.Count == 0 && _namedChannels[name].Item2.Count == 0)
                    _namedChannels.Remove(name);
            }
        }
    }
}
