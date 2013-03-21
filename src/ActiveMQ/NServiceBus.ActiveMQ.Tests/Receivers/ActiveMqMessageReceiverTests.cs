namespace NServiceBus.Transports.ActiveMQ.Tests.Receivers
{
    using System;
    using System.Transactions;
    using Apache.NMS;
    using Moq;
    using NUnit.Framework;
    using NServiceBus.Transports.ActiveMQ.Receivers;
    using TransactionSettings = Unicast.Transport.TransactionSettings;

    [TestFixture]
    public class ActiveMqMessageReceiverTests
    {
        private ActiveMqMessageReceiver testee;

        private Mock<IMessageConsumer> messageConsumerMock;
        private Mock<IConsumeEvents> eventConsumerMock;
        private Mock<IProcessMessages> messageProcessorMock;

        [SetUp] 
        public void SetUp()
        {
            Configure.Transactions.Enable()
                      .Advanced(
                          settings =>
                          settings.DefaultTimeout(TimeSpan.FromSeconds(10))
                                  .IsolationLevel(IsolationLevel.ReadCommitted)
                                  .EnableDistributedTransactions());

            this.messageProcessorMock = new Mock<IProcessMessages>();
            this.eventConsumerMock = new Mock<IConsumeEvents>();
            this.messageConsumerMock = new Mock<IMessageConsumer>();

            this.testee = new ActiveMqMessageReceiver(
                this.eventConsumerMock.Object,
                this.messageProcessorMock.Object);

            this.messageProcessorMock
                .Setup(mp => mp.CreateMessageConsumer(It.IsAny<string>()))
                .Returns(this.messageConsumerMock.Object);

            Address.InitializeLocalAddress("local");
        }

        [Test]
        public void OnStart_MessageProcessorIsStarted()
        {
            var transactionSettings = TransactionSettings.Default;

            this.testee.Start(Address.Local, transactionSettings);

            this.messageProcessorMock.Verify(mp => mp.Start(transactionSettings));
        }

        [Test]
        public void OnStart_WhenLocalAddress_EventConsumerIsStarted()
        {
            var transactionSettings = TransactionSettings.Default;

            this.testee.Start(Address.Local, transactionSettings);

            this.eventConsumerMock.Verify(mp => mp.Start());
        }

        [Test]
        public void OnStart_WhenNotLocalAddress_EventConsumerIsNotStarted()
        {
            var transactionSettings = TransactionSettings.Default;

            this.testee.Start(new Address("someOtherQueue", "localhost"), transactionSettings);

            this.eventConsumerMock.Verify(mp => mp.Start(), Times.Never());
        }

        [Test]
        public void OnStart_MessageConsumerForAddreddIsCreated()
        {
            var queue = "somequeue";
            var transactionSettings = TransactionSettings.Default;

            this.testee.Start(new Address(queue, "localhost"), transactionSettings);

            this.messageProcessorMock.Verify(mp => mp.CreateMessageConsumer("queue://" + queue));
        }

        [Test]
        public void WhenMessageReceived_MessageProcessorIsInvoked()
        {
            var message = new Mock<IMessage>().Object;

            this.testee.Start(Address.Local, TransactionSettings.Default);
            this.messageConsumerMock.Raise(mc => mc.Listener += null, message);

            this.messageProcessorMock.Verify(mp => mp.ProcessMessage(message));
        }

        [Test]
        public void OnStop_MessageProcessorIsStopped()
        {
            this.testee.Start(Address.Local, TransactionSettings.Default);
            this.testee.Stop();

            this.messageProcessorMock.Verify(mp => mp.Stop());
        }

        [Test]
        public void OnStop_EventConsumerIsStopped()
        {
            this.testee.Start(Address.Local, TransactionSettings.Default);
            this.testee.Stop();

            this.eventConsumerMock.Verify(mp => mp.Stop());
        }

        [Test]
        public void WhenMessageReceivedAfterStop_MessageProcessorIsNotInvoked()
        {
            var message = new Mock<IMessage>().Object;

            this.testee.Start(Address.Local, TransactionSettings.Default);
            this.testee.Stop();
            this.messageConsumerMock.Raise(mc => mc.Listener += null, message);

            this.messageProcessorMock.Verify(mp => mp.ProcessMessage(message), Times.Never());
        }

        [Test]
        public void OnDispose_MessageProcessorIsDisposed()
        {
            this.testee.Start(Address.Local, TransactionSettings.Default);
            this.testee.Stop();
            this.testee.Dispose();

            this.messageProcessorMock.Verify(mp => mp.Dispose());
        }

        [Test]
        public void OnDispose_EventConsumerIsDisposed()
        {
            this.testee.Start(Address.Local, TransactionSettings.Default);
            this.testee.Stop();
            this.testee.Dispose();

            this.eventConsumerMock.Verify(mp => mp.Dispose());
        }

        [Test]
        public void OnDispose_MessageConsumerIsDisposed()
        {
            this.testee.Start(Address.Local, TransactionSettings.Default);
            this.testee.Stop();
            this.testee.Dispose();

            this.messageConsumerMock.Verify(mp => mp.Dispose());
        }
    }
}