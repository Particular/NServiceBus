namespace NServiceBus.Transports.ActiveMQ.Tests.Receivers
{
    using System;
    using System.Transactions;
    using ActiveMQ.Receivers;
    using ActiveMQ.Receivers.TransactionsScopes;
    using ActiveMQ.SessionFactories;
    using Apache.NMS;
    using FluentAssertions;
    using Moq;
    using NUnit.Framework;
    using Unicast.Transport;

    [TestFixture]
    public class MessageProcessorTest
    {
        private MessageProcessor testee;

        private Mock<ISessionFactory> sessionFactoryMock;
        private Mock<IActiveMqMessageMapper> activeMqMessageMapperMock;
        private Mock<ISession> session;
        private Mock<IActiveMqPurger> purger;
        private Mock<ITransactionScopeFactory> transactionScopeFactoryMock;
        private string order;

        [SetUp]
        public void SetUp()
        {
            Configure.Transactions.Enable()
                     .Advanced(
                         settings =>
                         settings.DefaultTimeout(TimeSpan.FromSeconds(10))
                                 .IsolationLevel(IsolationLevel.ReadCommitted)
                                 .EnableDistributedTransactions());

            sessionFactoryMock = new Mock<ISessionFactory>();
            activeMqMessageMapperMock = new Mock<IActiveMqMessageMapper>();
            purger = new Mock<IActiveMqPurger>();
            transactionScopeFactoryMock = new Mock<ITransactionScopeFactory>();

            testee = new MessageProcessor(
                activeMqMessageMapperMock.Object,
                sessionFactoryMock.Object,
                purger.Object,
                transactionScopeFactoryMock.Object);

            transactionScopeFactoryMock
                .Setup(f => f.CreateNewTransactionScope(It.IsAny<TransactionSettings>(), It.IsAny<ISession>()))
                .Returns(new Mock<ITransactionScope>().Object);

order = string.Empty;
        }

        [Test]
        public void WhenMessageIsReceived_ThenMessageReceivedIsRaised()
        {
            var messageMock = new Mock<IMessage>();
            var transportMessage = new TransportMessage();
            TransportMessage receivedMessage = null;

            SetupMapMessageToTransportMessage(messageMock.Object, transportMessage);

            testee.EndProcessMessage = (m, e) => { };
            testee.TryProcessMessage = m =>
                {
                    receivedMessage = m;
                    return true;
                };


            StartTestee();
            testee.ProcessMessage(messageMock.Object);

            receivedMessage.Should().Be(transportMessage);
        }

        [Test]
        public void WhenMessageIsProcessedSuccessfully_ThenTransactionIsCompleted()
        {
            var message = new Mock<IMessage>().Object;

            SetupTransactionOrderTracking(message);

            testee.EndProcessMessage = (m, e) => { order += "EndProcess_"; };
            testee.TryProcessMessage = m =>
                {
                    order += "MsgProcessed_";  
                    return true; 
                };

            StartTestee();
            testee.ProcessMessage(message);

            order.Should().Be("StartTx_TxMessageAccepted_MsgProcessed_TxComplete_EndProcess_TxDispose_");
        }

        [Test]
        public void WhenMessageProcessingFails_ThenTransactionIsNotCompleted()
        {
            var message = new Mock<IMessage>().Object;

            SetupTransactionOrderTracking(message);

            testee.EndProcessMessage = (m, e) => { order += "EndProcess_"; };
            testee.TryProcessMessage = m =>
                {
                    order += "MsgProcessed_";  
                    return false; 
                };

            StartTestee();
            testee.ProcessMessage(message);

            order.Should().Be("StartTx_TxMessageAccepted_MsgProcessed_EndProcess_TxDispose_");
        }

        [Test]
        public void WhenMessageProcessingThrowsException_ThenTransactionIsNotCompleted()
        {
            var message = new Mock<IMessage>().Object;

            SetupTransactionOrderTracking(message);

            testee.EndProcessMessage = (m, e) => { order += "EndProcess_"; };
            testee.TryProcessMessage = m =>
            {
                order += "MsgProcessed_";
                throw new Exception();
            };

            StartTestee();
            testee.ProcessMessage(message);

            order.Should().Be("StartTx_TxMessageAccepted_MsgProcessed_EndProcess_TxDispose_");
        }

        [Test]
        public void WhenStoppedBeforeIncrementingCounter_MessageIsNotProcessedAndTransactionRolledBack()
        {
            var message = new Mock<IMessage>().Object;

            SetupTransactionOrderTracking(message);

            testee.EndProcessMessage = (m, e) => { order += "EndProcess_"; };
            testee.TryProcessMessage = m =>
            {
                order += "MsgProcessed_";
                return true;
            };

            StartTestee();
            testee.Stop();
            testee.ProcessMessage(message);

            order.Should().Be("");
        }

        [Test]
        public void WhenDisposed_SessionIsReleased()
        {
            var message = new Mock<IMessage>().Object;

            SetupTransactionOrderTracking(message);

            testee.EndProcessMessage = (m, e) => { };
            testee.TryProcessMessage = m => true;

            StartTestee();
            testee.Stop();
            testee.Dispose();

            sessionFactoryMock.Verify(sf => sf.Release(session.Object));
        }

        [Test]
        [Ignore]
        public void WhenConsumerIsCreated_AndPurgeOnStartup_ThenDestinationIsPurged()
        {
            const string Destination = "anyQueue";

            testee.PurgeOnStartup = true;
            StartTestee(TransactionSettings.Default);
            SetupGetQueue(session, Destination);

            testee.CreateMessageConsumer(Destination);

            purger.Verify(p => p.Purge(session.Object, It.Is<IQueue>(d => d.QueueName.Contains(Destination))));
        }

        [Test]
        [Ignore]
        public void WhenConsumerIsCreated_AndNotPurgeOnStartup_ThenDestinationIsNotPurged()
        {
            const string Destination = "anyQueue";

            testee.PurgeOnStartup = false;
            StartTestee(TransactionSettings.Default);
            SetupGetQueue(session, Destination);

            testee.CreateMessageConsumer(Destination);

            purger.Verify(p => p.Purge(session.Object, It.Is<IQueue>(d => d.QueueName.Contains(Destination))), Times.Never());
        }

        private void SetupTransactionOrderTracking(IMessage message)
        {
            var transactionScopeMock = new Mock<ITransactionScope>();

            transactionScopeMock.Setup(tx => tx.Complete()).Callback(() => order += "TxComplete_");
            transactionScopeMock.Setup(tx => tx.MessageAccepted(message)).Callback(() => order += "TxMessageAccepted_");
            transactionScopeMock.Setup(tx => tx.Dispose()).Callback(() => order += "TxDispose_");

            transactionScopeFactoryMock.Setup(
                f => f.CreateNewTransactionScope(It.IsAny<TransactionSettings>(), It.IsAny<ISession>()))
                .Returns(transactionScopeMock.Object)
                .Callback<TransactionSettings, ISession>((t, s) => order += "StartTx_");
        }

        private void StartTestee()
        {
            var txSettings = TransactionSettings.Default;
            txSettings.IsTransactional = false;
            txSettings.DontUseDistributedTransactions = false;
            txSettings.IsolationLevel = IsolationLevel.Serializable;
            
            StartTestee(txSettings);
        }

        private void StartTestee(TransactionSettings transactionSettings)
        {
            session = SetupCreateSession();
            testee.Start(transactionSettings);
        }

        private IQueue SetupGetQueue(Mock<ISession> sessionMock, string queue)
        {
            var destinationMock = new Mock<IQueue>();
            sessionMock.Setup(s => s.GetQueue(queue)).Returns(destinationMock.Object);
            destinationMock.Setup(destination => destination.QueueName).Returns(queue);
            return destinationMock.Object;
        }

        

        private Mock<ISession> SetupCreateSession()
        {
            var sessionMock = new Mock<ISession> { DefaultValue = DefaultValue.Mock };
            sessionFactoryMock.Setup(c => c.GetSession()).Returns(sessionMock.Object);
            return sessionMock;
        }

        private void SetupMapMessageToTransportMessage(IMessage messageMock, TransportMessage transportMessage)
        {
            activeMqMessageMapperMock.Setup(m => m.CreateTransportMessage(messageMock)).Returns(transportMessage);
        }
    }
}