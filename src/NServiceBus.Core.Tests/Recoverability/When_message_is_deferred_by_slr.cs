namespace NServiceBus.Core.Tests.Recoverability
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Threading.Tasks;
    using NServiceBus.Pipeline;
    using NServiceBus.Transports;
    using NUnit.Framework;
    using Testing;

    [TestFixture]
    public class When_message_is_deferred_by_slr
    {
        FakeDispatchPipeline dispatchPipeline;

        [SetUp]
        public void SetUp()
        {
            dispatchPipeline = new FakeDispatchPipeline();
        }

        [Test]
        public async Task _should_abort_message_receive_when_marking_for_deferral()
        {
            var policy = new FakePolicy();
            var failureStorage = new FailureInfoStorage(10);
            var criticalError = new CriticalError(c => Task.FromResult(0));

            var chain = new BehaviorChain(new[]
            {
                new BehaviorInstance(typeof(MoveFaultsToErrorQueueBehavior), new MoveFaultsToErrorQueueBehavior(criticalError, "error", "", TransportTransactionMode.None, failureStorage)),
                new BehaviorInstance(typeof(SecondLevelRetriesBehavior), new SecondLevelRetriesBehavior(policy, "", failureStorage)),
                new BehaviorInstance(typeof(FirstLevelRetriesBehavior), new FirstLevelRetriesBehavior(failureStorage, new FirstLevelRetryPolicy(0))), 
                new BehaviorInstance(typeof(FailingBehavior), new FailingBehavior())
            });

            var context = CreateContext();

            await chain.Invoke(context);

            Assert.IsTrue(context.ReceiveOperationAborted, "Message receive should be aborted by SecondLevelRetries");
        }

        [Test]
        public async Task _should_schedule_delayed_delivery_for_failed_message()
        {
            var policy = new FakePolicy();
            var failureStorage = new FailureInfoStorage(10);
            var criticalError = new CriticalError(c => Task.FromResult(0));
            var failingBehavior = new FailingBehavior();

            var chain = new BehaviorChain(new[]
            {
                new BehaviorInstance(typeof(MoveFaultsToErrorQueueBehavior), new MoveFaultsToErrorQueueBehavior(criticalError, "error", "", TransportTransactionMode.None, failureStorage)),
                new BehaviorInstance(typeof(SecondLevelRetriesBehavior), new SecondLevelRetriesBehavior(policy, "", failureStorage)),
                new BehaviorInstance(typeof(FirstLevelRetriesBehavior), new FirstLevelRetriesBehavior(failureStorage, new FirstLevelRetryPolicy(0))), 
                new BehaviorInstance(typeof(FailingBehavior), failingBehavior)
            });

            var message = CreateMessage();

            await chain.Invoke(CreateContext(message));
            await chain.Invoke(CreateContext(message));

            Assert.AreEqual(dispatchPipeline.MessageId, message.MessageId);
            Assert.AreEqual(1, failingBehavior.NumberOfCalls);
        }

        class FailingBehavior : ForkConnector<ITransportReceiveContext, IRoutingContext>
        {
            public int NumberOfCalls { get; private set; }

            public override Task Invoke(ITransportReceiveContext context, Func<Task> next, Func<IRoutingContext, Task> fork)
            {
                NumberOfCalls++;
                throw new Exception();
            }
        }

        IncomingMessage CreateMessage(string id = "id", string slrRetryHeader = null, byte[] body = null)
        {
            var headers = string.IsNullOrEmpty(slrRetryHeader)
                ? new Dictionary<string, string>()
                : new Dictionary<string, string>
                {
                    {Headers.Retries, slrRetryHeader}
                };

            return new IncomingMessage(id, headers, new MemoryStream(body ?? new byte[0]));
        }

        TestableTransportReceiveContext CreateContext()
        {
            return CreateContext(CreateMessage());
        }

        TestableTransportReceiveContext CreateContext(IncomingMessage message)
        {
            var context = new TestableTransportReceiveContext {Message = message };

            context.Extensions.Set<IEventAggregator>(new FakeEventAggregator());
            context.Extensions.Set<IPipelineCache>(new FakePipelineCache(dispatchPipeline));

            return context;
        }

        class FakePolicy : SecondLevelRetryPolicy
        {
            public override bool TryGetDelay(IncomingMessage message, Exception ex, int currentRetry, out TimeSpan delay)
            {
                delay = TimeSpan.FromSeconds(10);
                return true;
            }
        }

        class FakePipelineCache : IPipelineCache
        {
            public FakePipelineCache(IPipeline<IRoutingContext> pipeline)
            {
                this.pipeline = pipeline;
            }

            public IPipeline<TContext> Pipeline<TContext>()
                where TContext : IBehaviorContext

            {
                return (IPipeline<TContext>) pipeline;
            }

            IPipeline<IRoutingContext> pipeline;
        }

        class FakeDispatchPipeline : IPipeline<IRoutingContext>
        {
            IRoutingContext RoutingContext { get; set; }

            public string MessageId => RoutingContext.Message.MessageId;
            
            public Task Invoke(IRoutingContext context)
            {
                RoutingContext = context;
                return TaskEx.CompletedTask;
            }
        }
    }
}