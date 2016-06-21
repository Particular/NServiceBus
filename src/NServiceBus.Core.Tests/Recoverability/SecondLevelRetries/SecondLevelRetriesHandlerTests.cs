namespace NServiceBus.Core.Tests.Recoverability.SecondLevelRetries
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using DelayedDelivery;
    using DeliveryConstraints;
    using Extensibility;
    using NServiceBus.Pipeline;
    using NServiceBus.Routing;
    using NServiceBus.Transports;
    using NUnit.Framework;
    using Testing;

    [TestFixture]
    public class SecondLevelRetriesHandlerTests
    {
        [SetUp]
        public void SetUp()
        {
            failureInfoStorage = new FailureInfoStorage(10);
            dispatcher = new FakeDispatcher();
        }

        [Test]
        public async Task ShouldRetryIfPolicyReturnsADelay()
        {
            var message = CreateMessage(id: "message-id", slrRetryHeader: "1");
            var eventAggregator = new FakeEventAggregator();
            var context = CreateContext(message, eventAggregator);

            var delay = TimeSpan.FromSeconds(5);
            var behavior = new SecondLevelRetriesHandler(new FakePolicy(delay), failureInfoStorage, new DelayedRetryExecutor("local address", dispatcher));

            behavior.MarkForFutureDeferal(context, new Exception("exception-message"));
            var messageHandeled = await behavior.HandleIfPreviouslyFailed(context);

            Assert.IsTrue(messageHandeled);
            Assert.AreEqual(dispatcher.TransportOperations.UnicastTransportOperations.Count(), 1);
            Assert.AreEqual("exception-message", eventAggregator.GetNotification<MessageToBeRetried>().Exception.Message);
        }

        [Test]
        public Task ShouldSkipRetryIfNoDelayIsReturned()
        {
            var context = CreateContext();
            var behavior = new SecondLevelRetriesHandler(new FakePolicy(), failureInfoStorage, new DelayedRetryExecutor("local address", dispatcher));

            behavior.MarkForFutureDeferal(context, new Exception());

            Assert.That(async () => await behavior.HandleIfPreviouslyFailed(context), Throws.InstanceOf<Exception>());
            Assert.That(context.Message.Headers.ContainsKey(Headers.Retries) == false);
            Assert.That(dispatcher.TransportOperations, Is.Null);

            return Task.FromResult(0);
        }

        [Test]
        public async Task ShouldPullCurrentRetryCountFromHeaders()
        {
            var currentRetry = 3;
            var message = CreateMessage(slrRetryHeader: currentRetry.ToString());
            var context = CreateContext(message);

            var retryPolicy = new FakePolicy(TimeSpan.FromSeconds(5));
            var behavior = new SecondLevelRetriesHandler(retryPolicy, failureInfoStorage, new DelayedRetryExecutor("local address", dispatcher));

            behavior.MarkForFutureDeferal(context, new Exception());
            await behavior.HandleIfPreviouslyFailed(context);

            Assert.AreEqual(currentRetry + 1, retryPolicy.CurrentRetryValuePassed);
        }

        [Test]
        public Task ShouldAbortMessageReceiveWhenMarkingForDeferal()
        {
            var message = CreateMessage();
            var context = CreateContext(message);

            var behavior = new SecondLevelRetriesHandler(new FakePolicy(TimeSpan.FromSeconds(5)), failureInfoStorage, new DelayedRetryExecutor("local address", dispatcher));

            behavior.MarkForFutureDeferal(context, new Exception());

            Assert.IsTrue(context.ReceiveOperationAborted, "SLR should request receive operation abort when marking message for deferal.");

            return Task.FromResult(0);
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

        TestableTransportReceiveContext CreateContext(IncomingMessage incomingMessage, FakeEventAggregator eventAggregator = null)
        {
            var context = new TestableTransportReceiveContext
            {
                Message = incomingMessage
            };

            context.Extensions.Set<IEventAggregator>(eventAggregator ?? new FakeEventAggregator());

            return context;
        }

        FakeDispatcher dispatcher;
        FailureInfoStorage failureInfoStorage;
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

    class FakePolicy : SecondLevelRetryPolicy
    {
        public FakePolicy()
        {
        }

        public FakePolicy(TimeSpan delayToReturn)
        {
            this.delayToReturn = delayToReturn;
        }

        public int CurrentRetryValuePassed { get; private set; }

        public override bool TryGetDelay(SecondLevelRetryContext slrRetryContext, out TimeSpan delay)
        {
            CurrentRetryValuePassed = slrRetryContext.SecondLevelRetryAttempt;

            if (!delayToReturn.HasValue)
            {
                delay = TimeSpan.MinValue;
                return false;
            }
            delay = delayToReturn.Value;
            return true;
        }

        TimeSpan? delayToReturn;
    }

    class FakeDispatcher : IDispatchMessages
    {
        public TransportOperations TransportOperations { get; private set; }

        public Task Dispatch(TransportOperations outgoingMessages, ContextBag context)
        {
            TransportOperations = outgoingMessages;
            return TaskEx.CompletedTask;
        }
    }
}