namespace NServiceBus.Core.Tests.Recoverability.FirstLevelRetries
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using NServiceBus.Pipeline.Contexts;
    using NServiceBus.Recoverability.FirstLevelRetries;
    using Transports;
    using Unicast.Transport;
    using NUnit.Framework;

    [TestFixture]
    public class FirstLevelRetriesTests
    {
        [Test]
        public void ShouldNotPerformFLROnMessagesThatCantBeDeserialized()
        {
            //TODO: this test should be
            /*
            var handler = CreateFlrHandler(new FirstLevelRetryPolicy(0));

           
            var result = handler.TryHandle("pipeline", null, new MessageDeserializationException("test"));

            Assert.Throws<MessageDeserializationException>(async () => await handler.Invoke(null, () =>
            {
                throw new MessageDeserializationException("test");
            }));
            */
        }

        [Test]
        public void ShouldPerformFLRIfThereAreRetriesLeftToDo()
        {
            var handler = CreateFlrHandler(new FirstLevelRetryPolicy(1));
            var context = CreateContext("someid");

            var result = handler.TryHandle("pipeline", context, new Exception());

            Assert.IsTrue(result);
        }

        [Test]
        public void ShouldBubbleTheExceptionUpIfThereAreNoMoreRetriesLeft()
        {
            var handler = CreateFlrHandler(new FirstLevelRetryPolicy(0));
            var context = CreateContext("someid");

            var result = handler.TryHandle("pipeline", context, new Exception());

            Assert.IsFalse(result);

            //should set the retries header to capture how many flr attempts where made
            Assert.AreEqual("0", context.Message.Headers[Headers.FLRetries]);
        }

        [Test]
        public void ShouldClearStorageAfterGivingUp()
        {
            const string messageId = "someid";
            var storage = new FlrStatusStorage();
            var pipeline = new PipelineInfo("somePipeline", "someAddress");
            var handler = CreateFlrHandler(new FirstLevelRetryPolicy(1), storage);

            storage.IncrementFailuresForMessage(pipeline.Name + messageId);

            var result = handler.TryHandle(pipeline.Name, CreateContext(messageId), new Exception());

            Assert.IsFalse(result);
            Assert.AreEqual(0, storage.GetFailuresForMessage(pipeline.Name + messageId));
        }

        [Test]
        public void ShouldRememberRetryCountBetweenRetries()
        {
            const string messageId = "someid";
            var storage = new FlrStatusStorage();
            var pipeline = new PipelineInfo("anotherPipeline", "anotherAddress");
            var handler = CreateFlrHandler(new FirstLevelRetryPolicy(1), storage);

            var result = handler.TryHandle(pipeline.Name, CreateContext(messageId), new Exception());

            Assert.IsTrue(result);
            Assert.AreEqual(1, storage.GetFailuresForMessage(pipeline.Name + messageId));
        }

        [Test]
        public void ShouldRaiseBusNotificationsForFLR()
        {
            var notifications = new BusNotifications();
            var handler = CreateFlrHandler(new FirstLevelRetryPolicy(1), busNotifications: notifications);

            var notificationFired = false;

            notifications.Errors.MessageHasFailedAFirstLevelRetryAttempt.Subscribe(flr =>
            {
                Assert.AreEqual(0, flr.RetryAttempt);
                Assert.AreEqual("test", flr.Exception.Message);
                Assert.AreEqual("someid", flr.MessageId);

                notificationFired = true;
            });

            var result = handler.TryHandle("pipeline", CreateContext("someid"), new Exception("test"));

            Assert.IsTrue(result);
            Assert.True(notificationFired);
        }

        [Test]
        public void WillResetRetryCounterWhenFlrStorageCleared()
        {
            const string messageId = "someId";
            var storage = new FlrStatusStorage();
            var handler = CreateFlrHandler(new FirstLevelRetryPolicy(1), storage);

            var firstTry = handler.TryHandle("pipeline", CreateContext(messageId), new Exception());

            storage.ClearAllFailures();

            var secondTry = handler.TryHandle("pipeline", CreateContext(messageId), new Exception());

            Assert.IsTrue(firstTry);
            Assert.IsTrue(secondTry);
        }

        [Test]
        public void ShouldTrackRetriesForEachPipelineIndependently()
        {
            const string messageId = "someId";
            var storage = new FlrStatusStorage();
            var handler1 = CreateFlrHandler(new FirstLevelRetryPolicy(1), storage);
            var handler2 = CreateFlrHandler(new FirstLevelRetryPolicy(2), storage);

            var handler1FirstResult =  handler1.TryHandle("pipeline-1", CreateContext(messageId), new Exception());
            var handler1SecondResult = handler1.TryHandle("pipeline-1", CreateContext(messageId), new Exception());

            var handler2FirstResult =  handler2.TryHandle("pipeline-2", CreateContext(messageId), new Exception());
            var handler2SecondResult = handler2.TryHandle("pipeline-2", CreateContext(messageId), new Exception());

            Assert.IsTrue(handler1FirstResult);
            Assert.IsFalse(handler1SecondResult);

            Assert.IsTrue(handler2FirstResult);
            Assert.IsTrue(handler2SecondResult);
        }

        static FirstLevelRetriesHandler CreateFlrHandler(FirstLevelRetryPolicy retryPolicy, FlrStatusStorage storage = null, BusNotifications busNotifications = null)
        {
            var flrHandler = new FirstLevelRetriesHandler(
                storage ?? new FlrStatusStorage(), 
                retryPolicy, 
                busNotifications ?? new BusNotifications());

            return flrHandler;
        }

        TransportReceiveContext CreateContext(string messageId)
        {
            return new TransportReceiveContext(new IncomingMessage(messageId, new Dictionary<string, string>(), new MemoryStream()), new RootContext(null));
        }
    }
}