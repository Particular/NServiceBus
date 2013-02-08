namespace NServiceBus.Transport.ActiveMQ.Receivers.TransactionScopes
{
    using System;
    using System.Transactions;
    using FluentAssertions;
    using NServiceBus.Transport.ActiveMQ.Receivers.TransactonsScopes;
    using NUnit.Framework;

    [TestFixture]
    public class DTCTransactionScopeTests
    {
        [Test]
        public void WhenCreated_ThenNewTransactionIsStarted()
        {
            using (var tx = new DTCTransactionScope(null, new TransactionOptions()))
            {
                Transaction.Current.Should().NotBeNull();

                tx.Complete();
            }
        }

        [Test]
        public void WhenCompleted_ThenTransactionShouldBeCommited()
        {
            var transactionStatus = TransactionStatus.InDoubt;

            using (var tx = new DTCTransactionScope(null, new TransactionOptions()))
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
                    using (var tx = new DTCTransactionScope(null, new TransactionOptions()))
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