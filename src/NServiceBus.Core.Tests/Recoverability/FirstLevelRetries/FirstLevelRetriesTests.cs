﻿namespace NServiceBus.Core.Tests.Recoverability.FirstLevelRetries
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Threading.Tasks;
    using NServiceBus.Pipeline;
    using NServiceBus.Pipeline.Contexts;
    using NServiceBus.Recoverability.FirstLevelRetries;
    using NServiceBus.Transports;
    using NServiceBus.Unicast.Transport;
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
        public async Task ShouldPerformFLRIfThereAreRetriesLeftToDo()
        {
            var behavior = CreateFlrBehavior(new FirstLevelRetryPolicy(1));
            var context = CreateContext("someid");

            await behavior.Invoke(context, () =>
            {
                throw new Exception("test");
            });

            Assert.True(context.AbortReceiveOperation);
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
            Assert.AreEqual("0", context.GetPhysicalMessage().Headers[Headers.FLRetries]);
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
        public async Task ShouldRememberRetryCountBetweenRetries()
        {
            const string messageId = "someid";
            var storage = new FlrStatusStorage();
            var pipeline = new PipelineInfo("anotherPipeline", "anotherAddress");
            var behavior = CreateFlrBehavior(new FirstLevelRetryPolicy(1), storage, pipelineInfo: pipeline);

            await behavior.Invoke(CreateContext(messageId), () =>
            {
                throw new Exception("test");
            });

            Assert.AreEqual(1, storage.GetFailuresForMessage(pipeline.Name + messageId));
        }

        [Test]
        public async Task ShouldRaiseBusNotificationsForFLR()
        {
            var notifications = new BusNotifications();
            var behavior = CreateFlrBehavior(new FirstLevelRetryPolicy(1), busNotifications: notifications);

            var notificationFired = false;

            notifications.Errors.MessageHasFailedAFirstLevelRetryAttempt.Subscribe(flr =>
            {
                Assert.AreEqual(0, flr.RetryAttempt);
                Assert.AreEqual("test", flr.Exception.Message);
                Assert.AreEqual("someid", flr.Headers[Headers.MessageId]);

                notificationFired = true;
            });

            await behavior.Invoke(CreateContext("someid"), () =>
            {
                throw new Exception("test");
            });

            Assert.True(notificationFired);
        }

        [Test]
        public async Task WillResetRetryCounterWhenFlrStorageCleared()
        {
            const string messageId = "someId";
            var storage = new FlrStatusStorage();
            var behavior = CreateFlrBehavior(new FirstLevelRetryPolicy(1), storage);

            await behavior.Invoke(CreateContext(messageId), () =>
            {
                throw new Exception("test");
            });

            storage.ClearAllFailures();

            Assert.DoesNotThrow(async () => await behavior.Invoke(CreateContext(messageId), () =>
            {
                throw new Exception("test");
            }));
        }

        [Test]
        public async Task ShouldTrackRetriesForEachPipelineIndependently()
        {
            const string messageId = "someId";
            var storage = new FlrStatusStorage();
            var pipeline1 = new PipelineInfo("pipeline1", "address");
            var behavior1 = CreateFlrBehavior(new FirstLevelRetryPolicy(1), storage, pipelineInfo: pipeline1);
            var pipeline2 = new PipelineInfo("pipeline2", "address");
            var behavior2 = CreateFlrBehavior(new FirstLevelRetryPolicy(2), storage, pipelineInfo: pipeline2);

            await behavior1.Invoke(CreateContext(messageId), () =>
            {
                throw new Exception("test");
            });

            await behavior2.Invoke(CreateContext(messageId), () =>
            {
                throw new Exception("test");
            });

            Assert.Throws<Exception>(async () => await behavior1.Invoke(CreateContext(messageId), () =>
            {
                throw new Exception("test");
            }));

            Assert.DoesNotThrow(async () => await behavior2.Invoke(CreateContext(messageId), () =>
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

        PhysicalMessageProcessingStageBehavior.Context CreateContext(string messageId)
        {
            var context = new PhysicalMessageProcessingStageBehavior.Context(new TransportReceiveContext(new IncomingMessage(messageId, new Dictionary<string, string>(), new MemoryStream()), null));
            return context;
        }
    }
}