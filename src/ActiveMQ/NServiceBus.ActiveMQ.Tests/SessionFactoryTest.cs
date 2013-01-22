namespace NServiceBus.ActiveMQ
{
    using System.Collections.Generic;

    using System.Threading;
    using System.Threading.Tasks;
    using System.Transactions;

    using Apache.NMS;
    using FluentAssertions;
    using Moq;
    using NServiceBus.Transport.ActiveMQ;
    using NUnit.Framework;

    [TestFixture]
    public class SessionFactoryTest
    {
        private Mock<IConnectionFactory> connectionFactoryMock;
        private SessionFactory testee;

        private IDictionary<ISession, IConnection> connectionForSession;

        [SetUp]
        public void SetUp()
        {
            this.connectionForSession = new Dictionary<ISession, IConnection>();
            this.connectionFactoryMock = new Mock<IConnectionFactory>();
            this.connectionFactoryMock.Setup(cf => cf.CreateConnection()).Returns(this.CreateConnectionMock);

            this.testee = new SessionFactory(this.connectionFactoryMock.Object);
        }

        [Test]
        public void WhenGettingTwoSession_TheyShouldNotBeSame()
        {
            var session1 = this.testee.GetSession();
            var session2 = this.testee.GetSession();

            session1.Should().NotBeSameAs(session2);
        }

        [Test]
        public void WhenGettingTwoSession_EachShouldHaveItsOwnConnection()
        {
            var session1 = this.testee.GetSession();
            var session2 = this.testee.GetSession();

            connectionForSession[session1].Should().NotBeSameAs(connectionForSession[session2]);
        }
        
        [Test]
        public void WhenReleasingASession_ItShouldBeReusedOnNextGetSession()
        {
            var session1 = this.testee.GetSession();
            this.testee.Release(session1);
            var session2 = this.testee.GetSession();

            session1.Should().BeSameAs(session2);
        }

        [Test]
        public void WhenSessionIsPinnedForThread_ItShouldBeReusedOnNextGetSession()
        {
            var session1 = this.testee.GetSession();
            this.testee.SetSessionForCurrentThread(session1);
            var session2 = this.testee.GetSession();

            session1.Should().BeSameAs(session2);
        }

        [Test]
        public void WhenSessionIsUnpinnedForThread_ANewOneShouldBeReturnedOnNextGetSession()
        {
            var session1 = this.testee.GetSession();
            this.testee.SetSessionForCurrentThread(session1);
            this.testee.RemoveSessionForCurrentThread();
            var session2 = this.testee.GetSession();

            session1.Should().NotBeSameAs(session2);
        }

        [Test]
        public void WhenSessionIsPinnedForThread_ANewOneShouldBeReturnedOnAnotherThread()
        {
            ISession session2 = null;
            var autoResetEvent = new AutoResetEvent(false);

            var session1 = this.testee.GetSession();
            this.testee.SetSessionForCurrentThread(session1);


            Task.Factory.StartNew(
                () =>
                {
                    session2 = this.testee.GetSession();
                    autoResetEvent.Set();
                });

            autoResetEvent.WaitOne(1000).Should().BeTrue(reason: "Task was not finished!");
            session1.Should().NotBeSameAs(session2);
        }

        [Test]
        public void GetSession_WhenInTransaction_ThenSameSessionIsUsed()
        {
            ISession session1;
            ISession session2;

            using (var tx = new TransactionScope())
            {
                session1 = this.testee.GetSession();
                this.testee.Release(session1);

                session2 = this.testee.GetSession();
                this.testee.Release(session2);

                tx.Complete();
            }

            session1.Should().BeSameAs(session2);
        }

        [Test]
        public void GetSession_WhenInDifferentTransaction_ThenDifferentSessionAreUsed()
        {
            ISession session1;
            ISession session2;

            using (var tx1 = new TransactionScope())
            {
                session1 = this.testee.GetSession();
                this.testee.Release(session1);

                using (var tx2 = new TransactionScope(TransactionScopeOption.RequiresNew))
                {
                    session2 = this.testee.GetSession();
                    this.testee.Release(session2);

                    tx2.Complete();
                }

                tx1.Complete();
            }

            session1.Should().NotBeSameAs(session2);
        }

        [Test]
        public void GetSession_WhenInDifferentCompletedTransaction_ThenSessionIsReused()
        {
            ISession session1;
            ISession session2;
            using (var tx1 = new TransactionScope())
            {
                session1 = this.testee.GetSession();
                this.testee.Release(session1);

                tx1.Complete();
            }

            using (var tx2 = new TransactionScope())
            {
                session2 = this.testee.GetSession();
                this.testee.Release(session2);

                tx2.Complete();
            }

            session1.Should().BeSameAs(session2);
        }

        private IConnection CreateConnectionMock()
        {
            var connectionMock = new Mock<IConnection>();
            
            connectionMock.Setup(c => c.CreateSession()).Returns(() => this.CreateSessionMock(connectionMock.Object));

            return connectionMock.Object;
        }

        private ISession CreateSessionMock(IConnection connection)
        {
            var session = new Mock<ISession>().Object;

            this.connectionForSession[session] = connection;

            return session;
        }
    }
}
