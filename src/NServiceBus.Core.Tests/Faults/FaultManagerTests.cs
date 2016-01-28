namespace NServiceBus.Core.Tests.Faults
{
    using System;
    using System.Collections.Generic;
    using NServiceBus.Faults;
    using NServiceBus.Faults.Forwarder;
    using NServiceBus.ObjectBuilder;
    using NServiceBus.ObjectBuilder.Common;
    using NServiceBus.Transports;
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
            Configure.With();
            Configure.Instance.Builder = new FakeBuilder(fakeSender);
            faultManager = new FaultManager();
            faultManager.Init(new Address("fake", "fake"));
        }

        [TearDown]
        public void TearDown()
        {
            Configure.Instance.Builder = null;
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
        public void SendingToRetriesQueue_ProcessingAlwaysFailsForMessage_ShouldRemoveTTBR()
        {
            var exception = new InvalidOperationException();
            var message = new TransportMessage("id", new Dictionary<string, string>());
            message.TimeToBeReceived = TimeSpan.FromHours(1);

            var manager = (FaultManager)faultManager;
            manager.RetriesQueue = new Address("retries", "fake");

            faultManager.ProcessingAlwaysFailsForMessage(message, exception);

            var sentMessage = fakeSender.SentMessage;
            Assert.AreEqual(TimeSpan.MaxValue, sentMessage.TimeToBeReceived);
        }

        class FakeBuilder : IBuilder
        {
            private readonly FakeSender sender;

            public FakeBuilder(FakeSender sender)
            {
                this.sender = sender;
            }

            public object Build(Type typeToBuild)
            {
                throw new NotImplementedException();
            }

            public T Build<T>()
            {
                return (dynamic) sender;
            }

            public IEnumerable<object> BuildAll(Type typeToBuild)
            {
                throw new NotImplementedException();
            }

            public IEnumerable<T> BuildAll<T>()
            {
                throw new NotImplementedException();
            }

            public void BuildAndDispatch(Type typeToBuild, Action<object> action)
            {
                throw new NotImplementedException();
            }

            public IBuilder CreateChildBuilder()
            {
                throw new NotImplementedException();
            }

            public void Dispose()
            {
                throw new NotImplementedException();
            }

            public void Release(object instance)
            {
                throw new NotImplementedException();
            }
        }

        class FakeSender : ISendMessages
        {
            public TransportMessage SentMessage { get; set; }
            public void Send(TransportMessage message, Address address)
            {
                SentMessage = message;
            }
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
