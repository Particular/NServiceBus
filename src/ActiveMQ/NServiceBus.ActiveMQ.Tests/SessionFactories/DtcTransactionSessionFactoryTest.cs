namespace NServiceBus.Transports.ActiveMQ.Tests.SessionFactories
{
    using System.Transactions;
    using Apache.NMS;
    using FluentAssertions;
    using NUnit.Framework;
    using NServiceBus.Transports.ActiveMQ.SessionFactories;

    [TestFixture]
    public class DtcTransactionSessionFactoryTest
    {
        private DTCTransactionSessionFactory testee;
        private PooledSessionFactoryMock pooledPooledSessionFactoryMock;

        [SetUp]
        public void SetUp()
        {
            this.pooledPooledSessionFactoryMock = new PooledSessionFactoryMock();
            this.testee = new DTCTransactionSessionFactory(this.pooledPooledSessionFactoryMock);
        }

        [Test]
        public void WhenSessionIsRequested_OneFromThePoolesSessionFactoryIsReturned()
        {
            var expectedSessions = this.pooledPooledSessionFactoryMock.EnqueueNewSessions(1);

            var session = this.testee.GetSession();

            session.Should().BeSameAs(expectedSessions[0]);
        }

        [Test]
        public void WhenSessionIsReleased_ItIsReturnedToThePooledSessionFactory()
        {
            this.pooledPooledSessionFactoryMock.EnqueueNewSessions(1);

            var session = this.testee.GetSession();
            this.testee.Release(session);

            this.pooledPooledSessionFactoryMock.sessions.Should().Contain(session);
        }
        
        [Test]
        public void GetSession_WhenInTransaction_ThenSameSessionIsUsed()
        {
            this.pooledPooledSessionFactoryMock.EnqueueNewSessions(1);

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
            this.pooledPooledSessionFactoryMock.EnqueueNewSessions(2);

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
            this.pooledPooledSessionFactoryMock.EnqueueNewSessions(1);

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
    }
}
