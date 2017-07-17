﻿namespace NServiceBus.Core.Tests.Transports
{
    using System;
    using System.Threading.Tasks;
    using NUnit.Framework;
    using Transport;

    [TestFixture]
    public class TransportReceiverTests
    {
        [SetUp]
        public void SetUp()
        {
            pump = new Pump();

            receiver = new TransportReceiver("FakeReceiver", pump, new PushSettings("queue", "queue", true, TransportTransactionMode.SendsAtomicWithReceive), new PushRuntimeSettings(), null, null, null);
        }

        [Test]
        public void Start_should_start_the_pump()
        {
            receiver.Start();

            Assert.IsTrue(pump.Started);
        }

        [Test]
        public void Start_should_rethrow_when_pump_throws()
        {
            pump.ThrowOnStart = true;

            Assert.Throws<InvalidOperationException>(() => receiver.Start());
        }

        [Test]
        public async Task Stop_should_stop_the_pump()
        {
            receiver.Start();

            await receiver.Stop();

            Assert.IsTrue(pump.Stopped);
        }

        [Test]
        public void Stop_should_not_throw_when_pump_throws()
        {
            pump.ThrowOnStop = true;

            receiver.Start();

            Assert.DoesNotThrowAsync(async () => await receiver.Stop());
        }

        [Test]
        public async Task Stop_should_dispose_pump() // for autofac backward compat reasons
        {
            receiver.Start();

            await receiver.Stop();
            
            Assert.True(pump.Disposed);
        }

        Pump pump;
        TransportReceiver receiver;

        class Pump : IPushMessages, IDisposable
        {
            public bool ThrowOnStart { private get; set; }
            public bool ThrowOnStop { private get; set; }

            public bool Started { get; private set; }
            public bool Stopped { get; private set; }
            public bool Disposed { get; private set; }

            public Task Init(Func<MessageContext, Task> onMessage, Func<ErrorContext, Task<ErrorHandleResult>> onError, CriticalError criticalError, PushSettings settings)
            {
                throw new NotImplementedException();
            }

            public void Start(PushRuntimeSettings limitations)
            {
                if (ThrowOnStart)
                {
                    throw new InvalidOperationException();
                }

                Started = true;
            }

            public Task Stop()
            {
                if (ThrowOnStop)
                {
                    throw new InvalidOperationException();
                }

                Stopped = true;

                return TaskEx.CompletedTask;
            }

            public void Dispose()
            {
                Disposed = true;
            }
        }
    }
}