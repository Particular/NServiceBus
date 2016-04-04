namespace NServiceBus.Core.Tests.Recoverability.FirstLevelRetries
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Threading.Tasks;
    using NServiceBus.Transports;
    using NUnit.Framework;
    using Testing;

    [TestFixture]
    public class FirstLevelRetriesTests
    {
        [Test]
        public void ShouldNotPerformFLROnMessagesThatCantBeDeserialized()
        {
            var behavior = CreateFlrBehavior(new FirstLevelRetryPolicy(0));

            Assert.That(async () => await behavior.Invoke(CreateContext(), () => { throw new MessageDeserializationException("test"); }), Throws.InstanceOf<MessageDeserializationException>());
        }

        [Test]
        public async Task ShouldPerformFLRIfThereAreRetriesLeftToDo()
        {
            var behavior = CreateFlrBehavior(new FirstLevelRetryPolicy(1));
            var context = CreateContext();

            await behavior.Invoke(context, () => { throw new Exception("test"); });

            Assert.True(context.ReceiveOperationAborted, "Should request the transport to abort");
        }

        [Test]
        public void ShouldBubbleTheExceptionUpIfThereAreNoMoreRetriesLeft()
        {
            var behavior = CreateFlrBehavior(new FirstLevelRetryPolicy(0));
            var context = CreateContext();

            Assert.That(async () => await behavior.Invoke(context, () => { throw new Exception("test"); }), Throws.InstanceOf<Exception>());

            //should set the retries header to capture how many flr attempts where made
            Assert.AreEqual("0", context.Message.Headers[Headers.FLRetries]);
        }

        [Test]
        public void ShouldClearStorageAfterGivingUp()
        {
            const string messageId = "someid";
            var storage = new FlrStatusStorage();
            var behavior = CreateFlrBehavior(new FirstLevelRetryPolicy(1), storage);
            var transportReceiveContext = CreateContext(messageId);

            storage.IncrementFailuresForMessage(messageId);

            Assert.That(async () => await behavior.Invoke(transportReceiveContext, () => { throw new Exception("test"); }), Throws.InstanceOf<Exception>());

            Assert.AreEqual(0, storage.GetFailuresForMessage(messageId));
        }

        [Test]
        public async Task ShouldRememberRetryCountBetweenRetries()
        {
            const string messageId = "someid";
            var storage = new FlrStatusStorage();
            var behavior = CreateFlrBehavior(new FirstLevelRetryPolicy(1), storage);
            var transportReceiveContext = CreateContext(messageId);

            await behavior.Invoke(transportReceiveContext, () => { throw new Exception("test"); });

            Assert.AreEqual(1, storage.GetFailuresForMessage(messageId));
        }

        [Test]
        public async Task ShouldRaiseNotificationsForFLR()
        {
            var behavior = CreateFlrBehavior(new FirstLevelRetryPolicy(1));
            var eventAggregator = new FakeEventAggregator();

            var context = CreateContext("someid", eventAggregator);

            await behavior.Invoke(context, () => { throw new Exception("test"); });

            var failure = eventAggregator.GetNotification<MessageToBeRetried>();

            Assert.AreEqual(0, failure.Attempt);
            Assert.AreEqual("test", failure.Exception.Message);
            Assert.AreEqual("someid", failure.Message.MessageId);
        }

        [Test]
        public async Task WillResetRetryCounterWhenFlrStorageCleared()
        {
            const string messageId = "someId";
            var storage = new FlrStatusStorage();
            var behavior = CreateFlrBehavior(new FirstLevelRetryPolicy(1), storage);

            await behavior.Invoke(CreateContext(messageId), () => { throw new Exception("test"); });

            storage.ClearAllFailures();

            await behavior.Invoke(CreateContext(messageId), () => { throw new Exception("test"); });
        }

        static TestableTransportReceiveContext CreateContext(string messageId = null, FakeEventAggregator eventAggregator = null)
        {
            var context = new TestableTransportReceiveContext();

            context.Extensions.Set<IEventAggregator>(eventAggregator ?? new FakeEventAggregator());

            if (messageId != null)
            {
                context.Message = new IncomingMessage(messageId, new Dictionary<string, string>(), Stream.Null);
            }

            return context;
        }

        static FirstLevelRetriesBehavior CreateFlrBehavior(FirstLevelRetryPolicy retryPolicy, FlrStatusStorage storage = null)
        {
            var flrBehavior = new FirstLevelRetriesBehavior(
                storage ?? new FlrStatusStorage(),
                retryPolicy);

            return flrBehavior;
        }
    }
}