namespace NServiceBus.Transport.ActiveMQ.Receivers.TransactionScopes
{
    using Apache.NMS;

    using Moq;

    using NServiceBus.Transport.ActiveMQ.Receivers.TransactonsScopes;

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