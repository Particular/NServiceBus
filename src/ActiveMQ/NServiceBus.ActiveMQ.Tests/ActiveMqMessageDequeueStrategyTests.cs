namespace NServiceBus.Transports.ActiveMQ.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Transactions;
    using ActiveMQ.Receivers;
    using ActiveMQ.SessionFactories;
    using FluentAssertions;
    using Moq;
    using NUnit.Framework;
    using Unicast.Transport;

    [TestFixture]
    public class ActiveMqMessageDequeueStrategyTests
    {
        [SetUp]
        public void SetUp()
        {
            Configure.Transactions.Enable()
                     .Advanced(
                         settings =>
                         settings.DefaultTimeout(TimeSpan.FromSeconds(10))
                                 .IsolationLevel(IsolationLevel.ReadCommitted)
                                 .EnableDistributedTransactions());

            notifyMessageReceivedFactoryMock = new Mock<INotifyMessageReceivedFactory>();
            sessionFactoryMock = new Mock<ISessionFactory>();
            testee = new ActiveMqMessageDequeueStrategy(notifyMessageReceivedFactoryMock.Object, sessionFactoryMock.Object);
            messageReceivers = new List<Mock<INotifyMessageReceived>>();
            stoppedMessageReceivers = new List<Mock<INotifyMessageReceived>>();
            disposedMessageReceivers = new List<Mock<INotifyMessageReceived>>();

            notifyMessageReceivedFactoryMock
                .Setup(f => f.CreateMessageReceiver(It.IsAny<Func<TransportMessage, bool>>(), It.IsAny<Action<TransportMessage, Exception>>()))
                .Returns(CreateMessageReceiver);
        }

        private Mock<INotifyMessageReceivedFactory> notifyMessageReceivedFactoryMock;
        private ActiveMqMessageDequeueStrategy testee;
        private List<Mock<INotifyMessageReceived>> messageReceivers;
        private List<Mock<INotifyMessageReceived>> stoppedMessageReceivers;
        private List<Mock<INotifyMessageReceived>> disposedMessageReceivers;
        private readonly Func<TransportMessage, bool> tryReceiveMessage = m => true;
        private Mock<ISessionFactory> sessionFactoryMock;

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
            var settings = TransactionSettings.Default;
            const int NumberOfWorkers = 2;

            var address = new Address("someQueue", "machine");

            testee.Init(address, settings, tryReceiveMessage, (s, exception) => { });
            testee.Start(NumberOfWorkers);

            messageReceivers.Count.Should().Be(NumberOfWorkers);
            VerifyAllReceiversAreStarted(address, settings);
        }

        [Test]
        public void WhenStopped_ThenAllReceiversAreStopped()
        {
            const int InitialNumberOfWorkers = 5;
            var address = new Address("someQueue", "machine");

            testee.Init(address, TransactionSettings.Default, m => { return true; }, (s, exception) => { });
            testee.Start(InitialNumberOfWorkers);
            testee.Stop();

            stoppedMessageReceivers.Should().BeEquivalentTo(messageReceivers);
        }

        [Test]
        public void WhenStopped_ThenAllReceiversAreDisposed()
        {
            const int InitialNumberOfWorkers = 5;
            var address = new Address("someQueue", "machine");

            testee.Init(address, TransactionSettings.Default, m => { return true; }, (s, exception) => { });
            testee.Start(InitialNumberOfWorkers);
            testee.Stop();

            disposedMessageReceivers.Should().BeEquivalentTo(messageReceivers);
        }

        [Test]
        public void WhenStopped_SessionFactoryIsDisposedAfterMessageReceivers()
        {
            const int InitialNumberOfWorkers = 5;
            var disposedReceivers = 0;
            sessionFactoryMock.Setup(sf => sf.Dispose()).Callback(() => disposedReceivers = disposedMessageReceivers.Count);
            var address = new Address("someQueue", "machine");

            testee.Init(address, TransactionSettings.Default, m => true, (s, exception) => { });
            testee.Start(InitialNumberOfWorkers);
            testee.Stop();

            sessionFactoryMock.VerifyAll();
            disposedReceivers.Should().Be(InitialNumberOfWorkers);
        }
    }
}
