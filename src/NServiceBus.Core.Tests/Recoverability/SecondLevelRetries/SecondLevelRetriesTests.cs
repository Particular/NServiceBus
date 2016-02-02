namespace NServiceBus.Core.Tests.Recoverability.SecondLevelRetries
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using DelayedDelivery;
    using DeliveryConstraints;
    using Faults;
    using NServiceBus.Pipeline;
    using NServiceBus.Routing;
    using NServiceBus.Transports;
    using NUnit.Framework;

    [TestFixture]
    public class SecondLevelRetriesTests
    {
        [Test]
        public async Task ShouldRetryIfPolicyReturnsADelay()
        {
            var notifications = new BusNotifications();

            var delay = TimeSpan.FromSeconds(5);
            var fakeDispatchPipeline = new FakeDispatchPipeline();
            var behavior = new SecondLevelRetriesBehavior(new FakePolicy(delay), notifications, "test-address-for-this-pipeline");

            var slrNotification = new SecondLevelRetry();

            notifications.Errors.MessageHasBeenSentToSecondLevelRetries += (sender, retry) => slrNotification = retry;

            await behavior.Invoke(CreateContext("someid", 1, fakeDispatchPipeline), () => { throw new Exception("testex"); });

            Assert.AreEqual("someid", fakeDispatchPipeline.RoutingContext.Message.MessageId);
            Assert.AreEqual(delay, ((DelayDeliveryWith)fakeDispatchPipeline.RoutingContext.Extensions.GetDeliveryConstraints().Single(c => c is DelayDeliveryWith)).Delay);
            Assert.AreEqual("test-address-for-this-pipeline", ((UnicastAddressTag)fakeDispatchPipeline.RoutingContext.RoutingStrategies.First().Apply(new Dictionary<string, string>())).Destination);
            Assert.AreEqual("testex", slrNotification.Exception.Message);
        }

        [Test]
        public async Task ShouldSetTimestampHeaderForFirstRetry()
        {
            var delay = TimeSpan.FromSeconds(5);
            var fakeDispatchPipeline = new FakeDispatchPipeline();
            var behavior = new SecondLevelRetriesBehavior(new FakePolicy(delay), new BusNotifications(), "MyAddress");

            await behavior.Invoke(CreateContext("someid", 0, fakeDispatchPipeline), () => { throw new Exception("testex"); });

            Assert.True(fakeDispatchPipeline.RoutingContext.Message.Headers.ContainsKey(SecondLevelRetriesBehavior.RetriesTimestamp));
        }

        [Test]
        public void ShouldSkipRetryIfNoDelayIsReturned()
        {
            var fakeDispatchPipeline = new FakeDispatchPipeline();
            var behavior = new SecondLevelRetriesBehavior(new FakePolicy(), new BusNotifications(), "MyAddress");
            var context = CreateContext("someid", 1, fakeDispatchPipeline);

            Assert.Throws<Exception>(async () => await behavior.Invoke(context, () => { throw new Exception("testex"); }));

            Assert.False(context.Message.Headers.ContainsKey(Headers.Retries));
        }

        [Test]
        public void ShouldSkipRetryForDeserializationErrors()
        {
            var fakeDispatchPipeline = new FakeDispatchPipeline();
            var behavior = new SecondLevelRetriesBehavior(new FakePolicy(TimeSpan.FromSeconds(5)), new BusNotifications(), "MyAddress");
            var context = CreateContext("someid", 1, fakeDispatchPipeline);

            Assert.Throws<MessageDeserializationException>(async () => await behavior.Invoke(context, () => { throw new MessageDeserializationException("testex"); }));
            Assert.False(context.Message.Headers.ContainsKey(Headers.Retries));
        }

        [Test]
        public async Task ShouldPullCurrentRetryCountFromHeaders()
        {
            var retryPolicy = new FakePolicy(TimeSpan.FromSeconds(5));

            var fakeDispatchPipeline = new FakeDispatchPipeline();
            var behavior = new SecondLevelRetriesBehavior(retryPolicy, new BusNotifications(), "MyAddress");

            var currentRetry = 3;

            await behavior.Invoke(CreateContext("someid", currentRetry, fakeDispatchPipeline), () => { throw new Exception("testex"); });

            Assert.AreEqual(currentRetry + 1, retryPolicy.InvokedWithCurrentRetry);
        }

        [Test]
        public async Task ShouldDefaultRetryCountToZeroIfNoHeaderIsFound()
        {
            var retryPolicy = new FakePolicy(TimeSpan.FromSeconds(5));
            var fakeDispatchPipeline = new FakeDispatchPipeline();
            var context = CreateContext("someid", 2, fakeDispatchPipeline);

            context.Message.Headers.Clear();

            var behavior = new SecondLevelRetriesBehavior(retryPolicy, new BusNotifications(), "MyAddress");

            await behavior.Invoke(context, () => { throw new Exception("testex"); });

            Assert.AreEqual(1, retryPolicy.InvokedWithCurrentRetry);
            Assert.AreEqual("1", fakeDispatchPipeline.RoutingContext.Message.Headers[Headers.Retries]);
        }

        [Test]
        public async Task ShouldRevertMessageBodyWhenDispatchingMessage()
        {
            const string originalContent = "original content";
            var fakeDispatchPipeline = new FakeDispatchPipeline();
            var context = CreateContext("someId", 1, fakeDispatchPipeline, Encoding.UTF8.GetBytes(originalContent));
            var retryPolicy = new FakePolicy(TimeSpan.FromSeconds(0));
            var behavior = new SecondLevelRetriesBehavior(retryPolicy, new BusNotifications(), "test-address-for-this-pipeline");

            var message = context.Message;
            message.Body = Encoding.UTF8.GetBytes("modified content");

            await behavior.Invoke(context, () => { throw new Exception("test"); });

            var dispatchedMessage = fakeDispatchPipeline.RoutingContext.Message;
            Assert.AreEqual(originalContent, Encoding.UTF8.GetString(dispatchedMessage.Body));
            Assert.AreEqual(originalContent, Encoding.UTF8.GetString(message.Body));
        }

        ITransportReceiveContext CreateContext(string messageId, int currentRetryCount, FakeDispatchPipeline pipeline, byte[] messageBody = null)
        {
            return new TransportReceiveContext(new IncomingMessage(messageId, new Dictionary<string, string>
            {
                {Headers.Retries, currentRetryCount.ToString()}
            }, new MemoryStream(messageBody ?? new byte[0])), null, new CancellationTokenSource(),  new RootContext(null, new FakePipelineCache(pipeline)));
        }
    }

    class FakePipelineCache : IPipelineCache
    {
        IPipeline<IRoutingContext> pipeline;

        public FakePipelineCache(IPipeline<IRoutingContext> pipeline)
        {
            this.pipeline = pipeline;
        }

        public IPipeline<TContext> Pipeline<TContext>()
            where TContext : IBehaviorContext

        {
            return (IPipeline<TContext>) pipeline;
        }
    }

    class FakeDispatchPipeline : IPipeline<IRoutingContext>
    {
        public IRoutingContext RoutingContext { get; set; }

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