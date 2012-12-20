namespace NServiceBus.ActiveMQ
{
    using System;
    using System.Collections.Generic;
    using FluentAssertions;
    using Moq;
    using NUnit.Framework;
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
        private readonly Func<TransportMessage, bool> tryReceiveMessage = m => true;

        private void VerifyAllReceiversAreStarted(Address address, TransactionSettings settings)
        {
            foreach (var messageReceiver in messageReceivers)
            {
                messageReceiver.Verify(mr => mr.Start(address, settings));
                messageReceiver.Object.TryProcessMessage.Should().Be(this.tryReceiveMessage);
            }
        }

        private INotifyMessageReceived CreateMessageReceiver()
        {
            var messageReceiver = new Mock<INotifyMessageReceived>();
            messageReceivers.Add(messageReceiver);
            messageReceiver.Setup(mr => mr.Dispose()).Callback(() => messageReceivers.Remove(messageReceiver));
            messageReceiver.SetupAllProperties();
            return messageReceiver.Object;
        }

        [Test]
        public void WhenStarted_ThenTheSpecifiedNumberOfReceiversIsCreatedAndStarted()
        {
            TransactionSettings settings = new TransactionSettings();
            const int NumberOfWorkers = 2;
            notifyMessageReceivedFactoryMock.Setup(f => f.CreateMessageReceiver()).Returns(CreateMessageReceiver);
            var address = new Address("someQueue", "machine");

            testee.Init(address, settings, this.tryReceiveMessage);
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

            testee.Init(address, new TransactionSettings(), m => { return true; });
            testee.Start(InitialNumberOfWorkers);
            testee.Stop();

            messageReceivers.Count.Should().Be(0);
        }
    }
}