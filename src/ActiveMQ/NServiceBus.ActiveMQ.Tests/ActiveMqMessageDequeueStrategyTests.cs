namespace NServiceBus.ActiveMQ
{
    using System.Collections.Generic;
    using FluentAssertions;
    using Moq;
    using NUnit.Framework;
    using Unicast.Transport;
    using Unicast.Transport.Transactional;

    [TestFixture]
    public class ActiveMqMessageDequeueStrategyTests
    {
        [SetUp]
        public void SetUp()
        {
            notifyMessageReceivedFactoryMock = new Mock<INotifyMessageReceivedFactory>();
            testee = new ActiveMqMessageDequeueStrategy(notifyMessageReceivedFactoryMock.Object);
            messageReceivers = new List<Mock<INotifyMessageReceived>>();
        }

        private Mock<INotifyMessageReceivedFactory> notifyMessageReceivedFactoryMock;
        private ActiveMqMessageDequeueStrategy testee;
        private List<Mock<INotifyMessageReceived>> messageReceivers;

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
            messageReceiver.Setup(mr => mr.Dispose()).Callback(() => messageReceivers.Remove(messageReceiver));
            return messageReceiver.Object;
        }

        [Test]
        public void WhenMessageIsReceived_ThenMessageDequeuedIsRaised()
        {
            TransportMessageAvailableEventArgs lastDequeuedMessage = null;
            var message = new TransportMessage();

            notifyMessageReceivedFactoryMock.Setup(f => f.CreateMessageReceiver()).Returns(CreateMessageReceiver);
            var address = new Address("someQueue", "machine");

            testee.MessageDequeued += (sender, e) => lastDequeuedMessage = e;
            testee.Init(address, new TransactionSettings(), () => true);
            testee.Start(1);

            messageReceivers[0].Raise(mr => mr.MessageReceived += null, new TransportMessageReceivedEventArgs(message));

            lastDequeuedMessage.Should().NotBeNull();
            lastDequeuedMessage.Message.Should().Be(message);
        }

        [Test]
        public void WhenStarted_ThenTheSpecifiedNumberOfReceiversIsCreatedAndStarted()
        {
            TransactionSettings settings = new TransactionSettings();
            const int NumberOfWorkers = 2;
            notifyMessageReceivedFactoryMock.Setup(f => f.CreateMessageReceiver()).Returns(CreateMessageReceiver);
            var address = new Address("someQueue", "machine");

            testee.Init(address, settings, () => true);
            testee.Start(NumberOfWorkers);

            messageReceivers.Count.Should().Be(NumberOfWorkers);
            VerifyAllReceiversAreStarted(address, settings);
        }

        [Test]
        public void WhenStoped_ThenAllReceiversAreDisposed()
        {
            const int InitialNumberOfWorkers = 5;
            notifyMessageReceivedFactoryMock.Setup(f => f.CreateMessageReceiver()).Returns(CreateMessageReceiver);
            var address = new Address("someQueue", "machine");

            testee.Init(address, new TransactionSettings(), () => true);
            testee.Start(InitialNumberOfWorkers);
            testee.Stop();

            messageReceivers.Count.Should().Be(0);
        }
    }
}