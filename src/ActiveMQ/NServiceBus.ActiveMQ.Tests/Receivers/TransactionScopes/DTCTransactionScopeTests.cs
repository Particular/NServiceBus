namespace NServiceBus.Transports.ActiveMQ.Tests.Receivers.TransactionScopes
{
    using System;
    using System.Transactions;
    using FluentAssertions;
    using Moq;

    using NServiceBus.Transports.ActiveMQ.Receivers.TransactionsScopes;

    using NUnit.Framework;

    using NServiceBus.Transports.ActiveMQ.SessionFactories;

    [TestFixture]
    public class DTCTransactionScopeTests
    {
        private Mock<ISessionFactory> sesstionFactoryMock;

        [SetUp]
        public void SetUp()
        {
            this.sesstionFactoryMock = new Mock<ISessionFactory>();
        }

        [Test]
        public void WhenCreated_ThenNewTransactionIsStarted()
        {
            using (var tx = new DTCTransactionScope(null, new TransactionOptions(), this.sesstionFactoryMock.Object))
            {
                Transaction.Current.Should().NotBeNull();

                tx.Complete();
            }
        }

        [Test]
        public void WhenCompleted_ThenTransactionShouldBeCommited()
        {
            var transactionStatus = TransactionStatus.InDoubt;

            using (var tx = new DTCTransactionScope(null, new TransactionOptions(), this.sesstionFactoryMock.Object))
            {
                Transaction.Current.TransactionCompleted +=
                    (s, e) => transactionStatus = e.Transaction.TransactionInformation.Status;

                tx.Complete();
            }

            transactionStatus.Should().Be(TransactionStatus.Committed);
        }

        [Test]
        public void WhenDisposedButNotCommited_ThenTransactionShouldBeAbortedAndExceptionThrown()
        {
            var transactionStatus = TransactionStatus.InDoubt;

            Action action = () =>
                {
                    using (var tx = new DTCTransactionScope(null, new TransactionOptions(), this.sesstionFactoryMock.Object))
                    {
                        Transaction.Current.TransactionCompleted +=
                            (s, e) => transactionStatus = e.Transaction.TransactionInformation.Status;
                    }
                };

            action.ShouldThrow<Exception>();
            transactionStatus.Should().Be(TransactionStatus.Aborted);
        }
    }
}