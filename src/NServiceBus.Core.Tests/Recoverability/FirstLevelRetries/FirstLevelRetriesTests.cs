namespace NServiceBus.Core.Tests.Recoverability.FirstLevelRetries
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using NServiceBus.Pipeline.Contexts;
    using NServiceBus.Transports;
    using Unicast.Transport;
    using NUnit.Framework;

    [TestFixture]
    public class FirstLevelRetriesTests
    {
        [Test]
        public void ShouldNotPerformFLROnMessagesThatCantBeDeserialized()
        {
            var behavior = CreateFlrBehavior(new FirstLevelRetryPolicy(0));

            Assert.Throws<MessageDeserializationException>(async () => await behavior.Invoke(null, () =>
            {
                throw new MessageDeserializationException("test");
            }));
        }

        [Test]
        public void ShouldPerformFLRIfThereAreRetriesLeftToDo()
        {
            var behavior = CreateFlrBehavior(new FirstLevelRetryPolicy(1));
            var context = CreateContext("someid");

            Assert.Throws<MessageProcessingAbortedException>(async () => await behavior.Invoke(context, () =>
            {
                throw new Exception("test");
            }));
 }

        [Test]
        public void ShouldBubbleTheExceptionUpIfThereAreNoMoreRetriesLeft()
        {
            var behavior = CreateFlrBehavior(new FirstLevelRetryPolicy(0));
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
            var pipeline = new PipelineInfo("somePipeline", "someAddress");
            var behavior = CreateFlrBehavior(new FirstLevelRetryPolicy(1), storage, pipelineInfo: pipeline);

            storage.IncrementFailuresForMessage(pipeline.Name + messageId);

            Assert.Throws<Exception>(async () => await behavior.Invoke(CreateContext(messageId), () =>
            {
                throw new Exception("test");
            }));

            Assert.AreEqual(0, storage.GetFailuresForMessage(pipeline.Name + messageId));
        }

        [Test]
        public void ShouldRememberRetryCountBetweenRetries()
        {
            const string messageId = "someid";
            var storage = new FlrStatusStorage();
            var pipeline = new PipelineInfo("anotherPipeline", "anotherAddress");
            var behavior = CreateFlrBehavior(new FirstLevelRetryPolicy(1), storage, pipelineInfo: pipeline);

            Assert.Throws<MessageProcessingAbortedException>(async ()=> await behavior.Invoke(CreateContext(messageId), () =>
            {
                throw new Exception("test");
            }));

            Assert.AreEqual(1, storage.GetFailuresForMessage(pipeline.Name + messageId));
        }

        [Test]
        public void ShouldRaiseBusNotificationsForFLR()
        {
            var notifications = new BusNotifications();
            var behavior = CreateFlrBehavior(new FirstLevelRetryPolicy(1), busNotifications: notifications);

            var notificationFired = false;

            notifications.Errors.MessageHasFailedAFirstLevelRetryAttempt += (sender, retry) =>
            {
                Assert.AreEqual(0, retry.RetryAttempt);
                Assert.AreEqual("test", retry.Exception.Message);
                Assert.AreEqual("someid", retry.MessageId);

                notificationFired = true;
            };

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
            var behavior = CreateFlrBehavior(new FirstLevelRetryPolicy(1), storage);

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
            var pipeline1 = new PipelineInfo("pipeline1", "address");
            var behavior1 = CreateFlrBehavior(new FirstLevelRetryPolicy(1), storage, pipelineInfo: pipeline1);
            var pipeline2 = new PipelineInfo("pipeline2", "address");
            var behavior2 = CreateFlrBehavior(new FirstLevelRetryPolicy(2), storage, pipelineInfo: pipeline2);

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

        static FirstLevelRetriesBehavior CreateFlrBehavior(FirstLevelRetryPolicy retryPolicy, FlrStatusStorage storage = null, BusNotifications busNotifications = null, PipelineInfo pipelineInfo = null)
        {
            var flrBehavior = new FirstLevelRetriesBehavior(
                storage ?? new FlrStatusStorage(), 
                retryPolicy, 
                busNotifications ?? new BusNotifications());

            flrBehavior.Initialize(pipelineInfo ?? new PipelineInfo("samplePipeline", "address"));
            return flrBehavior;
        }

        ITransportReceiveContext CreateContext(string messageId)
        {
            return new TransportReceiveContext(new IncomingMessage(messageId, new Dictionary<string, string>(), new MemoryStream()), null, new RootContext(null));
        }
    }
}