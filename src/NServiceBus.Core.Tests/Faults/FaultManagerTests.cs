namespace NServiceBus.Core.Tests.Faults
{
    using System;
    using System.Collections.Generic;
    using NServiceBus.Faults;
    using NServiceBus.Faults.Forwarder;
    using NServiceBus.ObjectBuilder;
    using NServiceBus.ObjectBuilder.Common;
    using NServiceBus.SecondLevelRetries;
    using NServiceBus.Settings;
    using NServiceBus.Transports;
    using NServiceBus.Unicast;
    using NUnit.Framework;

    [TestFixture]
    public class FaultManagerTests
    {
        private IManageMessageFailures faultManager;
        private FakeSender fakeSender;

        [SetUp]
        public void SetUp()
        {
            fakeSender = new FakeSender();
            var settingsHolder = new SettingsHolder();
            faultManager = new FaultManager(fakeSender, new Configure(settingsHolder, new FakeContainer(), new List<Action<IConfigureComponents>>(), null), new BusNotifications());
            faultManager.Init(new Address("fake", "fake"));
        }

        [Test]
        public void SendingToErrorQueue_WhenSerializationFailedForMessage_ShouldRemoveTTBR()
        {
            var exception = new InvalidOperationException();
            var message = new TransportMessage("id", new Dictionary<string, string>());
            message.TimeToBeReceived = TimeSpan.FromHours(1);

            faultManager.SerializationFailedForMessage(message, exception);

            var sentMessage = fakeSender.SentMessage;
            Assert.AreEqual(TimeSpan.MaxValue, sentMessage.TimeToBeReceived);
        }

        [Test]
        public void SendingToErrorQueue_ProcessingAlwaysFailsForMessage_WhenSentFromSLR_ShouldRemoveTTBR()
        {
            var exception = new InvalidOperationException();
            var message = new TransportMessage("id", new Dictionary<string, string>());
            message.TimeToBeReceived = TimeSpan.FromHours(1);

            ((FaultManager) faultManager).RetriesQueue = new Address("fake", "fake");

            faultManager.ProcessingAlwaysFailsForMessage(message, exception);

            var sentMessage = fakeSender.SentMessage;
            Assert.AreEqual(TimeSpan.MaxValue, sentMessage.TimeToBeReceived);
        }

        [Test]
        public void SendingToErrorQueue_ProcessingAlwaysFailsForMessage_WhenRetriesQueueIsNull_ShouldRemoveTTBR()
        {
            var exception = new InvalidOperationException();
            var message = new TransportMessage("id", new Dictionary<string, string>());
            message.TimeToBeReceived = TimeSpan.FromHours(1);

            faultManager.ProcessingAlwaysFailsForMessage(message, exception);

            var sentMessage = fakeSender.SentMessage;
            Assert.AreEqual(TimeSpan.MaxValue, sentMessage.TimeToBeReceived);
        }

        [Test]
        public void SendingToErrorQueue_ProcessingAlwaysFailsForMessage_WhenPolicyReturnsTimeSpanZeroOrLess_ShouldRemoveTTBR()
        {
            var exception = new InvalidOperationException();
            var message = new TransportMessage("id", new Dictionary<string, string>());
            message.TimeToBeReceived = TimeSpan.FromHours(1);

            var manager = (FaultManager)faultManager;
            manager.RetriesQueue = new Address("retries", "fake");
            manager.SecondLevelRetriesConfiguration = new SecondLevelRetriesConfiguration
            {
                RetryPolicy = tm => TimeSpan.MinValue,
            };

            faultManager.ProcessingAlwaysFailsForMessage(message, exception);

            var sentMessage = fakeSender.SentMessage;
            Assert.AreEqual(TimeSpan.MaxValue, sentMessage.TimeToBeReceived);
        }

        [Test]
        public void SendingToRetriesQueue_ProcessingAlwaysFailsForMessage_ShouldRemoveTTBR()
        {
            var exception = new InvalidOperationException();
            var message = new TransportMessage("id", new Dictionary<string, string>());
            message.TimeToBeReceived = TimeSpan.FromHours(1);

            var manager = (FaultManager)faultManager;
            manager.RetriesQueue = new Address("retries", "fake");
            manager.SecondLevelRetriesConfiguration = new SecondLevelRetriesConfiguration
            {
                RetryPolicy = tm => TimeSpan.MaxValue,
            };

            faultManager.ProcessingAlwaysFailsForMessage(message, exception);

            var sentMessage = fakeSender.SentMessage;
            Assert.AreEqual(TimeSpan.MaxValue, sentMessage.TimeToBeReceived);
        }

        class FakeSender : ISendMessages
        {
            public void Send(TransportMessage message, SendOptions sendOptions)
            {
                SentMessage = message;
            }

            public TransportMessage SentMessage { get; set; }
        }

        class FakeContainer : IContainer
        {
            public void Dispose()
            {
            }

            public object Build(Type typeToBuild)
            {
                return null;
            }

            public IContainer BuildChildContainer()
            {
                return null;
            }

            public IEnumerable<object> BuildAll(Type typeToBuild)
            {
                yield break;
            }

            public void Configure(Type component, DependencyLifecycle dependencyLifecycle)
            {
            }

            public void Configure<T>(Func<T> component, DependencyLifecycle dependencyLifecycle)
            {
            }

            public void ConfigureProperty(Type component, string property, object value)
            {
            }

            public void RegisterSingleton(Type lookupType, object instance)
            {
            }

            public bool HasComponent(Type componentType)
            {
                return true;
            }

            public void Release(object instance)
            {
            }
        }
    }
}
