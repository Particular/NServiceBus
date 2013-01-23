namespace NServiceBus.Transport.ActiveMQ
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using System.Transactions;

    using Apache.NMS;
    using FluentAssertions;
    using Moq;
    using NServiceBus.Unicast.Transport.Transactional;
    using NUnit.Framework;

    [TestFixture]
    public class ActiveMqMessageReceiverTests
    {
        private ActiveMqMessageReceiver testee;

        private Mock<ISessionFactory> sessionFactoryMock;
        private Mock<IActiveMqMessageMapper> activeMqMessageMapperMock;
        private NotifyTopicSubscriptionsMock subscriptionManagerMock;
        private Mock<INetTxSession> session;
        private Mock<IActiveMqPurger> purger;
        private Mock<IMessageConsumer> consumer;

        [SetUp] 
        public void SetUp()
        {
            this.sessionFactoryMock = new Mock<ISessionFactory>();
            this.activeMqMessageMapperMock = new Mock<IActiveMqMessageMapper>();
            this.subscriptionManagerMock = new NotifyTopicSubscriptionsMock();
            this.purger = new Mock<IActiveMqPurger>();

            this.testee = new ActiveMqMessageReceiver(
                this.sessionFactoryMock.Object, 
                this.activeMqMessageMapperMock.Object, 
                this.subscriptionManagerMock,
                this.purger.Object);

            testee.EndProcessMessage = (s, exception) => { };
        }

        [Test]
        public void WhenMessageIsReceived_ThenMessageReceivedIsRaised()
        {
            const string Queue = "somequeue";
            var messageMock = new Mock<IMessage>();
            var transportMessage = new TransportMessage();
            TransportMessage receivedMessage = null;

            this.testee.TryProcessMessage = m =>
                {
                    receivedMessage = m; 
                    return true; 
                };
            this.SetupMapMessageToTransportMessage(messageMock.Object, transportMessage);

            this.StartTestee(new Address(Queue, "machine"));
            this.consumer.Raise(c => c.Listener += null, messageMock.Object);

            receivedMessage.Should().Be(transportMessage);
        }

        [Test]
        public void WhenSubscriptionIsAddedToReceiverWithLocalAddress_ThenTopicIsSubscribed()
        {
            const string Topic = "SomeTopic";
            const string ConsumerName = "A";
            var message = new Mock<IMessage>().Object;
            var transportMessage = new TransportMessage();
            TransportMessage receivedMessage = null;

            this.testee.TryProcessMessage = m =>
            {
                receivedMessage = m;
                return true;
            };
            this.testee.ConsumerName = ConsumerName;
            this.StartTesteeWithLocalAddress();

            this.SetupMapMessageToTransportMessage(message, transportMessage);
            var topicConsumer = this.SetupCreateConsumer(this.session, string.Format("Consumer.{0}.{1}", ConsumerName, Topic));

            this.RaiseTopicSubscribed(Topic);
            this.RaiseEventReceived(topicConsumer, message);

            receivedMessage.Should().Be(transportMessage);
        }

        [Test]
        public void WhenSubscriptionIsAddedToReceiverWithLocalAddressAndPurgeRequired_ThenPurge()
        {
            const string AnyTopic = "SomeTopic";

            this.testee.PurgeOnStartup = true;
            this.testee.ConsumerName = "A";

            this.StartTesteeWithLocalAddress();
            var destination = this.SetupGetQueue(this.session, string.Format("Consumer.{0}.{1}", "A", AnyTopic));

            this.RaiseTopicSubscribed(AnyTopic);

            this.purger.Verify(s => s.Purge(It.IsAny<ISession>(), destination));
        }

        [Test]
        public void WhenSubscriptionIsAddedToReceiverWithLocalAddressAndPurgeNotRequired_ThenNotPurge()
        {
            this.testee.PurgeOnStartup = false;

            this.StartTesteeWithLocalAddress();

            this.RaiseTopicSubscribed("AnyTopic");

            this.purger.Verify(s => s.Purge(It.IsAny<ISession>(), It.IsAny<IDestination>()), Times.Never());
        }

        [Test]
        public void WhenSubscriptionIsAddedToReceiverWithNotLocalAddress_ThenTopicIsNotSubscribed()
        {
            const string Topic = "SomeTopic";
            const string ConsumerName = "A";
            var messageMock = new Mock<IMessage>();
            TransportMessage receivedMessage = null;

            this.testee.TryProcessMessage = m =>
            {
                receivedMessage = m;
                return true;
            }; 
            
            this.testee.ConsumerName = ConsumerName;
            this.StartTestee(new Address("queue", "machine"));

            var topicConsumer = this.SetupCreateConsumer(this.session, string.Format("Consumer.{0}.{1}", ConsumerName, Topic));

            this.RaiseTopicSubscribed(Topic);
            topicConsumer.Raise(c => c.Listener += null, messageMock.Object);

            receivedMessage.Should().BeNull();
        }

        [Test]
        public void WhenTopicIsUnsubscribed_ThenConsumerIsDisposed()
        {
            const string Topic = "SomeTopic";
            const string ConsumerName = "A";

            this.testee.ConsumerName = ConsumerName;
            this.StartTesteeWithLocalAddress();

            var topicConsumer = this.SetupCreateConsumer(this.session, string.Format("Consumer.{0}.{1}", ConsumerName, Topic));

            this.RaiseTopicSubscribed(Topic);
            this.RaiseTopicUnsubscribed(Topic);

            topicConsumer.Verify(c => c.Dispose());
        }

        [Test]
        public void Start_WhenPurgeRequired_ThenPurge()
        {
            this.testee.PurgeOnStartup = true;

            this.StartTestee(new Address("anyqueue", "anymachine"));

            this.purger.Verify(s => s.Purge(this.session.Object, It.Is<IQueue>(d => d.QueueName.Contains("anyqueue"))));
        }

        [Test]
        public void Start_WhenPurgeNotRequired_ThenNotPurge()
        {
            this.testee.PurgeOnStartup = false;

            this.StartTestee(new Address("anyqueue", "anymachine"));

            this.purger.Verify(s => s.Purge(It.IsAny<ISession>(), It.IsAny<IDestination>()), Times.Never());
        }

        [Test]
        public void Dispose_ShouldReleaseResources()
        {
            const string Topic = "SomeTopic";
            const string ConsumerName = "A";

            this.testee.ConsumerName = ConsumerName;
            this.StartTesteeWithLocalAddress();

            var topicConsumer = this.SetupCreateConsumer(this.session, string.Format("Consumer.{0}.{1}", ConsumerName, Topic));

            this.RaiseTopicSubscribed(Topic);

            this.testee.Stop();

            topicConsumer.Verify(c => c.Close());
            topicConsumer.Verify(c => c.Dispose());

            this.consumer.Verify(c => c.Close());
            this.consumer.Verify(c => c.Dispose());
            this.sessionFactoryMock.Verify(sf => sf.Release(this.session.Object));
        }

        [Test]
        public void WhenUsingActiveMqTransaction_OnErrorDuringProcessing_ThenSessionIsRolledBack()
        {
            this.testee.TryProcessMessage = m => false;

            this.StartTestee(new Address("queue", "machine"), new TransactionSettings() { IsTransactional = true, SuppressDTC = true });
            this.consumer.Raise(c => c.Listener += null, new Mock<IMessage>().Object);

            session.Verify(s => s.Rollback());
        }

        [Test]
        public void WhenUsingActiveMqTransaction_WhenMessageIsProcessedSuccessfully_ThenSessionIsCommit()
        {
            this.testee.TryProcessMessage = m => true;

            this.StartTestee(new Address("queue", "machine"), new TransactionSettings() { IsTransactional = true, SuppressDTC = true });
            this.consumer.Raise(c => c.Listener += null, new Mock<IMessage>().Object);

            session.Verify(s => s.Commit());
        }

        [Test]
        public void WhenUsingActiveMqTransaction_SessionIsSetAsSessionForCurrentThreadDuringProcessing()
        {
            string executionOrder = string.Empty;

            this.testee.TryProcessMessage = m =>
                {
                    executionOrder += "ProcessMessage;";
                    return true;
                };
            this.sessionFactoryMock.Setup(sf => sf.SetSessionForCurrentThread(It.IsAny<INetTxSession>()))
                .Callback(() => executionOrder += "SetSessionForCurrentThread;");
            this.sessionFactoryMock.Setup(sf => sf.RemoveSessionForCurrentThread())
                .Callback(() => executionOrder += "RemoveSessionForCurrentThread;");

            this.StartTestee(new Address("queue", "machine"), new TransactionSettings() { IsTransactional = true, SuppressDTC = true });
            this.consumer.Raise(c => c.Listener += null, new Mock<IMessage>().Object);

            executionOrder.Should().Be("SetSessionForCurrentThread;ProcessMessage;RemoveSessionForCurrentThread;");
        }

        [Test]
        public void WhenUsingDTCTransaction_OnErrorDuringProcessing_ThenTransactionIsRolledback()
        {
            var transactionStatus = TransactionStatus.InDoubt;

            this.testee.TryProcessMessage = m => false;
            this.StartTestee(
                new Address("queue", "machine"),
                new TransactionSettings
                    {
                        IsTransactional = true,
                        SuppressDTC = false,
                        IsolationLevel = IsolationLevel.Serializable
                    });

            Action action = () =>
                {
                    using (var transaction = new TransactionScope())
                    {
                        Transaction.Current.TransactionCompleted +=
                            (s, e) => transactionStatus = e.Transaction.TransactionInformation.Status;

                        this.consumer.Raise(c => c.Listener += null, new Mock<IMessage>().Object);

                        transaction.Complete();
                    }
                };

            action.ShouldThrow<TransactionAbortedException>();
            transactionStatus.Should().Be(TransactionStatus.Aborted);
        }
        
        [Test]
        public void WhenUsingDTCTransaction_ProcessedSuccefully_ThenTransactionIsCommited()
        {
            var transactionStatus = TransactionStatus.InDoubt;

            this.testee.TryProcessMessage = m => true;
            this.StartTestee(
                new Address("queue", "machine"),
                new TransactionSettings
                {
                    IsTransactional = true,
                    SuppressDTC = false,
                    IsolationLevel = IsolationLevel.Serializable
                });

            using (var transaction = new TransactionScope())
            {
                Transaction.Current.TransactionCompleted += (s, e) => transactionStatus = e.Transaction.TransactionInformation.Status;

                this.consumer.Raise(c => c.Listener += null, new Mock<IMessage>().Object);

                transaction.Complete();
            }

            transactionStatus.Should().Be(TransactionStatus.Committed);
        }
        
        private void StartTesteeWithLocalAddress()
        {
            Address.InitializeLocalAddress("somequeue");
            this.StartTestee(Address.Local);
        }
        
        private void StartTestee(Address address)
        {
            this.StartTestee(
                address,
                new TransactionSettings
                    {
                        IsTransactional = false,
                        SuppressDTC = false,
                        IsolationLevel = IsolationLevel.Serializable
                    });
        }

        private void StartTestee(Address address, TransactionSettings transactionSettings)
        {
            this.session = this.SetupCreateSession();

            this.consumer = this.SetupCreateConsumer(this.session, address.Queue);

            this.testee.Start(address, transactionSettings);
        }

        private IQueue SetupGetQueue(Mock<INetTxSession> sessionMock, string queue)
        {
            var destinationMock = new Mock<IQueue>();
            sessionMock.Setup(s => s.GetQueue(queue)).Returns(destinationMock.Object);
            destinationMock.Setup(destination => destination.QueueName).Returns(queue);
            return destinationMock.Object;
        }
        
        private Mock<IMessageConsumer> SetupCreateConsumer(Mock<INetTxSession> sessionMock, IDestination destination)
        {
            var consumerMock = new Mock<IMessageConsumer>();
            sessionMock.Setup(s => s.CreateConsumer(destination)).Returns(consumerMock.Object);
            return consumerMock;
        }

        private Mock<IMessageConsumer> SetupCreateConsumer(Mock<INetTxSession> sessionMock, string queue)
        {
            var destination = this.SetupGetQueue(this.session, queue);
            return this.SetupCreateConsumer(sessionMock, destination);
        }

        private Mock<INetTxSession> SetupCreateSession()
        {
            var sessionMock = new Mock<INetTxSession> { DefaultValue = DefaultValue.Mock };
            this.sessionFactoryMock.Setup(c => c.GetSession()).Returns(sessionMock.Object);
            return sessionMock;
        }

        private void SetupMapMessageToTransportMessage(IMessage messageMock, TransportMessage transportMessage)
        {
            this.activeMqMessageMapperMock.Setup(m => m.CreateTransportMessage(messageMock)).Returns(transportMessage);
        }

        private void RaiseTopicSubscribed(string topic)
        {
            this.subscriptionManagerMock.RaiseTopicSubscribed(topic);
        }

        private void RaiseTopicUnsubscribed(string topic)
        {
            this.subscriptionManagerMock.RaiseTopicUnsubscribed(topic);
        }

        private void RaiseEventReceived(Mock<IMessageConsumer> topicConsumer, IMessage message)
        {
            topicConsumer.Raise(c => c.Listener += null, message);
        }

        private class NotifyTopicSubscriptionsMock : INotifyTopicSubscriptions
        {
            public event EventHandler<SubscriptionEventArgs> TopicSubscribed = delegate { };
            public event EventHandler<SubscriptionEventArgs> TopicUnsubscribed = delegate { };
            
            public IEnumerable<string> Register(ITopicSubscriptionListener listener)
            {
                this.TopicSubscribed += listener.TopicSubscribed;
                this.TopicUnsubscribed += listener.TopicUnsubscribed;

                return Enumerable.Empty<string>();
            }

            public void Unregister(ITopicSubscriptionListener listener)
            {
                this.TopicSubscribed -= listener.TopicSubscribed;
                this.TopicUnsubscribed -= listener.TopicUnsubscribed;
            }

            public void RaiseTopicSubscribed(string topic)
            {
                this.TopicSubscribed(this, new SubscriptionEventArgs(topic));
            }

            public void RaiseTopicUnsubscribed(string topic)
            {
                this.TopicUnsubscribed(this, new SubscriptionEventArgs(topic));
            }
        }
    }
}