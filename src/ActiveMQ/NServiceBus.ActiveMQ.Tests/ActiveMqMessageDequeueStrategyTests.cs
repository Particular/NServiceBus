namespace NServiceBus.Transports.ActiveMQ.Tests
{
    using System;
    using System.Collections.Generic;
    using FluentAssertions;
    using Moq;
    using NUnit.Framework;
    using NServiceBus.Transports.ActiveMQ;
    using NServiceBus.Transports.ActiveMQ.Receivers;
    using NServiceBus.Transports.ActiveMQ.SessionFactories;
    using NServiceBus.Unicast.Transport.Transactional;

    [TestFixture]
    public class ActiveMqMessageDequeueStrategyTests
    {
        [SetUp]
        public void SetUp()
        {
            notifyMessageReceivedFactoryMock = new Mock<INotifyMessageReceivedFactory>();
            pendingMessagesCounterMock = new Mock<IMessageCounter>();
            sessionFactroyMock = new Mock<ISessionFactory>();
            testee = new ActiveMqMessageDequeueStrategy(notifyMessageReceivedFactoryMock.Object, this.pendingMessagesCounterMock.Object, this.sessionFactroyMock.Object);
            messageReceivers = new List<Mock<INotifyMessageReceived>>();
            stoppedMessageReceivers = new List<Mock<INotifyMessageReceived>>();
            disposedMessageReceivers = new List<Mock<INotifyMessageReceived>>();

            notifyMessageReceivedFactoryMock
                .Setup(f => f.CreateMessageReceiver(It.IsAny<Func<TransportMessage, bool>>(), It.IsAny<Action<string, Exception>>()))
                .Returns(CreateMessageReceiver);
        }

        private Mock<INotifyMessageReceivedFactory> notifyMessageReceivedFactoryMock;
        private ActiveMqMessageDequeueStrategy testee;
        private List<Mock<INotifyMessageReceived>> messageReceivers;
        private List<Mock<INotifyMessageReceived>> stoppedMessageReceivers;
        private List<Mock<INotifyMessageReceived>> disposedMessageReceivers;
        private readonly Func<TransportMessage, bool> tryReceiveMessage = m => true;
        private Mock<IMessageCounter> pendingMessagesCounterMock;
        private Mock<ISessionFactory> sessionFactroyMock;

        private void VerifyAllReceiversAreStarted(Address address, TransactionSettings settings)
        {
            foreach (var messageReceiver in messageReceivers)
            {
                messageReceiver.Verify(mr => mr.Start(address, settings));
            }
        }

        private INotifyMessageReceived CreateMessageReceiver()
        {
            var messageReceiver = new Mock<INotifyMessageReceived>();
            messageReceivers.Add(messageReceiver);
            messageReceiver.Setup(mr => mr.Stop()).Callback(() => stoppedMessageReceivers.Add(messageReceiver));
            messageReceiver.Setup(mr => mr.Dispose()).Callback(() => disposedMessageReceivers.Add(messageReceiver));
            messageReceiver.SetupAllProperties();
            return messageReceiver.Object;
        }

        [Test]
        public void WhenStarted_ThenTheSpecifiedNumberOfReceiversIsCreatedAndStarted()
        {
            TransactionSettings settings = new TransactionSettings();
            const int NumberOfWorkers = 2;

            var address = new Address("someQueue", "machine");

            testee.Init(address, settings, this.tryReceiveMessage, (s, exception) => { });
            testee.Start(NumberOfWorkers);

            messageReceivers.Count.Should().Be(NumberOfWorkers);
            VerifyAllReceiversAreStarted(address, settings);
        }

        [Test]
        public void WhenStoped_ThenAllReceiversAreStopped()
        {
            const int InitialNumberOfWorkers = 5;
            var address = new Address("someQueue", "machine");

            testee.Init(address, new TransactionSettings(), m => { return true; }, (s, exception) => { });
            testee.Start(InitialNumberOfWorkers);
            testee.Stop();

            stoppedMessageReceivers.Should().BeEquivalentTo(messageReceivers);
        }

        [Test]
        public void WhenStoped_ThenAllReceiversAreDisposed()
        {
            const int InitialNumberOfWorkers = 5;
            var address = new Address("someQueue", "machine");

            testee.Init(address, new TransactionSettings(), m => { return true; }, (s, exception) => { });
            testee.Start(InitialNumberOfWorkers);
            testee.Stop();

            disposedMessageReceivers.Should().BeEquivalentTo(messageReceivers);
        }

        [Test]
        public void WhenStoped_ThenReceiversAreNotDisposedUntilAllPendingMessagesAreProcessed()
        {
            const int InitialNumberOfWorkers = 5;
            var address = new Address("someQueue", "machine");

            pendingMessagesCounterMock.Setup(mr => mr.Wait(It.IsAny<int>()))
                .Callback<int>(t => this.disposedMessageReceivers.Count.Should().Be(0));

            testee.Init(address, new TransactionSettings(), m => { return true; }, (s, exception) => { });
            testee.Start(InitialNumberOfWorkers);
            testee.Stop();

            this.pendingMessagesCounterMock.VerifyAll();
        }

        [Test]
        public void WhenStoped_SessionFactoryIsDisposedAfterMessageReceivers()
        {
            const int InitialNumberOfWorkers = 5;
            int disposedReceivers = 0;
            sessionFactroyMock.Setup(sf => sf.Dispose()).Callback(() => disposedReceivers = this.disposedMessageReceivers.Count);
            var address = new Address("someQueue", "machine");

            testee.Init(address, new TransactionSettings(), m => true, (s, exception) => { });
            testee.Start(InitialNumberOfWorkers);
            testee.Stop();

            sessionFactroyMock.VerifyAll();
            disposedReceivers.Should().Be(InitialNumberOfWorkers);
        }
    }
}