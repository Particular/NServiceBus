namespace NServiceBus.Transports.ActiveMQ.Tests.SessionFactories
{
    using System.Threading;
    using System.Threading.Tasks;
    using ActiveMQ.SessionFactories;
    using Apache.NMS;
    using FluentAssertions;
    using NUnit.Framework;

    [TestFixture]
    public class ActiveMqTransactionSessionFactoryTest
    {
        private ActiveMqTransactionSessionFactory testee;
        private PooledSessionFactoryMock pooledPooledSessionFactoryMock;

        [SetUp]
        public void SetUp()
        {
            pooledPooledSessionFactoryMock = new PooledSessionFactoryMock();
            testee = new ActiveMqTransactionSessionFactory(pooledPooledSessionFactoryMock);
        }

        [Test]
        public void EachGetSessionShouldRequestASessionFromThePooledSessionFactory()
        {
            var expectedSessions = pooledPooledSessionFactoryMock.EnqueueNewSessions(2);

            var session1 = testee.GetSession();
            var session2 = testee.GetSession();

            session1.Should().BeSameAs(expectedSessions[0]);
            session2.Should().BeSameAs(expectedSessions[1]);
        }

        [Test]
        public void OnReleaseSessionsShouldBeReleasedtoThePooledSessionFactory()
        {
            pooledPooledSessionFactoryMock.EnqueueNewSessions(1);

            var session = testee.GetSession();
            testee.Release(session);

            pooledPooledSessionFactoryMock.sessions.Should().Contain(session);
        }

        [Test]
        public void WhenSessionIsPinnedForThread_ItShouldBeReusedOnNextGetSession()
        {
            pooledPooledSessionFactoryMock.EnqueueNewSessions(1);

            var session1 = testee.GetSession();
            testee.SetSessionForCurrentThread(session1);

            var session2 = testee.GetSession();

            session1.Should().BeSameAs(session2);
        }

        [Test]
        public void WhenSessionIsUnpinnedForThread_ANewOneShouldBeReturnedOnNextGetSession()
        {
            pooledPooledSessionFactoryMock.EnqueueNewSessions(2);

            var session1 = testee.GetSession();
            testee.SetSessionForCurrentThread(session1);
            testee.RemoveSessionForCurrentThread();

            var session2 = testee.GetSession();

            session1.Should().NotBeSameAs(session2);
        }

        [Test]
        public void WhenSessionIsPinnedForThread_ANewOneShouldBeReturnedOnAnotherThread()
        {
            pooledPooledSessionFactoryMock.EnqueueNewSessions(2);

            ISession session2 = null;
            var autoResetEvent = new AutoResetEvent(false);

            var session1 = testee.GetSession();
            testee.SetSessionForCurrentThread(session1);


            Task.Factory.StartNew(
                () =>
                    {
                        session2 = testee.GetSession();
                        autoResetEvent.Set();
                    });

            autoResetEvent.WaitOne(1000).Should().BeTrue(reason: "Task was not finished!");
            session1.Should().NotBeSameAs(session2);
        }
    }
}