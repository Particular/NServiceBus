namespace NServiceBus.Transports.ActiveMQ.Tests.SessionFactories
{
    using System.Threading;
    using System.Threading.Tasks;
    using Apache.NMS;
    using FluentAssertions;
    using NUnit.Framework;
    using NServiceBus.Transports.ActiveMQ.SessionFactories;

    [TestFixture]
    public class ActiveMqTransactionSessionFactoryTest
    {
        private ActiveMqTransactionSessionFactory testee;
        private PooledSessionFactoryMock pooledPooledSessionFactoryMock;

        [SetUp]
        public void SetUp()
        {
            this.pooledPooledSessionFactoryMock = new PooledSessionFactoryMock();
            this.testee = new ActiveMqTransactionSessionFactory(this.pooledPooledSessionFactoryMock);
        }

        [Test]
        public void EachGetSessionShouldRequestASessionFromThePooledSessionFactory()
        {
            var expectedSessions = this.pooledPooledSessionFactoryMock.EnqueueNewSessions(2);

            var session1 = this.testee.GetSession();
            var session2 = this.testee.GetSession();

            session1.Should().BeSameAs(expectedSessions[0]);
            session2.Should().BeSameAs(expectedSessions[1]);
        }

        [Test]
        public void OnReleaseSessionsShouldBeReleasedtoThePooledSessionFactory()
        {
            this.pooledPooledSessionFactoryMock.EnqueueNewSessions(1);

            var session = this.testee.GetSession();
            this.testee.Release(session);

            this.pooledPooledSessionFactoryMock.sessions.Should().Contain(session);
        }

        [Test]
        public void WhenSessionIsPinnedForThread_ItShouldBeReusedOnNextGetSession()
        {
            this.pooledPooledSessionFactoryMock.EnqueueNewSessions(1);

            var session1 = this.testee.GetSession();
            this.testee.SetSessionForCurrentThread(session1);

            var session2 = this.testee.GetSession();

            session1.Should().BeSameAs(session2);
        }

        [Test]
        public void WhenSessionIsUnpinnedForThread_ANewOneShouldBeReturnedOnNextGetSession()
        {
            this.pooledPooledSessionFactoryMock.EnqueueNewSessions(2);

            var session1 = this.testee.GetSession();
            this.testee.SetSessionForCurrentThread(session1);
            this.testee.RemoveSessionForCurrentThread();

            var session2 = this.testee.GetSession();

            session1.Should().NotBeSameAs(session2);
        }

        [Test]
        public void WhenSessionIsPinnedForThread_ANewOneShouldBeReturnedOnAnotherThread()
        {
            this.pooledPooledSessionFactoryMock.EnqueueNewSessions(2);

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
    }
}