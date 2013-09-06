namespace NServiceBus.Transports.ActiveMQ.Tests.Receivers.TransactionScopes
{
    using ActiveMQ.Receivers.TransactionsScopes;
    using ActiveMQ.SessionFactories;
    using Apache.NMS;
    using Moq;
    using NUnit.Framework;

    [TestFixture]
    public class ActiveMqTransactionTests
    {
        [Test]
        public void WhenCreated_ThenSessionIsSetForCurrentThread()
        {
            var sessionFactoryMock = new Mock<ISessionFactory>();
            var session = new Mock<ISession>().Object;

            var testee = new ActiveMqTransaction(sessionFactoryMock.Object, session);

            sessionFactoryMock.Verify(sf => sf.SetSessionForCurrentThread(session));
            sessionFactoryMock.Verify(sf => sf.RemoveSessionForCurrentThread(), Times.Never());
        }

        [Test]
        public void WhenDisposed_ThenSessionIsReleasedForCurrentThread()
        {
            var sessionFactoryMock = new Mock<ISessionFactory>();
            var session = new Mock<ISession>().Object;

            var testee = new ActiveMqTransaction(sessionFactoryMock.Object, session);
            testee.Dispose();

            sessionFactoryMock.Verify(sf => sf.RemoveSessionForCurrentThread());
        }

        [Test]
        public void WhenCompletedAndDisposed_ThenSessionIsCommit_AndNotRollbacked()
        {
            var sessionFactoryMock = new Mock<ISessionFactory>();
            var sessionMock = new Mock<ISession>();

            var testee = new ActiveMqTransaction(sessionFactoryMock.Object, sessionMock.Object);
            testee.Complete();
            testee.Dispose();

            sessionMock.Verify(s => s.Commit());
            sessionMock.Verify(s => s.Rollback(), Times.Never());
        }

        [Test]
        public void WhenDisposed_ThenSessionIsRollbacked()
        {
            var sessionFactoryMock = new Mock<ISessionFactory>();
            var sessionMock = new Mock<ISession>();

            var testee = new ActiveMqTransaction(sessionFactoryMock.Object, sessionMock.Object);
            testee.Dispose();

            sessionMock.Verify(s => s.Rollback());
            sessionMock.Verify(s => s.Commit(), Times.Never());
        }
    }
}
