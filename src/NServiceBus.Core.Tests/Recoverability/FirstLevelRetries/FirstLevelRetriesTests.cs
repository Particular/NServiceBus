namespace NServiceBus.Core.Tests.Recoverability.FirstLevelRetries
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using NServiceBus.Core.Tests.Recoverability.SecondLevelRetries;
    using NServiceBus.Hosting;
    using NServiceBus.Pipeline.Contexts;
    using NServiceBus.Recoverability.Faults;
    using NServiceBus.Recoverability.FirstLevelRetries;
    using NServiceBus.Unicast.Transport;
    using Transports;
    using NUnit.Framework;

    [TestFixture]
    public class FirstLevelRetriesTests
    {
        [Test]
        public void ShouldNotPerformFLROnMessagesThatCantBeDeserialized()
        {
            var behavior = CreateBehavior(new FirstLevelRetryPolicy(0));

            Assert.Throws<MessageDeserializationException>(async () => await behavior.Invoke(CreateContext("messageId"), () =>
            {
                throw new MessageDeserializationException("test");
            }));
        }

        [Test]
        public void ShouldPerformFLRIfThereAreRetriesLeftToDo()
        {
            var behavior = CreateBehavior(new FirstLevelRetryPolicy(1));
            var context = CreateContext("someid");

            Assert.Throws<MessageProcessingAbortedException>(async () => await behavior.Invoke(context, () =>
            {
                throw new Exception("test");
            }));
        }

        [Test]
        public async void ShouldBubbleTheExceptionUpIfThereAreNoMoreRetriesLeft()
        {
            var behavior = CreateBehavior(new FirstLevelRetryPolicy(0));
            var context = CreateContext("someid");

            Assert.Throws<MessageProcessingAbortedException>(async () => await behavior.Invoke(context, () =>
            {
                throw new Exception("test");
            }));

            await behavior.Invoke(context, () => TaskEx.Completed);

            //should set the retries header to capture how many flr attempts where made
            Assert.AreEqual("0", context.Message.Headers[Headers.FLRetries]);
        }

        [Test]
        public async void ShouldClearStorageAfterGivingUp()
        {
            const string messageId = "someid";
            var storage = new FlrStatusStorage();
            var pipeline = new PipelineInfo("somePipeline", "someAddress");
            var behavior = CreateBehavior(new FirstLevelRetryPolicy(1), storage, pipelineInfo: pipeline);

            storage.AddFailuresForMessage(pipeline.Name + messageId, new Exception());

            Assert.Throws<MessageProcessingAbortedException>(async () => await behavior.Invoke(CreateContext(messageId), () =>
            {
                throw new Exception();
            }));

            await behavior.Invoke(CreateContext(messageId), () => TaskEx.Completed);

            Assert.IsNull(storage.GetFailuresForMessage(pipeline.Name + messageId));
        }

        [Test]
        public void ShouldRememberRetryCountBetweenRetries()
        {
            const string messageId = "someid";
            var storage = new FlrStatusStorage();
            var pipeline = new PipelineInfo("anotherPipeline", "anotherAddress");
            var behavior = CreateBehavior(new FirstLevelRetryPolicy(1), storage, pipelineInfo: pipeline);

            Assert.Throws<MessageProcessingAbortedException>(async () => await behavior.Invoke(CreateContext(messageId), () =>
            {
                throw new Exception("test");
            }));

            Assert.AreEqual(1, storage.GetFailuresForMessage(pipeline.Name + messageId).NumberOfFailures);
        }

        [Test]
        public async void ShouldRaiseBusNotificationsForFLR()
        {
            var notifications = new BusNotifications();
            var behavior = CreateBehavior(new FirstLevelRetryPolicy(1), notifications: notifications);

            var notificationFired = false;

            notifications.Errors.MessageHasFailedAFirstLevelRetryAttempt.Subscribe(flr =>
            {
                Assert.AreEqual(0, flr.RetryAttempt);
                Assert.AreEqual("test", flr.Exception.Message);
                Assert.AreEqual("someid", flr.MessageId);

                notificationFired = true;
            });


            Assert.Throws<MessageProcessingAbortedException>(async () => await behavior.Invoke(CreateContext("someid"), () =>
            {
                throw new Exception("test");
            }));

            await behavior.Invoke(CreateContext("someid"), () => TaskEx.Completed);

            Assert.True(notificationFired);
        }

        [Test]
        public void WillResetRetryCounterWhenFlrStorageCleared()
        {
            const string messageId = "someId";
            var storage = new FlrStatusStorage();
            var pipeline = new PipelineInfo("anotherPipeline", "anotherAddress");
            var behavior = CreateBehavior(new FirstLevelRetryPolicy(1), storage, pipelineInfo: pipeline);

            Assert.Throws<MessageProcessingAbortedException>(async () => await behavior.Invoke(CreateContext(messageId), () =>
            {
                throw new Exception("test");
            }));

            storage.ClearFailuresForMessage(pipeline.Name + messageId);

            Assert.Throws<MessageProcessingAbortedException>(async () => await behavior.Invoke(CreateContext(messageId), () =>
            {
                throw new Exception("test");
            }));
        }

        [Test]
        public async void ShouldTrackRetriesForEachPipelineIndependently()
        {
            const string messageId = "someId";
            var storage = new FlrStatusStorage();
            var pipeline1 = new PipelineInfo("pipeline1", "address");
            var behavior1 = CreateBehavior(new FirstLevelRetryPolicy(0), storage, pipelineInfo: pipeline1);
            var pipeline2 = new PipelineInfo("pipeline2", "address");
            var behavior2 = CreateBehavior(new FirstLevelRetryPolicy(1), storage, pipelineInfo: pipeline2);
            var handler1Invocations = 0;
            var handler2Invocations = 0;

            Assert.Throws<MessageProcessingAbortedException>(async () => await behavior1.Invoke(CreateContext(messageId), () =>
            {
                handler1Invocations++;
                throw new Exception("test");
            }));

            Assert.Throws<MessageProcessingAbortedException>(async () => await behavior2.Invoke(CreateContext(messageId), () =>
            {
                handler2Invocations++;
                throw new Exception("test");
            }));

            await behavior1.Invoke(CreateContext(messageId), () =>
            {
                handler1Invocations++;
                throw new Exception("test");
            });

            Assert.Throws<MessageProcessingAbortedException>(async () => await behavior2.Invoke(CreateContext(messageId), () =>
            {
                handler2Invocations++;
                throw new Exception("test");
            }));

            Assert.AreEqual(1, handler1Invocations, "There should be not retries done by behavior1");
            Assert.AreEqual(2, handler2Invocations, "Second behavior should do one rety.");
        }

        private RecoverabilityBehavior CreateBehavior(FirstLevelRetryPolicy retryPolicy, FlrStatusStorage storage = null, BusNotifications notifications = null, PipelineInfo pipelineInfo = null)
        {
            var pipeline = new FakeDispatchPipeline();

            notifications = notifications ?? new BusNotifications();
            storage = storage ?? new FlrStatusStorage();

            var slrHandler = new SecondLevelRetriesHandler(pipeline, new FakePolicy(TimeSpan.MinValue), notifications, "my address");
            var flrHandler = new FirstLevelRetriesHandler(retryPolicy, notifications);
            var faultsHandler = new MoveFaultsToErrorQueueHandler(new FakeCriticalError(), pipeline, new HostInformation(Guid.NewGuid(), "my host"), notifications, "errors");

            var bahavior = new RecoverabilityBehavior(
                storage,
                faultsHandler,
                flrHandler,
                slrHandler);

            bahavior.Initialize(pipelineInfo ?? new PipelineInfo("samplePipeline", "address"));

            return bahavior;
        }

        TransportReceiveContext CreateContext(string messageId)
        {
            return new TransportReceiveContext(new IncomingMessage(messageId, new Dictionary<string, string>(), new MemoryStream()), new RootContext(null));
        }
    }
}
 