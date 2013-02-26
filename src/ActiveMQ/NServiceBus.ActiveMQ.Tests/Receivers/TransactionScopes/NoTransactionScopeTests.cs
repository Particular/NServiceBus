namespace NServiceBus.Transports.ActiveMQ.Tests.Receivers.TransactionScopes
{
    using Apache.NMS;
    using Moq;
    using NUnit.Framework;
    using NServiceBus.Transports.ActiveMQ.Receivers.TransactonsScopes;

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