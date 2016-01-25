namespace NServiceBus.Core.Tests
{
    using System;
    using System.Threading.Tasks;
    using NServiceBus.OutgoingPipeline;
    using NServiceBus.Pipeline;
    using NServiceBus.Transports;
    using NUnit.Framework;

    [TestFixture]
    public class BusSessionTests
    {
        [Test]
        public async Task CanDispatchMultipleTimes()
        {
            var session = CreateBusSession(c => { });
            await session.Dispatch();

            Assert.That(async () => await session.Dispatch(), Throws.Nothing);
        }

        [Test]
        public void CanDisposeMultipleTimes()
        {
            var session = CreateBusSession(c => { });
            session.Dispose();

            Assert.That(() => session.Dispose(), Throws.Nothing);
        }

        [Test]
        public void DisposeWithoutOperationsAndDispatchDoesNotInvokeBatchPipeline()
        {
            IBatchDispatchContext context = null;
            var session = CreateBusSession(c => { context = c; });

            session.Dispose();

            Assert.That(context, Is.Null);
        }

        [Test]
        public async Task DisposeWithOperationsWithoutDispatchDoesNotInvokeBatchPipeline()
        {
            IBatchDispatchContext context = null;
            var session = CreateBusSession(c => { context = c; });

            await session.Send(new object(), new SendOptions());

            session.Dispose();

            Assert.That(context, Is.Null);
        }

        [Test]
        public async Task DisposeWithoutOperationsAndWithDispatchDoesNotInvokeBatchPipeline()
        {
            IBatchDispatchContext context = null;
            var session = CreateBusSession(c => { context = c; });

            await session.Dispatch();
            session.Dispose();

            Assert.That(context, Is.Null);
        }

        [Test]
        public async Task DisposeWithOperationWithDispatchDoesInvokeBatchPipeline()
        {
            IBatchDispatchContext context = null;
            var session = CreateBusSession(c => { context = c; });

            await session.Send(new object(), new SendOptions());
            await session.Dispatch();
            session.Dispose();

            Assert.That(context, Is.Not.Null);
            Assert.AreEqual(1, context.Operations.Count);
        }

        [Test]
        public async Task DisposeWithMultipleOperationsWithDispatchDoesInvokeBatchPipeline()
        {
            IBatchDispatchContext context = null;
            var session = CreateBusSession(c => { context = c; });

            await session.Send(new object(), new SendOptions());
            await session.Send(new object(), new SendOptions());
            await session.Send(new object(), new SendOptions());
            await session.Dispatch();
            session.Dispose();

            Assert.That(context, Is.Not.Null);
            Assert.AreEqual(3, context.Operations.Count);
        }

        [Test]
        public async Task DispatchMultipleTimesWithOperationsDoesOnlyDispatchOnce()
        {
            var called = 0;
            var session = CreateBusSession(c => { called++; });

            await session.Send(new object(), new SendOptions());
            await session.Send(new object(), new SendOptions());
            await session.Send(new object(), new SendOptions());
            await session.Dispatch();

            await session.Dispatch();
            session.Dispose();

            Assert.AreEqual(1, called);
        }

        static BusSession CreateBusSession(Action<IBatchDispatchContext> contextAction)
        {
            return new BusSession(new TransportSendContext(new TransportTransaction(), new RootContext(null, new FakeCache(contextAction))));
        }


        class FakeCache : IPipelineCache
        {
            readonly Action<IBatchDispatchContext> contextAction;

            public FakeCache(Action<IBatchDispatchContext> contextAction)
            {
                this.contextAction = contextAction;
            }

            public IPipeline<TContext> Pipeline<TContext>() where TContext : IBehaviorContext
            {
                if (typeof(TContext) == typeof(IOutgoingSendContext))
                {
                    return (IPipeline<TContext>)new FakeSendPipeline();
                }
                return (IPipeline<TContext>)new FakeBatchPipeline(contextAction);
            }
        }

        class FakeSendPipeline : IPipeline<IOutgoingSendContext>
        {
            public Task Invoke(IOutgoingSendContext context)
            {
                var operations = context.Extensions.Get<PendingTransportOperations>();
                // Faking out real pipeline
                operations.Add(new TransportOperation(null, null, DispatchConsistency.Default));
                return TaskEx.CompletedTask;
            }
        }

        class FakeBatchPipeline : IPipeline<IBatchDispatchContext>
        {
            readonly Action<IBatchDispatchContext> contextAction;

            public FakeBatchPipeline(Action<IBatchDispatchContext> contextAction)
            {
                this.contextAction = contextAction;
            }

            public Task Invoke(IBatchDispatchContext context)
            {
                contextAction(context);
                return TaskEx.CompletedTask;
            }
        }
    }
}