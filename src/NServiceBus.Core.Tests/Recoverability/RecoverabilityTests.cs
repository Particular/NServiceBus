namespace NServiceBus.Core.Tests.Recoverability
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using DelayedDelivery;
    using DeliveryConstraints;
    using NServiceBus.Pipeline;
    using NServiceBus.Routing;
    using NServiceBus.Transports;
    using NUnit.Framework;

    [TestFixture]
    public class RecoverabilityTests
    {
        FakeDispatchPipeline dispatchPipeline;

        [SetUp]
        public void SetUp()
        {
            dispatchPipeline = new FakeDispatchPipeline();
        }

        [Test]
        public async Task ShouldAbortMessageReceiveWhenMarkingForDeferal()
        {
            var policy = new FakePolicy();
            var failureStorage = new FailureInfoStorage(10);
            var criticalError = new CriticalError(c => Task.FromResult(0));

            var chain = new BehaviorChain(new[]
            {
                new BehaviorInstance(typeof(MoveFaultsToErrorQueueBehavior), new MoveFaultsToErrorQueueBehavior(criticalError, "", "", TransportTransactionMode.None, failureStorage)),
                new BehaviorInstance(typeof(SecondLevelRetriesBehavior), new SecondLevelRetriesBehavior(policy, "", failureStorage)),
                new BehaviorInstance(typeof(FailingBehavior), new FailingBehavior())
            });

            var context = CreateContext();

            await chain.Invoke(context);

            Assert.IsTrue(context.ReceiveOperationWasAborted, "Message receive should be aborted by SecondLevelRetries");
        }

        [Test]
        public async Task ShouldImmediatellyDelayFailedMessage()
        {
            var policy = new FakePolicy();
            var failureStorage = new FailureInfoStorage(10);
            var criticalError = new CriticalError(c => Task.FromResult(0));
            var trackingBehavior = new TrackingBehavior();

            var chain = new BehaviorChain(new[]
            {
                new BehaviorInstance(typeof(MoveFaultsToErrorQueueBehavior), new MoveFaultsToErrorQueueBehavior(criticalError, "", "", TransportTransactionMode.None, failureStorage)),
                new BehaviorInstance(typeof(SecondLevelRetriesBehavior), new SecondLevelRetriesBehavior(policy, "", failureStorage)),
                new BehaviorInstance(typeof(TrackingBehavior), trackingBehavior)
            });

            var message = CreateMessage();

            await chain.Invoke(CreateContext(message));
            await chain.Invoke(CreateContext(message));

            Assert.AreEqual(dispatchPipeline.MessageId, message.MessageId);
            Assert.IsFalse(trackingBehavior.WasCalled);
        }

        class TrackingBehavior : ForkConnector<ITransportReceiveContext, IRoutingContext>
        {
            public bool WasCalled { get; set; }
            public override Task Invoke(ITransportReceiveContext context, Func<Task> next, Func<IRoutingContext, Task> fork)
            {
                WasCalled = true;

                return Task.FromResult(0);
            }
        }

        class FailingBehavior : ForkConnector<ITransportReceiveContext, IRoutingContext>
        {
            public override Task Invoke(ITransportReceiveContext context, Func<Task> next, Func<IRoutingContext, Task> fork)
            {
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

        FakeTransportReceiveContext CreateContext()
        {
            return CreateContext(CreateMessage());
        }

        FakeTransportReceiveContext CreateContext(IncomingMessage message)
        {
            var context = new FakeTransportReceiveContext(message);

            context.Extensions.Set<IPipelineCache>(new FakePipelineCache(dispatchPipeline));

            return context;
        }

        class FakePolicy : SecondLevelRetryPolicy
        {
            public override bool TryGetDelay(IncomingMessage message, Exception ex, int currentRetry, out TimeSpan delay)
            {
                throw new NotImplementedException();
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
            public IRoutingContext RoutingContext { get; set; }

            public string MessageId => RoutingContext.Message.MessageId;

            public TimeSpan MessageDeliveryDelay => ((DelayDeliveryWith) RoutingContext.Extensions.GetDeliveryConstraints().Single(c => c is DelayDeliveryWith)).Delay;

            public string MessageDestination => ((UnicastAddressTag) RoutingContext.RoutingStrategies.First().Apply(new Dictionary<string, string>())).Destination;

            public Dictionary<string, string> MessageHeaders => RoutingContext.Message.Headers;

            public byte[] MessageBody => RoutingContext.Message.Body;

            public Task Invoke(IRoutingContext context)
            {
                RoutingContext = context;
                return TaskEx.CompletedTask;
            }
        }

        class FakeTransportReceiveContext : FakeBehaviorContext, ITransportReceiveContext
        {
            public FakeTransportReceiveContext(IncomingMessage message)
            {
                Message = message;
            }

            public bool ReceiveOperationWasAborted { get; private set; }

            public IncomingMessage Message { get; }

            public void AbortReceiveOperation()
            {
                ReceiveOperationWasAborted = true;
            }
        }
    }
}