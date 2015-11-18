namespace NServiceBus.Core.Tests.Recoverability.FirstLevelRetries
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using NServiceBus.Recoverability.FirstLevelRetries;
    using Transports;
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
            var message = CreateMessage("someid");
            var uniqueMessageId = "pipeline" + message.MessageId;

            handler.MarkFailure(uniqueMessageId, new Exception());

            ProcessingFailureInfo failureInfo;
            var result = handler.TryHandle(uniqueMessageId, message, out failureInfo);

            Assert.IsTrue(result);
        }

        [Test]
        public void ShouldBubbleTheExceptionUpIfThereAreNoMoreRetriesLeft()
        {
            var handler = CreateFlrHandler(new FirstLevelRetryPolicy(0));
            var message = CreateMessage("someid");
            var uniqueMessageId = "pipeline" + message.MessageId;

            handler.MarkFailure(uniqueMessageId, new Exception());

            ProcessingFailureInfo failureInfo;
            var result = handler.TryHandle(uniqueMessageId, message, out failureInfo);

            Assert.IsFalse(result);

            //should set the retries header to capture how many flr attempts where made
            Assert.AreEqual("0", message.Headers[Headers.FLRetries]);
        }

        [Test, Ignore]
        public void ShouldClearStorageAfterGivingUp()
        {
            //TODO: move this to behavior test after refactorings
            /*
            const string messageId = "someid";
            var storage = new FlrStatusStorage();
            var pipeline = new PipelineInfo("somePipeline", "someAddress");
            var handler = CreateFlrHandler(new FirstLevelRetryPolicy(1), storage);

            storage(pipeline.Name + messageId);

            var result = handler.TryHandle(pipeline.Name, CreateMessage(messageId), new Exception());

            Assert.IsFalse(result);
            Assert.AreEqual(0, storage.GetFailuresForMessage(pipeline.Name + messageId));
            */
        }

        [Test, Ignore]
        public void ShouldRememberRetryCountBetweenRetries()
        {
            //TODO: move this to behavior test after refactorings
            /*
            const string messageId = "someid";
            var storage = new FlrStatusStorage();
            var pipeline = new PipelineInfo("anotherPipeline", "anotherAddress");
            var handler = CreateFlrHandler(new FirstLevelRetryPolicy(1), storage);

            var result = handler.TryHandle(pipeline.Name, CreateMessage(messageId), new Exception());

            Assert.IsTrue(result);
            Assert.AreEqual(1, storage.GetFailuresForMessage(pipeline.Name + messageId));
            */
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
                Assert.AreEqual("someId", flr.MessageId);

                notificationFired = true;
            });

            var message = CreateMessage("someId");
            var uniqueMessageId = "pipeline" + message.MessageId;

            handler.MarkFailure(uniqueMessageId, new Exception("test"));

            ProcessingFailureInfo failureInfo;
            var result = handler.TryHandle(uniqueMessageId,message, out failureInfo);

            Assert.IsTrue(result);
            Assert.True(notificationFired);
        }

        [Test]
        public void WillResetRetryCounterWhenFlrStorageCleared()
        {
            const string messageId = "someId";
            var storage = new FlrStatusStorage();
            var handler = CreateFlrHandler(new FirstLevelRetryPolicy(1), storage);

            ProcessingFailureInfo failureInfo;

            handler.MarkFailure("msg1", new Exception());
            var firstTry = handler.TryHandle("pipeline", CreateMessage(messageId), out failureInfo);

            storage.ClearFailuresForMessage("msg1");

            handler.MarkFailure("msg1", new Exception());
            var secondTry = handler.TryHandle("pipeline", CreateMessage(messageId), out failureInfo);

            Assert.IsTrue(firstTry);
            Assert.IsTrue(secondTry);
        }

        [Test]
        public void ShouldTrackRetriesForEachPipelineIndependently()
        {
            //TODO: move to beharior tests after refactorings
            /*
            const string messageId = "someId";
            var storage = new FlrStatusStorage();
            var handler1 = CreateFlrHandler(new FirstLevelRetryPolicy(1), storage);
            var handler2 = CreateFlrHandler(new FirstLevelRetryPolicy(2), storage);

            var handler1FirstResult =  handler1.TryHandle("pipeline-1", CreateMessage(messageId), new Exception());
            var handler1SecondResult = handler1.TryHandle("pipeline-1", CreateMessage(messageId), new Exception());

            var handler2FirstResult =  handler2.TryHandle("pipeline-2", CreateMessage(messageId), new Exception());
            var handler2SecondResult = handler2.TryHandle("pipeline-2", CreateMessage(messageId), new Exception());

            Assert.IsTrue(handler1FirstResult);
            Assert.IsFalse(handler1SecondResult);

            Assert.IsTrue(handler2FirstResult);
            Assert.IsTrue(handler2SecondResult);
            */
        }

        static FirstLevelRetriesHandler CreateFlrHandler(FirstLevelRetryPolicy retryPolicy, FlrStatusStorage storage = null, BusNotifications busNotifications = null)
        {
            var flrHandler = new FirstLevelRetriesHandler(
                storage ?? new FlrStatusStorage(), 
                retryPolicy, 
                busNotifications ?? new BusNotifications());

            return flrHandler;
        }

        IncomingMessage CreateMessage(string messageId)
        {
            return new IncomingMessage(messageId, new Dictionary<string, string>(), new MemoryStream());
        }
    }
}
 