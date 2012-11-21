namespace NServiceBus.Unicast.Queuing.ActiveMQ.Tests
{
    using System.Collections.Generic;

    using FluentAssertions;

    using Moq;

    using NServiceBus.Unicast.Transport;
    using NServiceBus.Unicast.Transport.Transactional;
    using NServiceBus.Unicast.Transport.Transactional.DequeueStrategies;

    using NUnit.Framework;

    [TestFixture]
    public class ActiveMqMessageDequeueStrategyTests
    {
        private Mock<INotifyMessageReceivedFactory> notifyMessageReceivedFactoryMock;
        private ActiveMqMessageDequeueStrategy testee;
        private List<Mock<INotifyMessageReceived>> messageReceivers;

        [SetUp]
        public void SetUp()
        {
            this.notifyMessageReceivedFactoryMock = new Mock<INotifyMessageReceivedFactory>();
            this.testee = new ActiveMqMessageDequeueStrategy(this.notifyMessageReceivedFactoryMock.Object);
            this.messageReceivers = new List<Mock<INotifyMessageReceived>>();
        }

        [Test]
        public void WhenStarted_ThenTheSpecifiedNumberOfReceiversIsCreatedAndStarted()
        {
            const int NumberOfWorkers = 2;
            this.notifyMessageReceivedFactoryMock.Setup(f => f.CreateMessageReceiver()).Returns(this.CreateMessageReceiver);
            var address = new Address("someQueue", "machine");
            
            this.testee.Init(address, new TransactionSettings());
            this.testee.Start(NumberOfWorkers);

            this.messageReceivers.Count.Should().Be(NumberOfWorkers);
            this.VerifyAllReceiversAreStarted(address);
        }

        [Test]
        public void WhenParallelismIsIncreased_ThenNewReceiversAreCreatedAndStarted()
        {
            const int InitialNumberOfWorkers = 2;
            const int NewNumberOfWorkers = 5;
            this.notifyMessageReceivedFactoryMock.Setup(f => f.CreateMessageReceiver()).Returns(this.CreateMessageReceiver);
            var address = new Address("someQueue", "machine");

            this.testee.Init(address, new TransactionSettings());
            this.testee.Start(InitialNumberOfWorkers);
            this.testee.ChangeMaxDegreeOfParallelism(NewNumberOfWorkers);

            this.messageReceivers.Count.Should().Be(NewNumberOfWorkers);
            this.VerifyAllReceiversAreStarted(address);
        }

        [Test]
        public void WhenParallelismIsDecreased_ThenReceiversAreDisposed()
        {
            const int InitialNumberOfWorkers = 5;
            const int NewNumberOfWorkers = 3;
            this.notifyMessageReceivedFactoryMock.Setup(f => f.CreateMessageReceiver()).Returns(this.CreateMessageReceiver);
            var address = new Address("someQueue", "machine");

            this.testee.Init(address, new TransactionSettings());
            this.testee.Start(InitialNumberOfWorkers);
            this.testee.ChangeMaxDegreeOfParallelism(NewNumberOfWorkers);

            this.messageReceivers.Count.Should().Be(NewNumberOfWorkers);
        }

        [Test]
        public void WhenStoped_ThenAllReceiversAreDisposed()
        {
            const int InitialNumberOfWorkers = 5;
            this.notifyMessageReceivedFactoryMock.Setup(f => f.CreateMessageReceiver()).Returns(this.CreateMessageReceiver);
            var address = new Address("someQueue", "machine");

            this.testee.Init(address, new TransactionSettings());
            this.testee.Start(InitialNumberOfWorkers);
            this.testee.Stop();

            this.messageReceivers.Count.Should().Be(0);
        }

        [Test]
        public void WhenMessageIsReceived_ThenMessageDequeuedIsRaised()
        {
            TransportMessageAvailableEventArgs lastDequeuedMessage = null;
            var message = new TransportMessage();

            this.notifyMessageReceivedFactoryMock.Setup(f => f.CreateMessageReceiver()).Returns(this.CreateMessageReceiver);
            var address = new Address("someQueue", "machine");

            this.testee.MessageDequeued += (sender, e) => lastDequeuedMessage = e;
            this.testee.Init(address, new TransactionSettings());
            this.testee.Start(1);

            this.messageReceivers[0].Raise(mr => mr.MessageReceived += null, new TransportMessageReceivedEventArgs(message));

            lastDequeuedMessage.Should().NotBeNull();
            lastDequeuedMessage.Message.Should().Be(message);
        }
        
        private void VerifyAllReceiversAreStarted(Address address)
        {
            foreach (var messageReceiver in this.messageReceivers)
            {
                messageReceiver.Verify(mr => mr.Start(address));
            }
        }

        private INotifyMessageReceived CreateMessageReceiver()
        {
            var messageReceiver = new Mock<INotifyMessageReceived>();
            this.messageReceivers.Add(messageReceiver);
            messageReceiver.Setup(mr => mr.Dispose()).Callback(() => this.messageReceivers.Remove(messageReceiver));
            return messageReceiver.Object;
        }
    }
}