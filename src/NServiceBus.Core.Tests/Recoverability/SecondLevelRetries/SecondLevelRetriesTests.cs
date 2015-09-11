namespace NServiceBus.Core.Tests.Recoverability.SecondLevelRetries
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using NServiceBus.DelayedDelivery;
    using NServiceBus.DeliveryConstraints;
    using NServiceBus.Faults;
    using NServiceBus.Pipeline;
    using NServiceBus.Pipeline.Contexts;
    using NServiceBus.Recoverability.SecondLevelRetries;
    using NServiceBus.Routing;
    using NServiceBus.TransportDispatch;
    using NServiceBus.Transports;
    using NServiceBus.Unicast.Transport;
    using NUnit.Framework;

    [TestFixture]
    public class SecondLevelRetriesTests
    {
        [Test]
        public void ShouldRetryIfPolicyReturnsADelay()
        {
            var notifications = new BusNotifications();

            var delay = TimeSpan.FromSeconds(5);
            var fakeDispatchPipeline = new FakeDispatchPipeline();
            var behavior = new SecondLevelRetriesBehavior(fakeDispatchPipeline, new FakePolicy(delay), notifications, "test-address-for-this-pipeline");
            behavior.Initialize(new PipelineInfo("Test", "IncomingQueueForThisPipeline"));

            var slrNotification = new SecondLevelRetry();

            notifications.Errors.MessageHasBeenSentToSecondLevelRetries.Subscribe(slr => { slrNotification = slr; });

            behavior.Invoke(CreateContext("someid", 1), () => { throw new Exception("testex"); });

            Assert.AreEqual("someid", fakeDispatchPipeline.DispatchContext.Get<OutgoingMessage>().Headers[Headers.MessageId]);
            Assert.AreEqual(delay, ((DelayDeliveryWith) fakeDispatchPipeline.DispatchContext.GetDeliveryConstraints().Single(c => c is DelayDeliveryWith)).Delay);
            Assert.AreEqual("test-address-for-this-pipeline", ((DirectToTargetDestination) fakeDispatchPipeline.DispatchContext.Get<RoutingStrategy>()).Destination);
            Assert.AreEqual("testex", slrNotification.Exception.Message);
        }

        [Test]
        public void ShouldSetTimestampHeaderForFirstRetry()
        {
            var delay = TimeSpan.FromSeconds(5);
            var fakeDispatchPipeline = new FakeDispatchPipeline();
            var behavior = new SecondLevelRetriesBehavior(fakeDispatchPipeline, new FakePolicy(delay), new BusNotifications(), "MyAddress");
            behavior.Initialize(new PipelineInfo("Test", "test-address-for-this-pipeline"));

            behavior.Invoke(CreateContext("someid", 0), () => { throw new Exception("testex"); });

            Assert.True(fakeDispatchPipeline.DispatchContext.Get<OutgoingMessage>().Headers.ContainsKey(SecondLevelRetriesBehavior.RetriesTimestamp));
        }

        [Test]
        public void ShouldSkipRetryIfNoDelayIsReturned()
        {
            var fakeDispatchPipeline = new FakeDispatchPipeline();
            var behavior = new SecondLevelRetriesBehavior(fakeDispatchPipeline, new FakePolicy(), new BusNotifications(), "MyAddress");
            behavior.Initialize(new PipelineInfo("Test", "test-address-for-this-pipeline"));
            var context = CreateContext("someid", 1);

            Assert.Throws<Exception>(() => behavior.Invoke(context, () => { throw new Exception("testex"); }));

            Assert.False(context.GetPhysicalMessage().Headers.ContainsKey(Headers.Retries));
        }

        [Test]
        public void ShouldSkipRetryForDeserializationErrors()
        {
            var fakeDispatchPipeline = new FakeDispatchPipeline();
            var behavior = new SecondLevelRetriesBehavior(fakeDispatchPipeline, new FakePolicy(TimeSpan.FromSeconds(5)), new BusNotifications(), "MyAddress");
            behavior.Initialize(new PipelineInfo("Test", "test-address-for-this-pipeline"));
            var context = CreateContext("someid", 1);

            Assert.Throws<MessageDeserializationException>(() => behavior.Invoke(context, () => { throw new MessageDeserializationException("testex"); }));
            Assert.False(context.GetPhysicalMessage().Headers.ContainsKey(Headers.Retries));
        }

        [Test]
        public void ShouldPullCurrentRetryCountFromHeaders()
        {
            var retryPolicy = new FakePolicy(TimeSpan.FromSeconds(5));

            var fakeDispatchPipeline = new FakeDispatchPipeline();
            var behavior = new SecondLevelRetriesBehavior(fakeDispatchPipeline, retryPolicy, new BusNotifications(), "MyAddress");
            behavior.Initialize(new PipelineInfo("Test", "test-address-for-this-pipeline"));

            var currentRetry = 3;

            behavior.Invoke(CreateContext("someid", currentRetry), () => { throw new Exception("testex"); });

            Assert.AreEqual(currentRetry + 1, retryPolicy.InvokedWithCurrentRetry);
        }

        [Test]
        public void ShouldDefaultRetryCountToZeroIfNoHeaderIsFound()
        {
            var retryPolicy = new FakePolicy(TimeSpan.FromSeconds(5));
            var context = CreateContext("someid", 2);

            context.GetPhysicalMessage().Headers.Clear();

            var fakeDispatchPipeline = new FakeDispatchPipeline();
            var behavior = new SecondLevelRetriesBehavior(fakeDispatchPipeline, retryPolicy, new BusNotifications(), "MyAddress");
            behavior.Initialize(new PipelineInfo("Test", "test-address-for-this-pipeline"));

            behavior.Invoke(context, () => { throw new Exception("testex"); });

            Assert.AreEqual(1, retryPolicy.InvokedWithCurrentRetry);
            Assert.AreEqual("1", fakeDispatchPipeline.DispatchContext.Get<OutgoingMessage>().Headers[Headers.Retries]);
        }

        [Test]
        public void ShouldRevertMessageBodyWhenDispatchingMessage()
        {
            const string originalContent = "original content";
            var context = CreateContext("someId", 1, Encoding.UTF8.GetBytes(originalContent));
            var fakeDispatchPipeline = new FakeDispatchPipeline();
            var retryPolicy = new FakePolicy(TimeSpan.FromSeconds(0));
            var behavior = new SecondLevelRetriesBehavior(fakeDispatchPipeline, retryPolicy, new BusNotifications(), "test-address-for-this-pipeline");
            behavior.Initialize(new PipelineInfo("Test", "test-address-for-this-pipeline"));

            var message = context.GetPhysicalMessage();
            message.Body = Encoding.UTF8.GetBytes("modified content");

            behavior.Invoke(context, () => { throw new Exception("test"); });

            var dispatchedMessage = fakeDispatchPipeline.DispatchContext.Get<OutgoingMessage>();
            Assert.AreEqual(originalContent, Encoding.UTF8.GetString(dispatchedMessage.Body));
            Assert.AreEqual(originalContent, Encoding.UTF8.GetString(message.Body));
        }

        PhysicalMessageProcessingStageBehavior.Context CreateContext(string messageId, int currentRetryCount, byte[] messageBody = null)
        {
            var context = new PhysicalMessageProcessingStageBehavior.Context(new TransportReceiveContext(new IncomingMessage(messageId, new Dictionary<string, string>
            {
                {Headers.Retries, currentRetryCount.ToString()}
            }, new MemoryStream(messageBody ?? new byte[0])), null));
            return context;
        }
    }

    class FakeDispatchPipeline : IPipelineBase<DispatchContext>
    {
        public DispatchContext DispatchContext { get; set; }

        public void Invoke(DispatchContext context)
        {
            DispatchContext = context;
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

        public override bool TryGetDelay(TransportMessage message, Exception ex, int currentRetry, out TimeSpan delay)
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