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

            messageProcessorMock = new Mock<IProcessMessages>();
            eventConsumerMock = new Mock<IConsumeEvents>();
            messageConsumerMock = new Mock<IMessageConsumer>();

            testee = new ActiveMqMessageReceiver(
                eventConsumerMock.Object,
                messageProcessorMock.Object);

            messageProcessorMock
                .Setup(mp => mp.CreateMessageConsumer(It.IsAny<string>()))
                .Returns(messageConsumerMock.Object);

            Address.InitializeLocalAddress("local");
        }

        [Test]
        public void OnStart_MessageProcessorIsStarted()
        {
            var transactionSettings = TransactionSettings.Default;

            testee.Start(Address.Local, transactionSettings);

            messageProcessorMock.Verify(mp => mp.Start(transactionSettings));
        }

        [Test]
        public void OnStart_WhenLocalAddress_EventConsumerIsStarted()
        {
            var transactionSettings = TransactionSettings.Default;

            testee.Start(Address.Local, transactionSettings);

            eventConsumerMock.Verify(mp => mp.Start());
        }

        [Test]
        public void OnStart_WhenNotLocalAddress_EventConsumerIsNotStarted()
        {
            var transactionSettings = TransactionSettings.Default;

            testee.Start(new Address("someOtherQueue", "localhost"), transactionSettings);

            eventConsumerMock.Verify(mp => mp.Start(), Times.Never());
        }

        [Test]
        public void OnStart_MessageConsumerForAddreddIsCreated()
        {
            var queue = "somequeue";
            var transactionSettings = TransactionSettings.Default;

            testee.Start(new Address(queue, "localhost"), transactionSettings);

            messageProcessorMock.Verify(mp => mp.CreateMessageConsumer("queue://" + queue));
        }

        [Test]
        public void WhenMessageReceived_MessageProcessorIsInvoked()
        {
            var message = new Mock<IMessage>().Object;

            testee.Start(Address.Local, TransactionSettings.Default);
            messageConsumerMock.Raise(mc => mc.Listener += null, message);

            messageProcessorMock.Verify(mp => mp.ProcessMessage(message));
        }

        [Test]
        public void OnStop_MessageProcessorIsStopped()
        {
            testee.Start(Address.Local, TransactionSettings.Default);
            testee.Stop();

            messageProcessorMock.Verify(mp => mp.Stop());
        }

        [Test]
        public void OnStop_EventConsumerIsStopped()
        {
            testee.Start(Address.Local, TransactionSettings.Default);
            testee.Stop();

            eventConsumerMock.Verify(mp => mp.Stop());
        }

        [Test]
        public void WhenMessageReceivedAfterStop_MessageProcessorIsNotInvoked()
        {
            var message = new Mock<IMessage>().Object;

            testee.Start(Address.Local, TransactionSettings.Default);
            testee.Stop();
            messageConsumerMock.Raise(mc => mc.Listener += null, message);

            messageProcessorMock.Verify(mp => mp.ProcessMessage(message), Times.Never());
        }

        [Test]
        public void OnDispose_MessageProcessorIsDisposed()
        {
            testee.Start(Address.Local, TransactionSettings.Default);
            testee.Stop();
            testee.Dispose();

            messageProcessorMock.Verify(mp => mp.Dispose());
        }

        [Test]
        public void OnDispose_EventConsumerIsDisposed()
        {
            testee.Start(Address.Local, TransactionSettings.Default);
            testee.Stop();
            testee.Dispose();

            eventConsumerMock.Verify(mp => mp.Dispose());
        }

        [Test]
        public void OnDispose_MessageConsumerIsDisposed()
        {
            testee.Start(Address.Local, TransactionSettings.Default);
            testee.Stop();
            testee.Dispose();

            messageConsumerMock.Verify(mp => mp.Dispose());
        }
    }
}