﻿using Channels.Exceptions;
using log4net;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Channels.RabbitMQ.Impl
{
    public class SubscribableChannel<T> : ISubscribableNamedChannel<T>
    {
        #region helper methods
        private string SHA1HashStringForUTF8String(string s)
        {
            using (var sha1 = SHA1.Create())
            {
                byte[] hashBytes = sha1.ComputeHash(Encoding.UTF8.GetBytes(s));

                var sb = new StringBuilder();
                foreach (byte b in hashBytes)
                {
                    var hex = b.ToString("x2");
                    sb.Append(hex);
                }
                return sb.ToString();
            }
        }
        #endregion

        private static ILog _logger = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        #region nested types
        internal class SubscriptionReader<T> : INamedChannelReader<T>
        {
            #region helper methods
            private string SHA1HashStringForUTF8String(string s)
            {
                using (var sha1 = SHA1.Create())
                {
                    byte[] hashBytes = sha1.ComputeHash(Encoding.UTF8.GetBytes(s));

                    var sb = new StringBuilder();
                    foreach (byte b in hashBytes)
                    {
                        var hex = b.ToString("x2");
                        sb.Append(hex);
                    }
                    return sb.ToString();
                }
            }
            #endregion

            protected IModel _channel;
            protected EventingBasicConsumer _consumer;
            protected BasicDeliverEventArgs _queue = null;
            
            protected bool _drained = false;

            protected bool _disposed = false;

            protected ManualResetEvent _waitReadable;
            public SubscriptionReader(string topicName, string queueName)
            {
                Name = queueName;
                _channel = ChannelsRabbitMQManager.Ask().Connection.CreateModel();

                _channel.BasicQos(0, 1, true);

                _channel.ExchangeDeclare(ChannelsRabbitMQManager.Ask().SubscribableChannelCollectorExchange, ExchangeType.Topic, true, false, null);
                _waitReadable = new ManualResetEvent(false);

                _channel.QueueDeclare(queueName, true, false, false, null);
                _channel.QueueBind(queueName, ChannelsRabbitMQManager.Ask().SubscribableChannelCollectorExchange, $"{topicName}.#");

                _consumer = new EventingBasicConsumer(_channel);
                _consumer.Received += _consumer_Received;

                _channel.BasicConsume(Name, false, _consumer);
            }
            private void _consumer_Received(object sender, BasicDeliverEventArgs result)
            {
                lock (_channel)
                {
                    if (result.BasicProperties.Headers.ContainsKey(ChannelsRabbitMQManager.Ask().HeadersNameWriterMessageType)
                            && Encoding.UTF8.GetString((byte[])result.BasicProperties.Headers[ChannelsRabbitMQManager.Ask().HeadersNameWriterMessageType])==typeof(T).AssemblyQualifiedName)
                    {
                        if (result.BasicProperties.Headers.ContainsKey(ChannelsRabbitMQManager.Ask().HeadersNameWriterClosed)
                            && (bool)result.BasicProperties.Headers[ChannelsRabbitMQManager.Ask().HeadersNameWriterClosed])
                        {
                            _drained = true;
                            _channel.BasicAck(result.DeliveryTag, false);
                        }
                        else
                        {
                            _queue = result;
                        }

                        _waitReadable.Set();
                        Monitor.PulseAll(_channel);
                    }
                    else if(!result.BasicProperties.Headers.ContainsKey(ChannelsRabbitMQManager.Ask().HeadersNameWriterMessageType))
                    {
                        _logger.Debug("No type header found in message. Trying compatibility.");
                        try
                        {
                            JsonConvert.DeserializeObject<T>(Encoding.UTF8.GetString(result.Body));

                            if (result.BasicProperties.Headers.ContainsKey(ChannelsRabbitMQManager.Ask().HeadersNameWriterClosed)
                            && (bool)result.BasicProperties.Headers[ChannelsRabbitMQManager.Ask().HeadersNameWriterClosed])
                            {
                                _drained = true;
                                _channel.BasicAck(result.DeliveryTag, false);
                            }
                            else
                            {
                                _queue = result;
                            }

                            _waitReadable.Set();
                            Monitor.PulseAll(_channel);
                        }
                        catch(Exception e)
                        {
                            _logger.Debug("Impossible to convert untyped message for readability.Requeuing.", e);
                            _channel.BasicNack(result.DeliveryTag, false, false);
                        }
                    }
                    else
                    {
                        _channel.BasicNack(result.DeliveryTag, false, false);
                    }
                }
            }

            public bool Drained
            {
                get
                {
                    return _drained;
                }
            }

            public string Name
            {
                get;
                protected set;
            }

            public WaitHandle WaitReadable
            {
                get
                {
                    return _waitReadable;
                }
            }

            public void Dispose()
            {
                lock (_channel)
                {
                    if (!_disposed)
                    {
                        _channel.BasicCancel(_consumer.ConsumerTag);
                        
                        if (Drained)
                        {
                            uint purgedCount = _channel.QueueDelete(Name);
                            _logger.DebugFormat("Purged messages queue={0}, count={1}", Name, purgedCount);
                        }

                        _channel.Close(200, "Goodbye");
                        _channel.Dispose();

                        _queue = null;

                        _waitReadable.Dispose();

                        _disposed = true;
                    }
                }
            }

            public T Read()
            {
                return Read(Timeout.Infinite);
            }

            public T Read(int timeoutMillis)
            {
                IAbortableOperation<T> temp = Consume(timeoutMillis);

                try
                {
                    return temp.Value;
                }
                finally
                {
                    temp.Commit();
                }
            }

            public void Consume(Action<T> action)
            {
                Consume(action, Timeout.Infinite);
            }

            public void Consume(Action<T> action, int timeoutMillis)
            {
                IAbortableOperation<T> temp = Consume(timeoutMillis);
                try
                {
                    action(temp.Value);
                }
                catch (Exception e)
                {
                    temp.Abort();
                    throw new OperationCanceledException("Consume cancelled by action exception.", e);
                }
                temp.Commit();
            }
            public IAbortableOperation<T> Consume()
            {
                return Consume(Timeout.Infinite);
            }

            public IAbortableOperation<T> Consume(int timeoutMillis)
            {
                try
                {
                    using (TimeoutManager timeout = TimeoutManager.Start(timeoutMillis))
                    {
                        if (!Monitor.TryEnter(_channel, timeout))
                            throw new TimeoutException("Timeout locking queue.");

                        while (_queue == null)
                        {
                            if (Drained)
                                throw new ChannelDrainedException("Impossible to read other data, channel drained.");
                            _waitReadable.Reset();
                            if (!Monitor.Wait(_channel, timeout))
                                throw new TimeoutException("Timeout waiting readable state.");
                        }

                        T temp = JsonConvert.DeserializeObject<T>(Encoding.UTF8.GetString(_queue.Body));

                        return new AbortableOperationImpl<T>(temp,
                            () =>
                            {
                                _channel.BasicAck(_queue.DeliveryTag, false);
                                _queue = null;
                                _waitReadable.Reset();
                                Monitor.Exit(_channel);
                            },
                            () =>
                            {
                                _channel.BasicNack(_queue.DeliveryTag, false, true);
                                _queue = null;
                                _waitReadable.Reset();
                                Monitor.Exit(_channel);
                            });
                    }
                }
                catch (Exception e)
                {
                    if (Monitor.IsEntered(_channel))
                        Monitor.Exit(_channel);
                    throw e;
                }
            }
        }
        #endregion

        protected bool _closed = false;

        protected bool _disposed = false;
        protected IModel _channel;
        protected ManualResetEvent _waitWriteable;

        public SubscribableChannel(string name)
        {
            _waitWriteable = new ManualResetEvent(true);

            Name = name;
            _channel = ChannelsRabbitMQManager.Ask().Connection.CreateModel();

            ChannelsRabbitMQManager.Ask().Connection.ConnectionBlocked += Connection_ConnectionBlocked;
            ChannelsRabbitMQManager.Ask().Connection.ConnectionUnblocked += Connection_ConnectionUnblocked;

            _channel.ConfirmSelect();

            _channel.BasicQos(0,1,true);

            _channel.ExchangeDeclare(ChannelsRabbitMQManager.Ask().SubscribableChannelCollectorExchange, ExchangeType.Topic,true, false,null);
        }

        private void Connection_ConnectionUnblocked(object sender, EventArgs e)
        {
            _waitWriteable.Set();
        }

        private void Connection_ConnectionBlocked(object sender, ConnectionBlockedEventArgs e)
        {
            _waitWriteable.Reset();
        }

        public bool Closed
        {
            get
            {
                return _closed;
            }
        }

        public string Name
        {
            get;
            protected set;
        }

        public WaitHandle WaitWriteable
        {
            get
            {
                return _waitWriteable;
            }
        }

        public void Close()
        {
            lock (_channel)
            {
                _closed = true;

                IBasicProperties prop = _channel.CreateBasicProperties();
                prop.ContentEncoding = "utf-8";
                prop.ContentType = "application/json";
                prop.Persistent = true;
                prop.Headers = new Dictionary<string, object>();
                prop.Headers.Add(ChannelsRabbitMQManager.Ask().HeadersNameWriterClosed, true);
                prop.Headers.Add(ChannelsRabbitMQManager.Ask().HeadersNameWriterMessageType, typeof(T).AssemblyQualifiedName);

                byte[] data = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(default(T)));
                _channel.BasicPublish(ChannelsRabbitMQManager.Ask().SubscribableChannelCollectorExchange, $"{Name}.{SHA1HashStringForUTF8String(typeof(T).FullName)}", prop, data);
            }
        }

        public void Dispose()
        {
            lock (_channel)
            {
                if (!_disposed)
                {
                    try
                    {
                        _channel.WaitForConfirmsOrDie(TimeSpan.FromMilliseconds(10000));
                    }
                    catch (Exception e)
                    {
                        _logger.Info(string.Format("Unable to wait borker publish confirms exchange={0}", Name), e);
                    }

                    ChannelsRabbitMQManager.Ask().Connection.ConnectionBlocked -= Connection_ConnectionBlocked;
                    ChannelsRabbitMQManager.Ask().Connection.ConnectionUnblocked -= Connection_ConnectionUnblocked;

                    _channel.Close(200, "Goodbye");
                    _channel.Dispose();

                    _disposed = true;
                }
            }
        }

        public IChannelReader<T> Subscribe()
        {
            return Subscribe(Guid.NewGuid().ToString());
        }

        public INamedChannelReader<T> Subscribe(string name)
        {
            lock(_channel)
            {
                if (_closed)
                    throw new ChannelClosedException("Write end closed. Impossible to subscribe.");

                INamedChannelReader<T> ch = new SubscriptionReader<T>(Name, name);

                return ch;
            }
        }

        public void Write(T value)
        {
            Write(value, Timeout.Infinite);
        }

        public void Write(T value, int timeoutMillis)
        {
            using (TimeoutManager timeout = TimeoutManager.Start(timeoutMillis))
            {
                try
                {
                    if (!Monitor.TryEnter(_channel, timeout))
                        throw new TimeoutException("Timeout locking queue.");

                    if (Closed)
                        throw new ChannelClosedException("Impossible to send on a colsed channel.");

                    IBasicProperties prop = _channel.CreateBasicProperties();
                    prop.ContentEncoding = "utf-8";
                    prop.ContentType = "application/json";
                    prop.Persistent = true;
                    prop.Headers = new Dictionary<string, object>();
                    prop.Headers.Add(ChannelsRabbitMQManager.Ask().HeadersNameWriterClosed, false);
                    prop.Headers.Add(ChannelsRabbitMQManager.Ask().HeadersNameWriterMessageType, typeof(T).AssemblyQualifiedName);

                    byte[] data = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(value));
                    _channel.BasicPublish(ChannelsRabbitMQManager.Ask().SubscribableChannelCollectorExchange, $"{Name}.{SHA1HashStringForUTF8String(typeof(T).FullName)}", prop, data);
                }
                finally
                {
                    Monitor.Exit(_channel);
                }
            }
        }
    }
}
