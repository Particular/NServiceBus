﻿namespace NServiceBus.Core.Tests.Recoverability.SecondLevelRetries
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
    using NServiceBus.Pipeline.Contexts;
    using NServiceBus.Routing;
    using NServiceBus.Transports;
    using TransportDispatch;
    using NUnit.Framework;

    [TestFixture]
    public class SecondLevelRetriesTests
    {
        [Test]
        public async Task ShouldRetryIfPolicyReturnsADelay()
        {
            var slrNotification = new SecondLevelRetry();
            Func<SecondLevelRetry, Task> notifications = retry =>
            {
                slrNotification = retry;
                return Task.FromResult(0);
            };

            var delay = TimeSpan.FromSeconds(5);
            var pipeline = new FakeDispatchPipeline();
            var retryPolicy = new FakePolicy(delay);
            var behavior = new SecondLevelRetriesBehavior(pipeline, retryPolicy, notifications, "test-address-for-this-pipeline");

            await behavior.Invoke(CreateContext("someid", 1), () => { throw new Exception("testex"); });

            Assert.AreEqual("someid", pipeline.RoutingContext.Message.MessageId);
            Assert.AreEqual(delay, ((DelayDeliveryWith)pipeline.RoutingContext.GetDeliveryConstraints().Single(c => c is DelayDeliveryWith)).Delay);
            Assert.AreEqual("test-address-for-this-pipeline", ((UnicastAddressTag)pipeline.RoutingContext.RoutingStrategies.First().Apply(new Dictionary<string, string>())).Destination);
            Assert.AreEqual("testex", slrNotification.Exception.Message);
        }

        [Test]
        public async Task ShouldSetTimestampHeaderForFirstRetry()
        {
            var delay = TimeSpan.FromSeconds(5);
            var pipeline = new FakeDispatchPipeline();
            var retryPolicy = new FakePolicy(delay);
            var behavior = new SecondLevelRetriesBehavior(pipeline, retryPolicy, null, "MyAddress");

            await behavior.Invoke(CreateContext("someid", 0), () => { throw new Exception("testex"); });

            var headers = pipeline.RoutingContext.Message.Headers;
            Assert.True(headers.ContainsKey(SecondLevelRetriesBehavior.RetriesTimestamp));
        }

        [Test]
        public void ShouldSkipRetryIfNoDelayIsReturned()
        {
            var pipeline = new FakeDispatchPipeline();
            var retryPolicy = new FakePolicy();
            var behavior = new SecondLevelRetriesBehavior(pipeline, retryPolicy, null, "MyAddress");
            var context = CreateContext("someid", 1);

            Assert.Throws<Exception>(async () => await behavior.Invoke(context, () => { throw new Exception("testex"); }));

            Assert.False(context.Message.Headers.ContainsKey(Headers.Retries));
        }

        [Test]
        public void ShouldSkipRetryForDeserializationErrors()
        {
            var pipeline = new FakeDispatchPipeline();
            var retryPolicy = new FakePolicy(TimeSpan.FromSeconds(5));
            var behavior = new SecondLevelRetriesBehavior(pipeline, retryPolicy, null, "MyAddress");
            var context = CreateContext("someid", 1);

            Assert.Throws<MessageDeserializationException>(async () => await behavior.Invoke(context, () => { throw new MessageDeserializationException("testex"); }));
            Assert.False(context.Message.Headers.ContainsKey(Headers.Retries));
        }

        [Test]
        public async Task ShouldPullCurrentRetryCountFromHeaders()
        {
            var retryPolicy = new FakePolicy(TimeSpan.FromSeconds(5));

            var pipeline = new FakeDispatchPipeline();
            var behavior = new SecondLevelRetriesBehavior(pipeline, retryPolicy, null, "MyAddress");

            var currentRetry = 3;

            await behavior.Invoke(CreateContext("someid", currentRetry), () => { throw new Exception("testex"); });

            Assert.AreEqual(currentRetry + 1, retryPolicy.InvokedWithCurrentRetry);
        }

        [Test]
        public async Task ShouldDefaultRetryCountToZeroIfNoHeaderIsFound()
        {
            var retryPolicy = new FakePolicy(TimeSpan.FromSeconds(5));
            var context = CreateContext("someid", 2);

            context.Message.Headers.Clear();

            var pipeline = new FakeDispatchPipeline();
            var behavior = new SecondLevelRetriesBehavior(pipeline, retryPolicy, null, "MyAddress");

            await behavior.Invoke(context, () => { throw new Exception("testex"); });

            Assert.AreEqual(1, retryPolicy.InvokedWithCurrentRetry);
            Assert.AreEqual("1", pipeline.RoutingContext.Message.Headers[Headers.Retries]);
        }

        [Test]
        public async Task ShouldRevertMessageBodyWhenDispatchingMessage()
        {
            const string originalContent = "original content";
            var context = CreateContext("someId", 1, Encoding.UTF8.GetBytes(originalContent));
            var pipeline = new FakeDispatchPipeline();
            var retryPolicy = new FakePolicy(TimeSpan.FromSeconds(0));
            var behavior = new SecondLevelRetriesBehavior(pipeline, retryPolicy, null, "test-address-for-this-pipeline");

            var message = context.Message;
            message.Body = Encoding.UTF8.GetBytes("modified content");

            await behavior.Invoke(context, () => { throw new Exception("test"); });

            var dispatchedMessage = pipeline.RoutingContext.Message;
            Assert.AreEqual(originalContent, Encoding.UTF8.GetString(dispatchedMessage.Body));
            Assert.AreEqual(originalContent, Encoding.UTF8.GetString(message.Body));
        }

        ITransportReceiveContext CreateContext(string messageId, int currentRetryCount, byte[] messageBody = null)
        {
            return new TransportReceiveContext(new IncomingMessage(messageId, new Dictionary<string, string>
            {
                {Headers.Retries, currentRetryCount.ToString()}
            }, new MemoryStream(messageBody ?? new byte[0])), null, new RootContext(null));
        }
    }

    class FakeDispatchPipeline : IPipelineBase<IRoutingContext>
    {
        public IRoutingContext RoutingContext { get; set; }

        public Task Invoke(IRoutingContext context)
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