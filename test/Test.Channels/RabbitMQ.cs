using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Channels.RabbitMQ;
using Channels;
using mq = Channels.RabbitMQ.Impl;
using System.Threading.Tasks;
using System.Threading;
using Channels.Impl;
using System.Linq;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Channels.Exceptions;
using RabbitMQ.Client;
using System.Text;
using Newtonsoft.Json;
using RabbitMQ.Client.Exceptions;
using System.Security.Cryptography;

namespace Test.Channels
{
    [TestClass]
    public class RabbitMQ
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
        #region nested types

        [DataContract]
        public class SerializeObjectsData
        {
            [DataMember]
            public string OpId { get; set; }
            [DataMember]
            public string Message { get; set; }
            [DataMember]
            public string Status { get; set; }
        }

        #endregion

        [ClassInitialize]
        public static void Initialize(TestContext ctx)
        {
            ChannelsRabbitMQManager.Init();
        }

        public virtual int Timeblok { get { return 100; } }

        public string Salt { get { return "-"+Guid.NewGuid().ToString(); } }

        [TestCategory("RabbitMQ")]
        [TestMethod]
        public virtual void SyncDiff()
        {
            int expected = 3;

            INamedChannel<int> c = new mq.Channel<int>("test-sync" + Salt);
            {

                Task.Factory.StartNew(() =>
                {
                    INamedChannel<int> w = new mq.Channel<int>(c.Name);
                    Thread.Sleep(Timeblok);
                    w.Write(expected);
                });

                Assert.AreEqual(expected, c.Read());
            }
        }

        [TestCategory("RabbitMQ")]
        [TestMethod]
        public virtual void Sync()
        {
            int expected = 3;

            using (INamedChannel<int> c = new mq.Channel<int>("test-sync" + Salt))
            {

                Task.Factory.StartNew(() =>
                {
                    Thread.Sleep(Timeblok);
                    c.Write(expected);
                });

                Assert.AreEqual(expected, c.Read());
            }
        }
        [TestCategory("RabbitMQ")]
        [TestMethod]
        public virtual void Sync_Timeout()
        {
            int expected = 3;

            using (INamedChannel<int> c = new mq.Channel<int>("test-sync" + Salt))
            {
                Task.Factory.StartNew(() =>
                {
                    Thread.Sleep(Timeblok * 20);
                    c.Write(expected);
                    Thread.Sleep(Timeblok * 20);
                    c.Write(expected);
                });

                Assert.AreEqual(expected, c.Read(Timeblok * 40));
                try
                {
                    Assert.AreEqual(expected, c.Read(Timeblok / 2));
                    Assert.Fail("Expected exception.");
                }
                catch (Exception e)
                {
                    Assert.IsInstanceOfType(e, typeof(TimeoutException));
                }
            }
        }

        [TestCategory("RabbitMQ")]
        [TestMethod]
        public virtual void Sync_Transactional()
        {
            int expected = 3;

            using (INamedChannel<int> c = new mq.Channel<int>("test-sync" + Salt))
            {
                Task.Factory.StartNew(() =>
                {
                    Thread.Sleep(Timeblok);
                    c.Write(expected);
                });

                try
                {
                    c.Consume(p => { throw new Exception(); }, Timeblok * 20);
                    Assert.Fail();
                }
                catch (OperationCanceledException e)
                { }

                try
                {
                    c.Consume(p => { throw new Exception(); }, Timeblok * 20);
                    Assert.Fail();
                }
                catch (OperationCanceledException e)
                { }

                c.Consume(p => Assert.AreEqual(expected, p), Timeblok * 20);

                try
                {
                    c.Consume(p => Assert.AreEqual(expected, p), Timeblok / 2);
                    Assert.Fail("Expected exception.");
                }
                catch (Exception e)
                {
                    Assert.IsInstanceOfType(e, typeof(TimeoutException));
                }
            }
        }

        [TestCategory("RabbitMQ")]
        [TestMethod]
        public virtual void Sync_Transactional_2()
        {
            int expected = 3;

            using (INamedChannel<int> c = new mq.Channel<int>("test-sync" + Salt))
            {
                Task.Factory.StartNew(() =>
                {
                    Thread.Sleep(Timeblok);
                    c.Write(expected);
                });

                IAbortableOperation<int> res = c.Consume(Timeblok * 20);
                Assert.AreEqual(expected, res.Value);
                res.Abort();
                res = c.Consume(Timeblok * 20);
                Assert.AreEqual(expected, res.Value);
                res.Abort();

                res = c.Consume(Timeblok * 20);
                Assert.AreEqual(expected, res.Value);
                res.Commit();

                try
                {
                    c.Consume(Timeblok / 2);
                    Assert.Fail("Expected exception.");
                }
                catch (Exception e)
                {
                    Assert.IsInstanceOfType(e, typeof(TimeoutException));
                }
            }
        }

        [TestCategory("RabbitMQ")]
        [TestMethod]
        public virtual void OutputAdapter()
        {
            int expected = 3;

            using (IChannel<int> c = new mq.Channel<int>("test-OutputAdapter" + Salt))
            using (IChannelReader<string> ca = new FuncChannelOutputAdapter<int, string>(c, p => p.ToString()))
            {

                Task.Factory.StartNew(() =>
                {
                    Thread.Sleep(Timeblok);
                    c.Write(expected);
                });

                Assert.AreEqual(expected.ToString(), ca.Read());
            }
        }

        [TestCategory("RabbitMQ")]
        [TestMethod]
        public virtual void InputAdapter()
        {
            int expected = 3;

            using (IChannel<string> c = new mq.Channel<string>("test-InputAdapter" + Salt))
            using (IChannelWriter<int> ca = new FuncChannelInputAdapter<int, string>(c, p => p.ToString()))
            {

                Task.Factory.StartNew(() =>
                {
                    Thread.Sleep(Timeblok);
                    ca.Write(expected);
                });

                Assert.AreEqual(expected.ToString(), c.Read());
            }
        }

        [TestCategory("RabbitMQ")]
        [TestMethod]
        public virtual void CompositeChannel()
        {
            int expected = 3;
            using (IChannel<int> c = new mq.Channel<int>("test-CompositeChannel" + Salt))
            using (IChannel<string> x = new CompositeChannel<string>(
                    new FuncChannelInputAdapter<string, int>(c, p => Convert.ToInt32(p)),
                    new FuncChannelOutputAdapter<int, string>(c, p => p.ToString())))
            {

                Task.Factory.StartNew(() =>
                {
                    Thread.Sleep(Timeblok);
                    x.Write(expected.ToString());
                });

                Assert.AreEqual(expected.ToString(), x.Read());
            }
        }

        [TestCategory("RabbitMQ")]
        [TestMethod]
        public virtual void Pipe()
        {
            int expected = 3;

            using (IChannel<int> d = new mq.Channel<int>("test-Pipe2" + Salt))
            using (IChannel<int> c = new FuncChannelPipe<int>(new mq.Channel<int>("test-Pipe1" + Salt), d, p => p))
            {

                Task.Factory.StartNew(() =>
                {
                    Thread.Sleep(Timeblok);
                    c.Write(expected);
                });

                Assert.AreEqual(expected, c.Read());
                c.Close();
            }
        }

        [TestCategory("RabbitMQ")]
        [TestMethod]
        public virtual void SyncWrite()
        {
            int expected = 3;

            using (IChannel<int> c = new mq.Channel<int>("test-SyncWrite1" + Salt))
            using (IChannel<int> w = new mq.Channel<int>("test-SyncWrite2" + Salt))
            {
                Task.Factory.StartNew(() =>
                {
                    Thread.Sleep(Timeblok);
                    c.Read();
                    c.Read();
                    c.Write(expected);
                    w.Write(0);
                });
                c.Write(0);
                c.Write(1);
                w.Read();
                Assert.AreEqual(expected, c.Read());
            }
        }

        [TestCategory("RabbitMQ")]
        [TestMethod]
        public virtual void Acc()
        {
            int a = 1, b = 2;
            int expected = a + b;

            using (IChannel<int> c = new mq.Channel<int>("test-Acc" + Salt))
            {

                Task.Factory.StartNew(() =>
                {
                    Thread.Sleep(Timeblok);
                    c.Write(a);
                });
                Task.Factory.StartNew(() =>
                {
                    Thread.Sleep(Timeblok);
                    c.Write(b);
                });

                Assert.AreEqual(expected, c.Read() + c.Read());
            }
        }

        [TestCategory("RabbitMQ")]
        [TestMethod]
        public virtual void Enumerate()
        {
            int count = 100;
            int[] expected = Enumerable.Range(0, count).ToArray();
            List<int> actual = new List<int>();

            using (IChannel<int> c = new mq.Channel<int>("test-Enumerate1" + Salt))
            using (IChannel<int> w = new mq.Channel<int>("test-Enumerate2" + Salt))
            {
                Task.Factory.StartNew(() =>
                {
                    for (int i = 0; i < count; i++)
                        c.Write(i);

                    c.Close();
                });
                Task.Factory.StartNew(() =>
                {
                    foreach (int item in c.Enumerate())
                    {
                        actual.Add(item);
                    }

                    w.Write(count);
                    w.Close();
                });

                Assert.AreEqual(count, w.Read());
                w.WaitReadable.WaitOne();
                CollectionAssert.AreEquivalent(expected, actual);
            }
        }

        [TestCategory("RabbitMQ")]
        [TestMethod]
        public virtual void SyncSelect()
        {
            int expected = 3;

            using (IChannel<int> c0 = new mq.Channel<int>("test-SyncSelect1" + Salt))
            using (IChannel<int> c1 = new mq.Channel<int>("test-SyncSelect2" + Salt))
            using (IChannel<int> c2 = new mq.Channel<int>("test-SyncSelect3" + Salt))
            {
                Task.Factory.StartNew(() =>
                {
                    Thread.Sleep(Timeblok);
                    c0.Write(expected);
                });
                Task.Factory.StartNew(() =>
                {
                    Thread.Sleep(2* Timeblok);
                    c1.Write(expected);
                });
                Task.Factory.StartNew(() =>
                {
                    Thread.Sleep(3* Timeblok);
                    c2.Write(expected);
                });

                IChannelReader<int> res = c0.SelectWith(c1, c2);
                Assert.AreEqual(c0, res);
                Assert.AreEqual(expected, res.Read());
            }
        }

        [TestCategory("RabbitMQ")]
        [TestMethod]
        public virtual void SyncBarrier()
        {
            int expected = 3;

            using (IChannel<int> c0 = new mq.Channel<int>("test-SyncBarrier1" + Salt))
            using (IChannel<int> c1 = new mq.Channel<int>("test-SyncBarrier2" + Salt))
            using (IChannel<int> c2 = new mq.Channel<int>("test-SyncBarrier3" + Salt))
            {
                Task.Factory.StartNew(() =>
                {
                    Thread.Sleep(Timeblok);
                    c0.Write(expected);
                });
                Task.Factory.StartNew(() =>
                {
                    Thread.Sleep(2* Timeblok);
                    c1.Write(expected);
                });
                Task.Factory.StartNew(() =>
                {
                    Thread.Sleep(3* Timeblok);
                    c2.Write(expected);
                });

                IEnumerable<IChannelReader<int>> res = c0.BarrierWith(c1, c2);
                CollectionAssert.AreEquivalent(new IChannelReader<int>[] { c0, c1, c2 }, res.ToArray());
                foreach (IChannelReader<int> item in res)
                {
                    Assert.AreEqual(expected, item.Read());
                }
            }
        }

        [TestCategory("RabbitMQ")]
        [TestMethod]
        public virtual void ConcurrentReaders()
        {
            int count = 2000;
            int r0 = 0;
            int r1 = 0;

            using (IChannel<int> c = new mq.Channel<int>("test-ConcurrentReaders1" + Salt))
            using (IChannel<int> w0 = new mq.Channel<int>("test-ConcurrentReaders2" + Salt))
            using (IChannel<int> w1 = new mq.Channel<int>("test-ConcurrentReaders3" + Salt))
            {
                Task.Factory.StartNew(() =>
                {
                    foreach (int item in c.Enumerate())
                    {
                        r0++;
                    }
                    w0.Write(r0);
                });
                Task.Factory.StartNew(() =>
                {
                    foreach (int item in c.Enumerate())
                    {
                        r1++;
                    }
                    w1.Write(r1);
                });

                for (int i = 0; i < count; i++)
                {
                    c.Write(i);
                }
                c.Close();

                Assert.AreEqual(count, w0.Read() + w1.Read());
                Assert.IsTrue(r0 > 0);
                Assert.IsTrue(r1 > 0);
                //Assert.AreEqual(0.5, r0 / (double)count, 0.2);
                //Assert.AreEqual(0.5, r1 / (double)count, 0.2);
            }
        }


        //TEST: flaky, ma forse è giusto così +++ Potrebbe essere veramente giusto così
        [TestCategory("RabbitMQ")]
        [TestMethod]
        public virtual void ConcurretSingleQueue()
        {
            string salt = Salt;
            using (IChannel<int> c1 = new mq.Channel<int>("test-ConcurretSingleQueue" + salt))
            using (IChannel<int> c2 = new mq.Channel<int>("test-ConcurretSingleQueue" + salt))
            {
                c2.Write(1);
                c1.Write(2);

                IChannelReader<int> x = c1.SelectWith(c2);

                Assert.AreEqual(1, x.Read());

                IChannelReader<int> y = c1.SelectWith(c2);

                Assert.AreNotEqual(x, y);
                Assert.AreEqual(2, y.Read());
                Console.WriteLine();
            }
        }

        [TestCategory("RabbitMQ")]
        [TestMethod]
        public virtual void SubscribedReaders()
        {
            int count = 10;
            int r0 = 0;
            int r1 = 0;

            using (ISubscribableChannel<int> c = new mq.SubscribableChannel<int>("test-SubscribedReaders1" + Salt))
            using (IChannel<int> w0 = new mq.Channel<int>("test-SubscribedReaders2" + Salt))
            using (IChannel<int> w1 = new mq.Channel<int>("test-SubscribedReaders3" + Salt))
            {
                Task.Factory.StartNew((object rd) =>
                {

                    foreach (int item in ((IChannelReader<int>)rd).Enumerate())
                    {
                        r0++;
                    }
                    w0.Write(r0);
                }, c.Subscribe());

                Task.Factory.StartNew((object rd) =>
                {
                    foreach (int item in ((IChannelReader<int>)rd).Enumerate())
                    {
                        r1++;
                    }
                    w1.Write(r1);
                }, c.Subscribe());

                for (int i = 0; i < count; i++)
                {
                    c.Write(i);
                }
                c.Close();

                Assert.AreEqual(2 * count, w0.Read() + w1.Read());
                Assert.AreEqual(count, r0);
                Assert.AreEqual(count, r1);
            }
        }
        
        [TestCategory("RabbitMQ")]
        [TestMethod]
        public virtual void SerializeObjects()
        {
            string name = "test-SerializeObjects" + Salt;
            Queue<string> rq = new Queue<string>(new string[] { "aaa", "aab", "abb", "bbb", "bbc", "bcc", "ccc" });
            Queue<string> wq = new Queue<string>(new string[] { "aaa", "aab", "abb", "bbb", "bbc", "bcc", "ccc" });

            AbstractChannelsFactory fc = new RabbitMQChannelsFactory();

            Task tr=Task.Factory.StartNew(()=> {
                using (IChannelReader<SerializeObjectsData> ch = fc.GetSubscribableChannel<SerializeObjectsData>(name).Subscribe())
                {
                    foreach (SerializeObjectsData item in ch.Enumerate())
                    {
                        Assert.AreEqual(item.OpId, rq.Dequeue());
                    }
                    Assert.IsTrue(ch.Drained);
                }
            });

            Task tw=Task.Factory.StartNew(() => {
                using (IChannelWriter<SerializeObjectsData> ch = fc.GetSubscribableChannel<SerializeObjectsData>(name))
                {
                    while (wq.Count > 0)
                        ch.Write(new SerializeObjectsData() { OpId= wq.Dequeue(), Status="test", Message="/messaggio/lungo/con/sbarre" });
                    ch.Close();
                }
            });

            Task.WaitAll(tr, tw);
        }

        [TestCategory("RabbitMQ")]
        [TestMethod]
        public virtual void ActiveEnumeratorTest()
        {
            int count = 100;
            int[] expected = Enumerable.Range(0, count).ToArray();
            List<string> actual = new List<string>();

            using (IChannel<int> c = new mq.Channel<int>("test-ActiveEnumeratorTest1" + Salt))
            using (IChannel<string> w = new mq.Channel<string>("test-ActiveEnumeratorTest2" + Salt))
            {
                w.ActiveEnumerate(p => actual.Add(p)).StoppedEvent += e => {
                    Assert.IsInstanceOfType(e, typeof(ChannelDrainedException));
                    c.Write(0);
                };

                Task.Factory.StartNew(() =>
                {
                    for (int i = 0; i < count; i++)
                        w.Write(i.ToString());

                    w.Close();
                });

                c.Read();

                Assert.AreEqual(count, actual.Count);
                CollectionAssert.AreEquivalent(expected.Select(p => p.ToString()).ToArray(), actual);
            }
        }

        [TestCategory("RabbitMQ")]
        [TestMethod]
        public virtual void ShovelTest()
        {
            int count = 100;
            int[] expected = Enumerable.Range(0, count).ToArray();
            List<string> actual = new List<string>();

            using (IChannel<int> c = new mq.Channel<int>("test-ShovelTest1" + Salt))
            using (IChannel<string> w = new mq.Channel<string>("test-ShovelTest2" + Salt))
            {
                Task.Factory.StartNew(() =>
                {
                    for (int i = 0; i < count; i++)
                        c.Write(i);

                    c.Close();
                });

                AbstractShovelThread<int, string> th = c.ShovelTo<int, string>(w, p => p.ToString(), TimeSpan.FromSeconds(10), true);
                th.StoppedEvent += e => Assert.IsInstanceOfType(e, typeof(ChannelDrainedException));

                foreach (string item in w.Enumerate())
                {
                    actual.Add(item);
                }

                Assert.AreEqual(count, actual.Count);
                CollectionAssert.AreEquivalent(expected.Select(p => p.ToString()).ToArray(), actual);
            }
        }

        [TestCategory("RabbitMQ")]
        [TestMethod]
        public virtual void DifferentTypesTest()
        {
            int count = 100;
            int[] expected = Enumerable.Range(0, count).ToArray();
            List<int> actualInt = new List<int>();
            string testString = "test";
            string queueName = "test-DifferentTypesTest" + Salt;

            using (ISubscribableChannel<int> c = new mq.SubscribableChannel<int>(queueName))
            using (ISubscribableChannel<string> w = new mq.SubscribableChannel<string>(queueName))
            {
                IChannelReader<string> t = w.Subscribe();
                IChannelReader<int> x= c.Subscribe();
                Task.Factory.StartNew(() =>
                {
                    for (int i = 0; i < count; i++)
                    {
                        if (i == 2)
                            w.Write(testString);
                        c.Write(i);
                    }

                    c.Close();
                });

                string val = t.Read();
                foreach (int item in x.Enumerate())
                {
                    actualInt.Add(item);
                }
                w.Write(testString + "1");
                val = t.Read();

                Assert.AreEqual(val, testString + "1");
                Assert.IsFalse(t.Drained);
                Assert.IsTrue(x.Drained);
            }
        }

        [TestCategory("RabbitMQ")]
        [TestMethod]
        public virtual void UntypedTypesTest()
        {
            int count = 100;
            int[] expected = Enumerable.Range(0, count).ToArray();
            List<int> actualInt = new List<int>();
            string testString = "test";
            string queueName = "test-UntypedTypesTest" + Salt;
            
            using (ISubscribableChannel<string> w = new mq.SubscribableChannel<string>(queueName))
            {
                IChannelReader<string> t = w.Subscribe();
                
                #region bare rabbitmq
                ConnectionFactory fc = new ConnectionFactory() { HostName = "code.test.aliaslab.net", Port = 5672, VirtualHost = "channels-lib", UserName = "admin", Password = "admin" };
                IConnection connection = fc.CreateConnection();
                IModel _channel = connection.CreateModel();

                _channel.BasicQos(0, 1, true);

                _channel.ExchangeDeclare(queueName, ExchangeType.Fanout, true, true, null);
                IBasicProperties prop = _channel.CreateBasicProperties();
                prop.ContentEncoding = "utf-8";
                prop.ContentType = "application/json";
                prop.Persistent = true;
                prop.Headers = new Dictionary<string, object>();

                byte[] data = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(new { name = "piero", age = 13 }));
                _channel.BasicPublish(queueName, "test", prop, data);

                #endregion

                w.Write(testString);

                Assert.AreEqual(testString, t.Read());
            }
        }

        [TestCategory("RabbitMQ")]
        [TestMethod]
        public virtual void UntypedTypesTest2()
        {
            int count = 100;
            int[] expected = Enumerable.Range(0, count).ToArray();
            List<int> actualInt = new List<int>();
            Dictionary<string, string> testString = new Dictionary<string, string>() { { "val", "test" } };
            string queueName = "test-UntypedTypesTest2" + Salt;

            using (ISubscribableChannel<Dictionary<string, string>> w = new mq.SubscribableChannel<Dictionary<string, string>>(queueName))
            {
                IChannelReader<Dictionary<string, string>> t = w.Subscribe();

                #region bare rabbitmq
                ConnectionFactory fc = new ConnectionFactory() { HostName = "code.test.aliaslab.net", Port = 5672, VirtualHost = "channels-lib", UserName = "admin", Password = "admin" };
                IConnection connection = fc.CreateConnection();
                IModel _channel = connection.CreateModel();

                _channel.BasicQos(0, 1, true);

                _channel.ExchangeDeclare(queueName, ExchangeType.Fanout, true, true, null);
                IBasicProperties prop = _channel.CreateBasicProperties();
                prop.ContentEncoding = "utf-8";
                prop.ContentType = "application/json";
                prop.Persistent = true;
                prop.Headers = new Dictionary<string, object>();

                byte[] data = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(new { name = "piero", age = 13 }));
                _channel.BasicPublish(queueName, "test", prop, data);

                _channel.BasicPublish("channels-subscribableChannelCollectorExchange", $"{queueName}", prop, data);

                #endregion

                w.Write(testString);

                Dictionary<string, string> result = t.Read();
                Assert.IsTrue(result.ContainsKey("name"));
                Assert.AreEqual("piero", result["name"]);
            }
        }

        [TestCategory("RabbitMQ")]
        [TestMethod]
        public virtual void CloseTest()
        {
            int count = 100;
            int[] expected = Enumerable.Range(0, count).ToArray();
            List<int> actualInt = new List<int>();
            Dictionary<string, string> testString = new Dictionary<string, string>() { { "val", "test" } };
            string queueName = "test-CloseTest" + Salt;

            using (ISubscribableChannel<Dictionary<string, string>> w = new mq.SubscribableChannel<Dictionary<string, string>>(queueName))
            using (IChannel<Dictionary<string, string>> c =new mq.Channel<Dictionary<string,string>>(queueName))
            {
                IChannelReader<Dictionary<string, string>> t = w.Subscribe();

                c.Close();

                w.Write(testString);

                Dictionary<string, string> result = t.Read();
                Assert.IsTrue(result.ContainsKey("val"));
                Assert.AreEqual(testString["val"], result["val"]);
            }
        }
    }
}
