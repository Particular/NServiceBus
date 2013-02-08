﻿namespace NServiceBus.ActiveMQ.Receivers
{
    using System;
    using System.Transactions;

    using Apache.NMS;

    using FluentAssertions;

    using Moq;

    using NServiceBus.Transport.ActiveMQ;
    using NServiceBus.Transport.ActiveMQ.Receivers;
    using NServiceBus.Transport.ActiveMQ.Receivers.TransactonsScopes;
    using NServiceBus.Unicast.Transport.Transactional;

    using NUnit.Framework;

    [TestFixture]
    public class MessageProcessorTest
    {
        private MessageProcessor testee;

        private Mock<ISessionFactory> sessionFactoryMock;
        private Mock<IActiveMqMessageMapper> activeMqMessageMapperMock;
        private Mock<ISession> session;
        private Mock<IActiveMqPurger> purger;
        private Mock<IMessageCounter> pendingMessageCounterMock;
        private Mock<ITransactionScopeFactory> transactionScopeFactoryMock;
        private string order;

        [SetUp]
        public void SetUp()
        {
            this.sessionFactoryMock = new Mock<ISessionFactory>();
            this.activeMqMessageMapperMock = new Mock<IActiveMqMessageMapper>();
            this.purger = new Mock<IActiveMqPurger>();
            this.pendingMessageCounterMock = new Mock<IMessageCounter>();
            this.transactionScopeFactoryMock = new Mock<ITransactionScopeFactory>();

            this.testee = new MessageProcessor(
                this.pendingMessageCounterMock.Object,
                this.activeMqMessageMapperMock.Object,
                this.sessionFactoryMock.Object,
                this.purger.Object,
                this.transactionScopeFactoryMock.Object);

            this.transactionScopeFactoryMock
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

            this.SetupMapMessageToTransportMessage(messageMock.Object, transportMessage);

            this.testee.EndProcessMessage = (m, e) => { };
            this.testee.TryProcessMessage = m =>
                {
                    receivedMessage = m;
                    return true;
                };


            this.StartTestee();
            this.testee.ProcessMessage(messageMock.Object);

            receivedMessage.Should().Be(transportMessage);
        }

        [Test]
        public void WhenMessageIsProcessedSucccessfully_ThenTransactionIsCompleted()
        {
            var message = new Mock<IMessage>().Object;

            this.SetuptransactionOrderTracking(message);

            this.testee.EndProcessMessage = (m, e) => { order += "EndProcess_"; };
            this.testee.TryProcessMessage = m =>
                {
                    order += "MsgProcessed_";  
                    return true; 
                };

            this.StartTestee();
            this.testee.ProcessMessage(message);

            order.Should().Be("IncCounter_StartTx_TxMessageAccepted_MsgProcessed_TxComplete_EndProcess_TxDispose_DecCounter_");
        }

        [Test]
        public void WhenMessageProcessingFails_ThenTransactionIsNotCompleted()
        {
            var message = new Mock<IMessage>().Object;

            this.SetuptransactionOrderTracking(message);

            this.testee.EndProcessMessage = (m, e) => { order += "EndProcess_"; };
            this.testee.TryProcessMessage = m =>
                {
                    order += "MsgProcessed_";  
                    return false; 
                };

            this.StartTestee();
            this.testee.ProcessMessage(message);

            order.Should().Be("IncCounter_StartTx_TxMessageAccepted_MsgProcessed_EndProcess_TxDispose_DecCounter_");
        }

        [Test]
        public void WhenMessageProcessingThrowsException_ThenTransactionIsNotCompleted()
        {
            var message = new Mock<IMessage>().Object;

            this.SetuptransactionOrderTracking(message);

            this.testee.EndProcessMessage = (m, e) => { order += "EndProcess_"; };
            this.testee.TryProcessMessage = m =>
            {
                order += "MsgProcessed_";
                throw new Exception();
            };

            this.StartTestee();
            this.testee.ProcessMessage(message);

            order.Should().Be("IncCounter_StartTx_TxMessageAccepted_MsgProcessed_EndProcess_TxDispose_DecCounter_");
        }

        [Test]
        public void WhenStoppedRightAfterIncrementingCounter_MessageIsNotProcessedAndTransactionRollbacked()
        {
            var message = new Mock<IMessage>().Object;

            this.SetuptransactionOrderTracking(message);
            this.pendingMessageCounterMock.Setup(c => c.Increment()).Callback(
                () =>
                    {
                        this.order += "IncCounter_";
                        this.testee.Stop();
                    });


            this.testee.EndProcessMessage = (m, e) => { order += "EndProcess_"; };
            this.testee.TryProcessMessage = m =>
            {
                order += "MsgProcessed_";
                return true;
            };

            this.StartTestee();
            this.testee.ProcessMessage(message);

            order.Should().Be("IncCounter_StartTx_TxDispose_DecCounter_");
        }

        [Test]
        public void WhenStoppedBeforeIncrementingCounter_MessageIsNotProcessedAndTransactionRollbacked()
        {
            var message = new Mock<IMessage>().Object;

            this.SetuptransactionOrderTracking(message);

            this.testee.EndProcessMessage = (m, e) => { order += "EndProcess_"; };
            this.testee.TryProcessMessage = m =>
            {
                order += "MsgProcessed_";
                return true;
            };

            this.StartTestee();
            this.testee.Stop();
            this.testee.ProcessMessage(message);

            order.Should().Be("IncCounter_StartTx_TxDispose_DecCounter_");
        }

        [Test]
        public void WhenDisposed_SessionIsReleased()
        {
            var message = new Mock<IMessage>().Object;

            this.SetuptransactionOrderTracking(message);

            this.testee.EndProcessMessage = (m, e) => { };
            this.testee.TryProcessMessage = m => true;

            this.StartTestee();
            this.testee.Stop();
            this.testee.Dispose();

            this.sessionFactoryMock.Verify(sf => sf.Release(this.session.Object));
        }

        [Test]
        public void WhenConsumerIsCreated_AndPurgeOnStartup_ThenDestinationIsPurged()
        {
            const string Destination = "anyqueue";

            this.testee.PurgeOnStartup = true;
            this.StartTestee(new TransactionSettings());
            this.SetupGetQueue(this.session, Destination);

            this.testee.CreateMessageConsumer(Destination);

            this.purger.Verify(p => p.Purge(this.session.Object, It.Is<IQueue>(d => d.QueueName.Contains(Destination))));
        }

        [Test]
        public void WhenConsumerIsCreated_AndNotPurgeOnStartup_ThenDestinationIsNotPurged()
        {
            const string Destination = "anyqueue";

            this.testee.PurgeOnStartup = false;
            this.StartTestee(new TransactionSettings());
            this.SetupGetQueue(this.session, Destination);

            this.testee.CreateMessageConsumer(Destination);

            this.purger.Verify(p => p.Purge(this.session.Object, It.Is<IQueue>(d => d.QueueName.Contains(Destination))), Times.Never());
        }

        private void SetuptransactionOrderTracking(IMessage message)
        {
            var transactionScopeMock = new Mock<ITransactionScope>();

            transactionScopeMock.Setup(tx => tx.Complete()).Callback(() => this.order += "TxComplete_");
            transactionScopeMock.Setup(tx => tx.MessageAccepted(message)).Callback(() => this.order += "TxMessageAccepted_");
            transactionScopeMock.Setup(tx => tx.Dispose()).Callback(() => this.order += "TxDispose_");
            this.pendingMessageCounterMock.Setup(c => c.Increment()).Callback(() => this.order += "IncCounter_");
            this.pendingMessageCounterMock.Setup(c => c.Decrement()).Callback(() => this.order += "DecCounter_");

            this.transactionScopeFactoryMock.Setup(
                f => f.CreateNewTransactionScope(It.IsAny<TransactionSettings>(), It.IsAny<ISession>()))
                .Returns(transactionScopeMock.Object)
                .Callback<TransactionSettings, ISession>((t, s) => this.order += "StartTx_");
        }

        private void StartTestee()
        {
            this.StartTestee(
                new TransactionSettings
                    {
                        IsTransactional = false,
                        DontUseDistributedTransactions = false,
                        IsolationLevel = IsolationLevel.Serializable
                    });
        }

        private void StartTestee(TransactionSettings transactionSettings)
        {
            this.session = this.SetupCreateSession();
            this.testee.Start(transactionSettings);
        }

        private IQueue SetupGetQueue(Mock<ISession> sessionMock, string queue)
        {
            var destinationMock = new Mock<IQueue>();
            sessionMock.Setup(s => s.GetQueue(queue)).Returns(destinationMock.Object);
            destinationMock.Setup(destination => destination.QueueName).Returns(queue);
            return destinationMock.Object;
        }

        private Mock<IMessageConsumer> SetupCreateConsumer(Mock<ISession> sessionMock, IDestination destination)
        {
            var consumerMock = new Mock<IMessageConsumer>();
            sessionMock.Setup(s => s.CreateConsumer(destination)).Returns(consumerMock.Object);
            return consumerMock;
        }

        private Mock<IMessageConsumer> SetupCreateConsumer(Mock<ISession> sessionMock, string queue)
        {
            var destination = this.SetupGetQueue(this.session, queue);
            return this.SetupCreateConsumer(sessionMock, destination);
        }

        private Mock<ISession> SetupCreateSession()
        {
            var sessionMock = new Mock<ISession> { DefaultValue = DefaultValue.Mock };
            this.sessionFactoryMock.Setup(c => c.GetSession()).Returns(sessionMock.Object);
            return sessionMock;
        }

        private void SetupMapMessageToTransportMessage(IMessage messageMock, TransportMessage transportMessage)
        {
            this.activeMqMessageMapperMock.Setup(m => m.CreateTransportMessage(messageMock)).Returns(transportMessage);
        }
    }
}