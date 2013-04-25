namespace NServiceBus.Transports.ActiveMQ.Tests.Receivers.TransactionScopes
{
    using Apache.NMS;
    using Moq;

    using NServiceBus.Transports.ActiveMQ.Receivers.TransactionsScopes;

    using NUnit.Framework;

    [TestFixture]
    public class NoTransactionScopeTests
    {
        [Test]
        public void WhenMessageIsAccepted_ThisItIsAcknowledged()
        {
            var messageMock = new Mock<IMessage>();
            var testee = new NoTransactionScope();

            testee.MessageAccepted(messageMock.Object);

            messageMock.Verify(m => m.Acknowledge());
        }
    }
}