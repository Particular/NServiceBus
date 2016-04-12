namespace NServiceBus.Core.Tests.Recoverability.SecondLevelRetries
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using DelayedDelivery;
    using DeliveryConstraints;
    using NServiceBus.Pipeline;
    using NServiceBus.Routing;
    using NServiceBus.Transports;
    using NUnit.Framework;
    using Testing;

    [TestFixture]
    public class SecondLevelRetriesTests
    {
        FakeDispatchPipeline dispatchPipline;
        FailureInfoStorage failureInfoStorage;

        [SetUp]
        public void SetUp()
        {
            dispatchPipline = new FakeDispatchPipeline();
            failureInfoStorage = new FailureInfoStorage(10);
        }

        [Test]
        public async Task ShouldRetryIfPolicyReturnsADelay()
        {
            var message = CreateMessage(id: "message-id", slrRetryHeader: "1");
            var eventAggregator = new FakeEventAggregator();
            var context = CreateContext(message, eventAggregator);

            var delay = TimeSpan.FromSeconds(5);
            var behavior = new SecondLevelRetriesBehavior(new FakePolicy(delay), "deferral-address", failureInfoStorage);

            await behavior.Invoke(context, () => { throw new Exception("exception-message"); });
            await behavior.Invoke(context, () => Task.FromResult(0));

            Assert.AreEqual("message-id", dispatchPipline.MessageId);
            Assert.AreEqual(delay, dispatchPipline.MessageDeliveryDelay);
            Assert.AreEqual("deferral-address", dispatchPipline.MessageDestination);
            Assert.AreEqual("exception-message", eventAggregator.GetNotification<MessageToBeRetried>().Exception.Message);
        }

        [Test]
        public async Task ShouldSetTimestampHeaderForFirstRetry()
        {
            var message = CreateMessage();
            var context = CreateContext(message);

            var delay = TimeSpan.FromSeconds(5);
            var behavior = new SecondLevelRetriesBehavior(new FakePolicy(delay), string.Empty, failureInfoStorage);

            await behavior.Invoke(context, () => { throw new Exception(); });
            await behavior.Invoke(context, () => Task.FromResult(0) );

            var retryTimestampHeader = dispatchPipline.MessageHeaders.ContainsKey(Headers.RetriesTimestamp)
                ? dispatchPipline.MessageHeaders[Headers.RetriesTimestamp]
                : null;

            Assert.NotNull(retryTimestampHeader, "Message should have retry timestamp set.");
            Assert.DoesNotThrow(() => DateTimeExtensions.ToUtcDateTime(retryTimestampHeader), "Timestamp should be a proper format.");
        }

        [Test]
        public async Task ShouldSkipRetryIfNoDelayIsReturned()
        {
            var context = CreateContext();
            var behavior = new SecondLevelRetriesBehavior(new FakePolicy(), string.Empty, failureInfoStorage);

            await behavior.Invoke(context, () => { throw new Exception(); });

            Assert.That(async () => await behavior.Invoke(context, () => Task.FromResult(0)), Throws.InstanceOf<Exception>());
            Assert.That(context.Message.Headers.ContainsKey(Headers.Retries) == false);
        }

        [Test]
        public void ShouldSkipRetryForDeserializationErrors()
        {
            var message = CreateMessage(slrRetryHeader: "1");
            var context = CreateContext(message);
            var behavior = new SecondLevelRetriesBehavior(new FakePolicy(TimeSpan.FromSeconds(5)), string.Empty, failureInfoStorage);

            var behaviorInvocation = new TestDelegate(async () => await behavior.Invoke(context, () => { throw new MessageDeserializationException(string.Empty); }));

            Assert.That(behaviorInvocation, Throws.InstanceOf<MessageDeserializationException>());
            Assert.That(context.Message.Headers.ContainsKey(Headers.Retries) == false);
        }

        [Test]
        public async Task ShouldPullCurrentRetryCountFromHeaders()
        {
            var currentRetry = 3;
            var message = CreateMessage(slrRetryHeader: currentRetry.ToString());
            var context = CreateContext(message);

            var retryPolicy = new FakePolicy(TimeSpan.FromSeconds(5));
            var behavior = new SecondLevelRetriesBehavior(retryPolicy, string.Empty, failureInfoStorage);
            
            await behavior.Invoke(context, () => { throw new Exception("testex"); });
            await behavior.Invoke(context, () => Task.FromResult(0));

            Assert.AreEqual(currentRetry + 1, retryPolicy.InvokedWithCurrentRetry);
        }

        [Test]
        public async Task ShouldRevertMessageBodyWhenDispatchingMessage()
        {
            var originalContent = Encoding.UTF8.GetBytes("original");
            var message = CreateMessage(slrRetryHeader: "1", body: originalContent);
            var context = CreateContext(message);

            var behavior = new SecondLevelRetriesBehavior(new FakePolicy(TimeSpan.FromSeconds(0)), string.Empty, failureInfoStorage);

            await behavior.Invoke(context, c =>
            {
                c.Message.Body = Encoding.UTF8.GetBytes("modified");
                throw new Exception();
            });
      
            await behavior.Invoke(context, () => Task.FromResult(0));
            
            CollectionAssert.AreEqual(originalContent, context.Message.Body);
        }

        [Test]
        public async Task ShouldNotInvokeContinuationAfterMessageFailure()
        {
            var message = CreateMessage(slrRetryHeader: "1");
            var context = CreateContext(message);
            
            var behavior = new SecondLevelRetriesBehavior(new FakePolicy(TimeSpan.FromSeconds(5)), string.Empty, failureInfoStorage);
            var calledTwice = false;


            await behavior.Invoke(context, () => { throw new Exception(); });
            await behavior.Invoke(context, () =>
            {
                calledTwice = true;
                return Task.FromResult(0);
            });

            Assert.IsFalse(calledTwice, "SLR should not call pipline continuation when processing a message marked for defferal.");
        }

        [Test]
        public async Task ShouldAbortMessageReceiveWhenMarkingForDeferal()
        {
            var message = CreateMessage();
            var context = CreateContext(message);

            var behavior = new SecondLevelRetriesBehavior(new FakePolicy(TimeSpan.FromSeconds(5)), string.Empty, failureInfoStorage);

            await behavior.Invoke(context, () => { throw new Exception(); });

            Assert.IsTrue(context.ReceiveOperationAborted, "SLR should request receive operation abort when marking message for deferal.");
        }

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
            context.Extensions.Set<IPipelineCache>(new FakePipelineCache(dispatchPipline));

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

        public override bool TryGetDelay(IncomingMessage message, Exception ex, int currentRetry, out TimeSpan delay)
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