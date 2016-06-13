namespace NServiceBus.Core.Tests.Recoverability.SecondLevelRetries
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
    using Testing;
    using Timeout;
    using Timeout.TimeoutManager;

    [TestFixture]
    public class SecondLevelRetriesTests
    {
        FakeDispatchPipeline dispatchPipeline;
        FailureInfoStorage failureInfoStorage;

        [SetUp]
        public void SetUp()
        {
            dispatchPipeline = new FakeDispatchPipeline();
            failureInfoStorage = new FailureInfoStorage(10);
        }

        [Test]
        public Task ShouldRetryIfPolicyReturnsADelay()
        {
            var message = CreateMessage(id: "message-id", slrRetryHeader: "1");
            var eventAggregator = new FakeEventAggregator();
            var context = CreateContext(message, eventAggregator);
            var dispatcher = new RecordingFakeDispatcher();

            var delay = TimeSpan.FromSeconds(5);
            var behavior = new SecondLevelRetriesBehavior(new FakePolicy(delay), "deferral-address", failureInfoStorage, dispatcher);

            var messageDeclaredDefered = behavior.Invoke(new Exception(), 1, context.Message.Headers);

            var dispatchOperation = dispatcher.DispatchedMessages[0].Operations.UnicastTransportOperations.FirstOrDefault();

            Assert.NotNull(dispatchOperation);
            Assert.AreEqual("message-id", dispatchOperation.Message.MessageId);
            Assert.IsTrue(dispatchOperation.DeliveryConstraints.Any(c => c is DelayDeliveryWith && ((DelayDeliveryWith) c).Delay == delay));
            Assert.IsTrue(messageDeclaredDefered);
            Assert.AreEqual("deferral-address", dispatchOperation.Destination);
            Assert.AreEqual("exception-message", eventAggregator.GetNotification<MessageToBeRetried>().Exception.Message);

            return Task.FromResult(0);
        }

        [Test]
        public Task ShouldSetTimestampHeaderForFirstRetry()
        {
            var message = CreateMessage();
            var context = CreateContext(message);
            var dispatcher = new RecordingFakeDispatcher();

            var delay = TimeSpan.FromSeconds(5);
            var behavior = new SecondLevelRetriesBehavior(new FakePolicy(delay), string.Empty, failureInfoStorage, dispatcher);

            behavior.Invoke(new Exception(), 1, context.Message.Headers);

            var headers = dispatcher.DispatchedMessages[0].Operations.UnicastTransportOperations.First().Message.Headers;

            var retryTimestampHeader = headers.ContainsKey(Headers.RetriesTimestamp)
                ? dispatchPipeline.MessageHeaders[Headers.RetriesTimestamp]
                : null;

            Assert.NotNull(retryTimestampHeader, "Message should have retry timestamp set.");
            Assert.DoesNotThrow(() => DateTimeExtensions.ToUtcDateTime(retryTimestampHeader), "Timestamp should be a proper format.");

            return Task.FromResult(0);
        }

        [Test]
        public Task ShouldSkipRetryIfNoDelayIsReturned()
        {
            var context = CreateContext();
            var dispatcher = new RecordingFakeDispatcher();

            var behavior = new SecondLevelRetriesBehavior(new FakePolicy(), string.Empty, failureInfoStorage, dispatcher);

            var messageDeclaredAsDefered = behavior.Invoke(new Exception(), 1, context.Message.Headers);

            Assert.IsFalse(messageDeclaredAsDefered);
            Assert.AreEqual(0, dispatcher.DispatchedMessages);

            return Task.FromResult(0);
        }

        [Test]
        public Task ShouldSkipRetryForDeserializationErrors()
        {
            var message = CreateMessage(slrRetryHeader: "1");
            var context = CreateContext(message);
            var dispatcher = new RecordingFakeDispatcher();

            var behavior = new SecondLevelRetriesBehavior(new FakePolicy(TimeSpan.FromSeconds(5)), string.Empty, failureInfoStorage, dispatcher);

            var messageDeclaredAsDefered = behavior.Invoke(new MessageDeserializationException(String.Empty), 1, context.Message.Headers);

            Assert.IsFalse(messageDeclaredAsDefered);
            Assert.AreEqual(0, dispatcher.DispatchedMessages);

            return Task.FromResult(0);
        }

        [Test]
        public Task ShouldPullCurrentRetryCountFromHeaders()
        {
            var currentRetry = 3;
            var message = CreateMessage(slrRetryHeader: currentRetry.ToString());
            var context = CreateContext(message);

            var retryPolicy = new FakePolicy(TimeSpan.FromSeconds(5));
            var behavior = new SecondLevelRetriesBehavior(retryPolicy, string.Empty, failureInfoStorage, new FakeMessageDispatcher());

            behavior.Invoke(new Exception(), currentRetry, context.Message.Headers);
            
            Assert.AreEqual(currentRetry + 1, retryPolicy.InvokedWithCurrentRetry);

            return Task.FromResult(0);
        }

        /* This one was regression I think we still need this but currently that should probably be ATT 
        [Test]
        public async Task ShouldInvokePipelineWhenDeferredMessageDeliveredImmediately()
        {
            var message = CreateMessage();
            var eventAggregator = new FakeEventAggregator();
            var context = CreateContext(message, eventAggregator);

            var delay = TimeSpan.FromSeconds(1);
            var behavior = new SecondLevelRetriesBehavior(new FakePolicy(delay), "", failureInfoStorage);

            var continuationCalledAfterDeferral = false;

            await behavior.Invoke(context, () => { throw new Exception(); }, c => Task.FromResult(0));

            await behavior.Invoke(context, () => Task.FromResult(0), c =>
            {
                return behavior.Invoke(context, () =>
                {
                    continuationCalledAfterDeferral = true;
                    return Task.FromResult(0);
                });
            });

            Assert.IsTrue(continuationCalledAfterDeferral);
        }
        */

        IncomingMessage CreateMessage(string id = "id", string slrRetryHeader = null, byte[] body = null)
        {
            var headers = string.IsNullOrEmpty(slrRetryHeader)
                ? new Dictionary<string, string>()
                : new Dictionary<string, string> { { Headers.Retries, slrRetryHeader }};

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
            context.Extensions.Set<IPipelineCache>(new FakePipelineCache(dispatchPipeline));

            return context;
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

        public string MessageDestination => ((UnicastAddressTag)RoutingContext.RoutingStrategies.First().Apply(new Dictionary<string, string>())).Destination;

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

        public int InvokedWithCurrentRetry { get; private set; }

        public override bool TryGetDelay(Dictionary<string, string> message, Exception ex, int currentRetry, out TimeSpan delay)
        {
            InvokedWithCurrentRetry = currentRetry;

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
}