namespace NServiceBus.Core.Tests.Recoverability.FirstLevelRetries
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Runtime.ExceptionServices;
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

            var shouldImmediatelyRetry = behavior.Invoke(new MessageDeserializationException(string.Empty), 1, string.Empty);

            Assert.IsFalse(shouldImmediatelyRetry);
        }

        [Test]
        public void ShouldPerformFLRIfThereAreRetriesLeftToDo()
        {
            var behavior = CreateFlrBehavior(new FirstLevelRetryPolicy(1));

            var shouldImmediatelyRetry = behavior.Invoke(new Exception(), 0, string.Empty);

            Assert.True(shouldImmediatelyRetry);
        }

        [Test]
        public void ShouldGiveUpIfThereAreNoMoreRetriesLeft()
        {
            var storage = GetFailureInfoStorage();
            var behavior = CreateFlrBehavior(new FirstLevelRetryPolicy(0), storage);

            var shouldImmediatelyRetry = behavior.Invoke(new Exception(), 0, string.Empty);

            Assert.True(shouldImmediatelyRetry);

            //should update the failure info storage to capture how many flr attempts where made
            Assert.AreEqual(0, storage.GetFailureInfoForMessage("someid").FLRetries);
        }

        /* This should probably go to transport level test
        [Test]
        public void ShouldNotClearStorageAfterGivingUp()
        {
            const string messageId = "someid";
            var storage = GetFailureInfoStorage();
            var behavior = CreateFlrBehavior(new FirstLevelRetryPolicy(1), storage);

            storage.RecordFirstLevelRetryAttempt(messageId, ExceptionDispatchInfo.Capture(new Exception("test")));

            Assert.That(async () => await behavior.Invoke(CreateContext(messageId), () => { throw new Exception("test"); }), Throws.InstanceOf<Exception>());

            Assert.AreEqual(1, storage.GetFailureInfoForMessage(messageId).FLRetries);
        }
        

        [Test]
        public async Task ShouldRememberRetryCountBetweenRetries()
        {
            const string messageId = "someid";
            var storage = GetFailureInfoStorage();
            var behavior = CreateFlrBehavior(new FirstLevelRetryPolicy(1), storage);

            await behavior.Invoke(CreateContext(messageId), () => { throw new Exception("test"); });

            Assert.AreEqual(1, storage.GetFailureInfoForMessage(messageId).FLRetries);
        }

        [Test]
        public async Task WillResetRetryCounterWhenFlrStorageCleared()
        {
            const string messageId = "someId";
            var storage = GetFailureInfoStorage();
            var behavior = CreateFlrBehavior(new FirstLevelRetryPolicy(1), storage);

            await behavior.Invoke(CreateContext(messageId), () => { throw new Exception("test"); });

            storage.ClearFailureInfoForMessage(messageId);

            Assert.DoesNotThrowAsync(async () => await behavior.Invoke(CreateContext(messageId), () => { throw new Exception("test"); }));
        }
        */

        [Test]
        public void ShouldRaiseNotificationsForFLR()
        {
            var behavior = CreateFlrBehavior(new FirstLevelRetryPolicy(1));
            var eventAggregator = new FakeEventAggregator();

            behavior.Invoke(new Exception(), 0, "someId");

            var failure = eventAggregator.GetNotification<MessageToBeRetried>();

            Assert.AreEqual(0, failure.Attempt);
            Assert.AreEqual("test", failure.Exception.Message);
            Assert.AreEqual("someid", failure.Message.MessageId);
        }
        
        static FirstLevelRetriesBehavior CreateFlrBehavior(FirstLevelRetryPolicy retryPolicy, FailureInfoStorage storage = null)
        {
            var flrBehavior = new FirstLevelRetriesBehavior(retryPolicy);

            return flrBehavior;
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

        static FailureInfoStorage GetFailureInfoStorage()
        {
            return new FailureInfoStorage(10);
        }
    }
}