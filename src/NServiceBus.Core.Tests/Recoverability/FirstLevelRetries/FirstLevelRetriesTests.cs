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
        public async Task ShouldNotPerformFLROnMessagesThatCantBeDeserialized()
        {
            var behavior = CreateFlrBehavior(new FirstLevelRetryPolicy(0));
            var context = CreateContext("someid");

            var failureHandeled = await behavior.HandleMessageFailure(context, new MessageDeserializationException("test"));

            Assert.IsFalse(
                failureHandeled, 
                "MessageDeserializationException should cause message to be treated as poisonous and never handled"
            );
        }

        [Test]
        public async Task ShouldPerformFLRIfThereAreRetriesLeftToDo()
        {
            var behavior = CreateFlrBehavior(new FirstLevelRetryPolicy(1));
            var context = CreateContext("someid");

            await behavior.HandleMessageFailure(context, new Exception("test")).ConfigureAwait(false);

            Assert.True(context.ReceiveOperationAborted, "Should request the transport to abort");
        }

        [Test]
        public async Task ShouldBubbleTheExceptionUpIfThereAreNoMoreRetriesLeft()
        {
            var storage = GetFailureInfoStorage();
            var behavior = CreateFlrBehavior(new FirstLevelRetryPolicy(0), storage);
            var context = CreateContext("someid");

            var failureHandeled = await behavior.HandleMessageFailure(context, new Exception());

            Assert.IsFalse(failureHandeled, "FLR should give up when max retries has been reached.");
            //should update the failure info storage to capture how many flr attempts where made
            Assert.AreEqual(0, storage.GetFailureInfoForMessage("someid").FLRetries);
        }

        [Test]
        public async Task ShouldNotClearStorageAfterGivingUp()
        {
            const string messageId = "someid";
            var storage = GetFailureInfoStorage();
            var behavior = CreateFlrBehavior(new FirstLevelRetryPolicy(1), storage);

            storage.RecordFirstLevelRetryAttempt(messageId, ExceptionDispatchInfo.Capture(new Exception()));

            var failureHandeled = await behavior.HandleMessageFailure(CreateContext(messageId), new Exception());

            Assert.IsFalse(failureHandeled, "FLR should give-up after reaching max retries");
            Assert.AreEqual(1, storage.GetFailureInfoForMessage(messageId).FLRetries);
        }

        [Test]
        public async Task ShouldRememberRetryCountBetweenRetries()
        {
            const string messageId = "someid";
            var storage = GetFailureInfoStorage();
            var behavior = CreateFlrBehavior(new FirstLevelRetryPolicy(1), storage);

            await behavior.HandleMessageFailure(CreateContext(messageId), new Exception());

            Assert.AreEqual(1, storage.GetFailureInfoForMessage(messageId).FLRetries);
        }

        [Test]
        public async Task ShouldRaiseNotificationsForFLR()
        {
            var behavior = CreateFlrBehavior(new FirstLevelRetryPolicy(1));
            var eventAggregator = new FakeEventAggregator();

            var context = CreateContext("someid", eventAggregator);

            await behavior.HandleMessageFailure(context, new Exception("test"));

            var failure = eventAggregator.GetNotification<MessageToBeRetried>();

            Assert.AreEqual(0, failure.Attempt);
            Assert.AreEqual("test", failure.Exception.Message);
            Assert.AreEqual("someid", failure.Message.MessageId);
        }

        [Test]
        public async Task WillResetRetryCounterWhenFlrStorageCleared()
        {
            const string messageId = "someId";
            var storage = GetFailureInfoStorage();
            var behavior = CreateFlrBehavior(new FirstLevelRetryPolicy(1), storage);

            await behavior.HandleMessageFailure(CreateContext(messageId), new Exception("test"));

            storage.ClearFailureInfoForMessage(messageId);

            var failureHandeled = await behavior.HandleMessageFailure(CreateContext(messageId), new Exception());

            Assert.IsTrue(failureHandeled, "If storage contains no previous failures for the mesage, it should be handled by FLR");
        }
        
        static FirstLevelRetriesHandler CreateFlrBehavior(FirstLevelRetryPolicy retryPolicy, FailureInfoStorage storage = null)
        {
            var flrBehavior = new FirstLevelRetriesHandler(
                storage ?? GetFailureInfoStorage(),
                retryPolicy);

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