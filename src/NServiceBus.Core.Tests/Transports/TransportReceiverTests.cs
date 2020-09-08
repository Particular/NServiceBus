﻿namespace NServiceBus.Core.Tests.Transports
{
    using System;
    using System.Threading;
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
        public async Task Start_should_start_the_pump()
        {
            await receiver.Start(CancellationToken.None);

            Assert.IsTrue(pump.Started);
        }

        [Test]
        public void Start_should_rethrow_when_pump_throws()
        {
            pump.ThrowOnStart = true;

            Assert.ThrowsAsync<InvalidOperationException>(async () => await receiver.Start(CancellationToken.None));
        }

        [Test]
        public async Task Stop_should_stop_the_pump()
        {
            await receiver.Start(CancellationToken.None);

            await receiver.Stop(CancellationToken.None);

            Assert.IsTrue(pump.Stopped);
        }

        [Test]
        public async Task Stop_should_not_throw_when_pump_throws()
        {
            pump.ThrowOnStop = true;

            await receiver.Start(CancellationToken.None);

            Assert.DoesNotThrowAsync(async () => await receiver.Stop(CancellationToken.None));
        }

        [Test]
        public async Task Stop_should_dispose_pump() // for container backward compat reasons
        {
            await receiver.Start(CancellationToken.None);

            await receiver.Stop(CancellationToken.None);

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

            public Task Init(Func<MessageContext, CancellationToken, Task> onMessage, Func<ErrorContext, CancellationToken, Task<ErrorHandleResult>> onError, CriticalError criticalError, PushSettings settings, CancellationToken cancellationToken)
            {
                throw new NotImplementedException();
            }

            public void Start(PushRuntimeSettings limitations, CancellationToken cancellationToken)
            {
                if (ThrowOnStart)
                {
                    throw new InvalidOperationException();
                }

                Started = true;
            }

            public Task Stop(CancellationToken cancellationToken)
            {
                if (ThrowOnStop)
                {
                    throw new InvalidOperationException();
                }

                Stopped = true;

                return Task.CompletedTask;
            }

            public void Dispose()
            {
                Disposed = true;
            }
        }
    }
}