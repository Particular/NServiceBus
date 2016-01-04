namespace NServiceBus.Core.Tests.Recoverability.FirstLevelRetries
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Threading.Tasks;
    using NServiceBus.Faults;
    using NServiceBus.Pipeline.Contexts;
    using NServiceBus.Transports;
    using NUnit.Framework;

    [TestFixture]
    public class FirstLevelRetriesTests
    {
        [Test]
        public void ShouldNotPerformFLROnMessagesThatCantBeDeserialized()
        {
            var storage = new FlrStatusStorage();
            var retryPolicy = new FirstLevelRetryPolicy(0);
            var behavior = new FirstLevelRetriesBehavior(storage, retryPolicy, null, "");
            Assert.Throws<MessageDeserializationException>(async () => await behavior.Invoke(null, () =>
            {
                throw new MessageDeserializationException("test");
            }));
        }

        [Test]
        public void ShouldPerformFLRIfThereAreRetriesLeftToDo()
        {
            var retryPolicy = new FirstLevelRetryPolicy(1);
            var storage = new FlrStatusStorage();
            var behavior = new FirstLevelRetriesBehavior(storage, retryPolicy, null, "");
            var context = CreateContext("someid");

            Assert.Throws<MessageProcessingAbortedException>(async () => await behavior.Invoke(context, () =>
            {
                throw new Exception("test");
            }));
        }

        [Test]
        public void ShouldBubbleTheExceptionUpIfThereAreNoMoreRetriesLeft()
        {
            var storage = new FlrStatusStorage();
            var retryPolicy = new FirstLevelRetryPolicy(0);
            var behavior = new FirstLevelRetriesBehavior(storage, retryPolicy, null, "");
            var context = CreateContext("someid");

            Assert.Throws<Exception>(async () => await behavior.Invoke(context, () =>
            {
                throw new Exception("test");
            }));

            //should set the retries header to capture how many flr attempts where made
            Assert.AreEqual("0", context.Message.Headers[Headers.FLRetries]);
        }

        [Test]
        public void ShouldClearStorageAfterGivingUp()
        {
            const string messageId = "someid";
            var storage = new FlrStatusStorage();
            var retryPolicy = new FirstLevelRetryPolicy(1);
            var behavior = new FirstLevelRetriesBehavior(storage, retryPolicy, null, "");

            storage.IncrementFailuresForMessage(messageId);

            Assert.Throws<Exception>(async () => await behavior.Invoke(CreateContext(messageId), () =>
            {
                throw new Exception("test");
            }));

            Assert.AreEqual(0, storage.GetFailuresForMessage(messageId));
        }

        [Test]
        public void ShouldRememberRetryCountBetweenRetries()
        {
            const string messageId = "someid";
            var storage = new FlrStatusStorage();
            var retryPolicy = new FirstLevelRetryPolicy(1);
            var behavior = new FirstLevelRetriesBehavior(storage, retryPolicy, null, "");

            Assert.Throws<MessageProcessingAbortedException>(async ()=> await behavior.Invoke(CreateContext(messageId), () =>
            {
                throw new Exception("test");
            }));

            Assert.AreEqual(1, storage.GetFailuresForMessage(messageId));
        }

        [Test]
        public void ShouldRaiseBusNotificationsForFLR()
        {
            var notificationFired = false;
            Func<FirstLevelRetry, Task> notifications = retry =>
            {
                Assert.AreEqual(0, retry.RetryAttempt);
                Assert.AreEqual("test", retry.Exception.Message);
                Assert.AreEqual("someid", retry.MessageId);
                notificationFired = true;
                return Task.FromResult(0);
            }; 
            var storage = new FlrStatusStorage();
            var retryPolicy = new FirstLevelRetryPolicy(1);
            var behavior = new FirstLevelRetriesBehavior(storage, retryPolicy, notifications, "");

            Assert.Throws<MessageProcessingAbortedException>(async () => await behavior.Invoke(CreateContext("someid"), () =>
            {
                throw new Exception("test");
            }));

            Assert.True(notificationFired);
        }

        [Test]
        public void WillResetRetryCounterWhenFlrStorageCleared()
        {
            const string messageId = "someId";
            var storage = new FlrStatusStorage();
            var retryPolicy = new FirstLevelRetryPolicy(1);
            var behavior = new FirstLevelRetriesBehavior(storage, retryPolicy, null, "");

            Assert.Throws<MessageProcessingAbortedException>(async () => await behavior.Invoke(CreateContext(messageId), () =>
            {
                throw new Exception("test");
            }));

            storage.ClearAllFailures();

            Assert.Throws<MessageProcessingAbortedException>(async () => await behavior.Invoke(CreateContext(messageId), () =>
            {
                throw new Exception("test");
            }));
        }

        [Test]
        public void ShouldTrackRetriesForEachPipelineIndependently()
        {
            const string messageId = "someId";
            var storage = new FlrStatusStorage();
            var retryPolicy1 = new FirstLevelRetryPolicy(1);
            var behavior1 = new FirstLevelRetriesBehavior(storage, retryPolicy1, null, "1");
            var retryPolicy2 = new FirstLevelRetryPolicy(2);
            var behavior2 = new FirstLevelRetriesBehavior(storage, retryPolicy2, null,"2");

            Assert.Throws<MessageProcessingAbortedException>(async () => await behavior1.Invoke(CreateContext(messageId), () =>
            {
                throw new Exception("test");
            }));

            Assert.Throws<MessageProcessingAbortedException>(async () => await behavior2.Invoke(CreateContext(messageId), () =>
            {
                throw new Exception("test");
            }));

            Assert.Throws<Exception>(async () => await behavior1.Invoke(CreateContext(messageId), () =>
            {
                throw new Exception("test");
            }));

            Assert.Throws<MessageProcessingAbortedException>(async () => await behavior2.Invoke(CreateContext(messageId), () =>
            {
                throw new Exception("test");
            }));
        }

        ITransportReceiveContext CreateContext(string messageId)
        {
            return new TransportReceiveContext(new IncomingMessage(messageId, new Dictionary<string, string>(), new MemoryStream()), null, new RootContext(null));
        }
    }
}