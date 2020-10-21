namespace NServiceBus.Core.Tests.Transports
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

            receiver = new TransportReceiver(pump, new PushRuntimeSettings());
        }

        [Test]
        public async Task Start_should_start_the_pump()
        {
            await receiver.Start(context => Task.CompletedTask, context => Task.FromResult(ErrorHandleResult.Handled));

            Assert.IsTrue(pump.Started);
        }

        [Test]
        public void Start_should_rethrow_when_pump_throws()
        {
            pump.ThrowOnStart = true;

            Assert.ThrowsAsync<InvalidOperationException>(async () => await receiver.Start(context => Task.CompletedTask, context => Task.FromResult(ErrorHandleResult.Handled)));
        }

        [Test]
        public async Task Stop_should_stop_the_pump()
        {
            await receiver.Start(context => Task.CompletedTask, context => Task.FromResult(ErrorHandleResult.Handled));

            await receiver.Stop();

            Assert.IsTrue(pump.Stopped);
        }

        [Test]
        public async Task Stop_should_not_throw_when_pump_throws()
        {
            pump.ThrowOnStop = true;

            await receiver.Start(context => Task.CompletedTask, context => Task.FromResult(ErrorHandleResult.Handled));

            Assert.DoesNotThrowAsync(async () => await receiver.Stop());
        }

        [Test]
        public async Task Stop_should_dispose_pump() // for container backward compat reasons
        {
            await receiver.Start(context => Task.CompletedTask, context => Task.FromResult(ErrorHandleResult.Handled));

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


            public Task Start(PushRuntimeSettings limitations, Func<MessageContext, Task> onMessage, Func<ErrorContext, Task<ErrorHandleResult>> onError)
            {
                if (ThrowOnStart)
                {
                    throw new InvalidOperationException();
                }

                Started = true;

                return Task.CompletedTask;
            }

            public Task Stop()
            {
                if (ThrowOnStop)
                {
                    throw new InvalidOperationException();
                }

                Stopped = true;

                return Task.CompletedTask;
            }

            public IManageSubscriptions Subscriptions { get; }
            public string Id { get; }

            public void Dispose()
            {
                Disposed = true;
            }
        }
    }
}