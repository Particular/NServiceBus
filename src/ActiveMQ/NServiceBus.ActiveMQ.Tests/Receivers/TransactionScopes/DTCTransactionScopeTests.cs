namespace NServiceBus.Transports.ActiveMQ.Tests.Receivers.TransactionScopes
{
    using System;
    using System.Transactions;
    using ActiveMQ.Receivers.TransactionsScopes;
    using ActiveMQ.SessionFactories;
    using FluentAssertions;
    using Moq;
    using NUnit.Framework;

    [TestFixture]
    public class DTCTransactionScopeTests
    {
        private Mock<ISessionFactory> sesstionFactoryMock;

        [SetUp]
        public void SetUp()
        {
            sesstionFactoryMock = new Mock<ISessionFactory>();
        }

        [Test]
        public void WhenCreated_ThenNewTransactionIsStarted()
        {
            using (var tx = new DTCTransactionScope(null, new TransactionOptions(), sesstionFactoryMock.Object))
            {
                Transaction.Current.Should().NotBeNull();

                tx.Complete();
            }
        }

        [Test]
        public void WhenCompleted_ThenTransactionShouldBeCommited()
        {
            var transactionStatus = TransactionStatus.InDoubt;

            using (var tx = new DTCTransactionScope(null, new TransactionOptions(), sesstionFactoryMock.Object))
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
                    using (var tx = new DTCTransactionScope(null, new TransactionOptions(), sesstionFactoryMock.Object))
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