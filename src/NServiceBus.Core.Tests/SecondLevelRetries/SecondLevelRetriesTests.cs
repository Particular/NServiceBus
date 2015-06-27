namespace NServiceBus.Core.Tests.SecondLevelRetries
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using NServiceBus.DelayedDelivery;
    using NServiceBus.DeliveryConstraints;
    using NServiceBus.Faults;
    using NServiceBus.Pipeline;
    using NServiceBus.Pipeline.Contexts;
    using NServiceBus.Routing;
    using NServiceBus.SecondLevelRetries;
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
            var behavior = new SecondLevelRetriesBehavior(fakeDispatchPipeline, new FakePolicy(delay), notifications);
            behavior.Initialize(new PipelineInfo("Test", "test-address-for-this-pipeline"));

            var slrNotification = new SecondLevelRetry();

            notifications.Errors.MessageHasBeenSentToSecondLevelRetries.Subscribe(slr =>
            {
                slrNotification = slr; });

            behavior.Invoke(CreateContext("someid", 1), () => { throw new Exception("testex"); });

            Assert.AreEqual("someid", fakeDispatchPipeline.DispatchContext.Get<OutgoingMessage>().Headers[Headers.MessageId]);
            Assert.AreEqual(delay, ((DelayDeliveryWith)fakeDispatchPipeline.DispatchContext.GetDeliveryConstraints().Single(c=>c is DelayDeliveryWith)).Delay);
            Assert.AreEqual("test-address-for-this-pipeline", ((DirectToTargetDestination)fakeDispatchPipeline.DispatchContext.Get<RoutingStrategy>()).Destination);
            Assert.AreEqual("testex", slrNotification.Exception.Message);
        }

        [Test]
        public void ShouldSetTimestampHeaderForFirstRetry()
        {
            var delay = TimeSpan.FromSeconds(5);
            var fakeDispatchPipeline = new FakeDispatchPipeline();
            var behavior = new SecondLevelRetriesBehavior(fakeDispatchPipeline, new FakePolicy(delay), new BusNotifications());
            behavior.Initialize(new PipelineInfo("Test", "test-address-for-this-pipeline"));

            behavior.Invoke(CreateContext("someid", 0), () => { throw new Exception("testex"); });

            Assert.True(fakeDispatchPipeline.DispatchContext.Get<OutgoingMessage>().Headers.ContainsKey(SecondLevelRetriesBehavior.RetriesTimestamp));
        }

        [Test]
        public void ShouldSkipRetryIfNoDelayIsReturned()
        {
            var fakeDispatchPipeline = new FakeDispatchPipeline();
            var behavior = new SecondLevelRetriesBehavior(fakeDispatchPipeline, new FakePolicy(), new BusNotifications());
            behavior.Initialize(new PipelineInfo("Test", "test-address-for-this-pipeline"));
            var context = CreateContext("someid", 1);

            Assert.Throws<Exception>(() => behavior.Invoke(context, () => { throw new Exception("testex"); }));

            Assert.False(context.GetPhysicalMessage().Headers.ContainsKey(Headers.Retries));
        }
        [Test]
        public void ShouldSkipRetryForDeserializationErrors()
        {
            var fakeDispatchPipeline = new FakeDispatchPipeline();
            var behavior = new SecondLevelRetriesBehavior(fakeDispatchPipeline, new FakePolicy(TimeSpan.FromSeconds(5)), new BusNotifications());
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
            var behavior = new SecondLevelRetriesBehavior(fakeDispatchPipeline, retryPolicy, new BusNotifications());
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
            var behavior = new SecondLevelRetriesBehavior(fakeDispatchPipeline, retryPolicy, new BusNotifications());
            behavior.Initialize(new PipelineInfo("Test", "test-address-for-this-pipeline"));

            behavior.Invoke(context, () => { throw new Exception("testex"); });

            Assert.AreEqual(1, retryPolicy.InvokedWithCurrentRetry);
            Assert.AreEqual("1", fakeDispatchPipeline.DispatchContext.Get<OutgoingMessage>().Headers[Headers.Retries]);
        }


        PhysicalMessageProcessingStageBehavior.Context CreateContext(string messageId, int currentRetryCount)
        {
            var context = new PhysicalMessageProcessingStageBehavior.Context(new TransportReceiveContext(new IncomingMessage(messageId, new Dictionary<string, string> { { Headers.Retries, currentRetryCount.ToString() } }, new MemoryStream()), null));
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
        TimeSpan? delayToReturn;

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
    }
}
