# Channels
Framework to enable [CSP](https://en.wikipedia.org/wiki/Communicating_sequential_processes) (Communicating sequential processes) for process synchronization.
It is based on the concept of _channel_ and provides an internode queue based implementation built on [RabbitMQ](https://www.rabbitmq.com).

## Description

A channel is composed by a read end and a write end, represented by the interfaces **IChannelReader** and **IChannelWriter**.

A channel is a synchronized queue of a given type _T_ and is possible to use _adapeters_ to create multitype channels.

Every read operation is unique, to multicast a single datum is necessary to use an _ISubscribableChannel_ implementation.

On the channels is possible to perform synchronization oepration like: race (select) and barrier.

Here are some examples taken from the unittests:
<pre>
    [TestCategory("Channels")]
    [TestMethod]
    public virtual void Sync()
    {
        int expected = 3;

        using (IChannel<int> c = new Channel<int>())
        {
            Task.Factory.StartNew(() =>
            {
                Thread.Sleep(Timeblok);
                c.Write(expected);
            });

            Assert.AreEqual(expected, c.Read());
        }
    }

    [TestCategory("Channels")]
    [TestMethod]
    public virtual void OutputAdapter()
    {
        int expected = 3;

        using (IChannel<int> c = new Channel<int>())
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

    [TestCategory("Channels")]
    [TestMethod]
    public virtual void InputAdapter()
    {
        int expected = 3;

        using (IChannel<string> c = new Channel<string>())
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

    [TestCategory("Channels")]
    [TestMethod]
    public virtual void CompositeChannel()
    {
        int expected = 3;
        using (IChannel<int> c = new Channel<int>())
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

    [TestCategory("Channels")]
    [TestMethod]
    public virtual void Pipe()
    {
        int expected = 3;

        using (IChannel<int> c = new FuncChannelPipe<int>(new Channel<int>(), new Channel<int>(), p => p))
        {
            Task.Factory.StartNew(() =>
            {
                Thread.Sleep(Timeblok);
                c.Write(expected);
            });

            Assert.AreEqual(expected, c.Read());
        }
    }

    [TestCategory("Channels")]
    [TestMethod]
    public virtual void SyncWrite()
    {
        int expected = 3;

        using (IChannel<int> c = new Channel<int>())
        using (IChannel<int> w = new Channel<int>())
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

    [TestCategory("Channels")]
    [TestMethod]
    public virtual void Acc()
    {
        int a = 1, b = 2;
        int expected = a+b;

        using (IChannel<int> c = new Channel<int>())
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

    [TestCategory("Channels")]
    [TestMethod]
    public virtual void Enumerate()
    {
        int count = 100;
        int[] expected = Enumerable.Range(0,count).ToArray();
        List<int> actual = new List<int>();

        using (IChannel<int> c = new Channel<int>(5))
        using (IChannel<int> w = new Channel<int>())
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
            });

            Assert.AreEqual(count, w.Read());
            CollectionAssert.AreEquivalent(expected, actual);
        }
    }

    [TestCategory("Channels")]
    [TestMethod]
    public virtual void SyncSelect()
    {
        int expected = 3;

        using (IChannel<int> c0 = new Channel<int>())
        using (IChannel<int> c1 = new Channel<int>())
        using (IChannel<int> c2 = new Channel<int>())
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

    [TestCategory("Channels")]
    [TestMethod]
    public virtual void SyncBarrier()
    {
        int expected = 3;

        using (IChannel<int> c0 = new Channel<int>())
        using (IChannel<int> c1 = new Channel<int>())
        using (IChannel<int> c2 = new Channel<int>())
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

    //TEST: flaky
    [TestCategory("Channels")]
    [TestMethod]
    public virtual void ConcurrentReaders()
    {
        int count = 2000;
        int r0 = 0;
        int r1 = 0;

        using (IChannel<int> c = new Channel<int>())
        using (IChannel<int> w0 = new Channel<int>())
        using (IChannel<int> w1 = new Channel<int>())
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

    [TestCategory("Channels")]
    [TestMethod]
    public virtual void SubscribedReaders()
    {
        int count = 10;
        int r0 = 0;
        int r1 = 0;

        using (ISubscribableChannel<int> c = new SubscribableChannel<int>())
        using (IChannel<int> w0 = new Channel<int>())
        using (IChannel<int> w1 = new Channel<int>())
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
</pre>