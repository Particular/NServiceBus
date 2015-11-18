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
    using Faults;
    using NServiceBus.Core.Tests.Features;
    using NServiceBus.Hosting;
    using NServiceBus.Pipeline;
    using NServiceBus.Pipeline.Contexts;
    using NServiceBus.Recoverability.FirstLevelRetries;
    using NServiceBus.Recoverability.SecondLevelRetries;
    using NServiceBus.Routing;
    using TransportDispatch;
    using Transports;
    using Unicast.Transport;
    using NUnit.Framework;

    [TestFixture]
    public class SecondLevelRetriesTests
    {
        FakeDispatchPipeline pipeline;

        [SetUp]
        private void Setup()
        {
            pipeline = new FakeDispatchPipeline();
        }

        private RecoverabilityBehavior Create(SecondLevelRetryPolicy retryPolicy, string address)
        {
            var notifications = new BusNotifications();

            var slrHandler = new SecondLevelRetriesHandler(pipeline, retryPolicy, notifications, address);
            var flrHandler = new FirstLevelRetriesHandler(new FlrStatusStorage(), new FirstLevelRetryPolicy(0), notifications);

            var bahavior = new RecoverabilityBehavior(
                new FakeCriticalError(), 
                pipeline,
                new HostInformation(Guid.NewGuid(), "my host"), 
                notifications,
                "errors",
                flrHandler,
                slrHandler);

            return bahavior;
        }

        [Test]
        public async Task ShouldRetryIfPolicyReturnsADelay()
        {
            var notifications = new BusNotifications();

            var delay = TimeSpan.FromSeconds(5);
            var behavior = Create(new FakePolicy(delay), "test-address-for-this-pipeline");
                
            behavior.Initialize(new PipelineInfo("Test", "IncomingQueueForThisPipeline"));

            var slrNotification = new SecondLevelRetry();

            notifications.Errors.MessageHasBeenSentToSecondLevelRetries.Subscribe(slr => { slrNotification = slr; });

            await SimulateFailingExecution(behavior, CreateContext("someid", 1));

            Assert.AreEqual("someid", pipeline.RoutingContext.Message.MessageId);
            Assert.AreEqual(delay, ((DelayDeliveryWith)pipeline.RoutingContext.GetDeliveryConstraints().Single(c => c is DelayDeliveryWith)).Delay);
            Assert.AreEqual("test-address-for-this-pipeline", ((UnicastAddressTag)pipeline.RoutingContext.RoutingStrategies.First().Apply(new Dictionary<string, string>())).Destination);
            Assert.AreEqual("testex", slrNotification.Exception.Message);
        }

        [Test]
        public async Task ShouldSetTimestampHeaderForFirstRetry()
        {
            var delay = TimeSpan.FromSeconds(5);
            var behavior = Create(new FakePolicy(delay), "MyAddress");
            behavior.Initialize(new PipelineInfo("Test", "test-address-for-this-pipeline"));

            await SimulateFailingExecution(behavior, CreateContext("someid", 0));

            Assert.True(pipeline.RoutingContext.Message.Headers.ContainsKey(SecondLevelRetriesHandler.RetriesTimestamp));
        }

        [Test]
        public void ShouldSkipRetryIfNoDelayIsReturned()
        {
            var behavior = Create(new FakePolicy(), "MyAddress");
            behavior.Initialize(new PipelineInfo("Test", "test-address-for-this-pipeline"));

            var context = CreateContext("someid", 1);

            Assert.Throws<Exception>(async () => await SimulateFailingExecution(behavior, context));
            Assert.False(context.Message.Headers.ContainsKey(Headers.Retries));
        }

        [Test]
        public void ShouldSkipRetryForDeserializationErrors()
        {
            var behavior = Create(new FakePolicy(TimeSpan.FromSeconds(5)), "MyAddress");
            behavior.Initialize(new PipelineInfo("Test", "test-address-for-this-pipeline"));

            var context = CreateContext("someid", 1);

            Assert.Throws<MessageDeserializationException>(async () => await behavior.Invoke(context, () => { throw new MessageDeserializationException("testex"); }));
            Assert.False(context.Message.Headers.ContainsKey(Headers.Retries));
        }

        [Test]
        public async Task ShouldPullCurrentRetryCountFromHeaders()
        {
            var retryPolicy = new FakePolicy(TimeSpan.FromSeconds(5));

            var behavior = Create(retryPolicy, "MyAddress");
            behavior.Initialize(new PipelineInfo("Test", "test-address-for-this-pipeline"));

            var currentRetry = 3;
            
            await SimulateFailingExecution(behavior, CreateContext("someid", currentRetry));

            Assert.AreEqual(currentRetry + 1, retryPolicy.InvokedWithCurrentRetry);
        }

        [Test]
        public async Task ShouldDefaultRetryCountToZeroIfNoHeaderIsFound()
        {
            var retryPolicy = new FakePolicy(TimeSpan.FromSeconds(5));
            var context = CreateContext("someid", 2);

            context.Message.Headers.Clear();

            var behavior = Create(retryPolicy, "MyAddress");
            behavior.Initialize(new PipelineInfo("Test", "test-address-for-this-pipeline"));

            await SimulateFailingExecution(behavior, context);

            Assert.AreEqual(1, retryPolicy.InvokedWithCurrentRetry);
            Assert.AreEqual("1", pipeline.RoutingContext.Message.Headers[Headers.Retries]);
        }

        [Test]
        public async Task ShouldRevertMessageBodyWhenDispatchingMessage()
        {
            const string originalContent = "original content";
            var context = CreateContext("someId", 1, Encoding.UTF8.GetBytes(originalContent));
            var retryPolicy = new FakePolicy(TimeSpan.FromSeconds(0));
            var behavior = Create(retryPolicy, "test-address-for-this-pipeline");
            behavior.Initialize(new PipelineInfo("Test", "test-address-for-this-pipeline"));

            var message = context.Message;
            message.Body = Encoding.UTF8.GetBytes("modified content");

            await SimulateFailingExecution(behavior, context);

            var dispatchedMessage = pipeline.RoutingContext.Message;
            Assert.AreEqual(originalContent, Encoding.UTF8.GetString(dispatchedMessage.Body));
            Assert.AreEqual(originalContent, Encoding.UTF8.GetString(message.Body));
        }

        static async Task SimulateFailingExecution(RecoverabilityBehavior behavior, TransportReceiveContext context)
        {
            try
            {
                await behavior.Invoke(context, () => { throw new Exception("testex"); });
            }
            catch (MessageProcessingAbortedException)
            {
            }

            await behavior.Invoke(context, () => Task.FromResult(0));
        }

        TransportReceiveContext CreateContext(string messageId, int currentRetryCount, byte[] messageBody = null)
        {
            return new TransportReceiveContext(new IncomingMessage(messageId, new Dictionary<string, string>
            {
                {Headers.Retries, currentRetryCount.ToString()}
            }, new MemoryStream(messageBody ?? new byte[0])), new RootContext(null));
        }
    }

    class FakeCriticalError : CriticalError
    {
        public FakeCriticalError()
            : base((s, e) => { }, new FakeBuilder())
        {
        }

        public override void Raise(string errorMessage, Exception exception)
        {
            ErrorRaised = true;
        }

        public bool ErrorRaised { get; private set; }
    }

    class FakeDispatchPipeline : IPipelineBase<RoutingContext>
    {
        public RoutingContext RoutingContext { get; set; }

        public Task Invoke(RoutingContext context)
        {
            RoutingContext = context;
            return Task.FromResult(0);
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