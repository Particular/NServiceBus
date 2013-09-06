namespace NServiceBus.Transports.ActiveMQ.Tests.SessionFactories
{
    using System.Collections.Generic;
    using ActiveMQ.SessionFactories;
    using Apache.NMS;
    using FluentAssertions;
    using Moq;
    using NUnit.Framework;

    [TestFixture]
    public class PooledSessionFactoryTest
    {
        private Mock<IConnectionFactory> connectionFactoryMock;

        private PooledSessionFactory testee;

        private IDictionary<ISession, IConnection> connectionForSession;

        [SetUp]
        public void SetUp()
        {
            connectionForSession = new Dictionary<ISession, IConnection>();
            connectionFactoryMock = new Mock<IConnectionFactory>();
            connectionFactoryMock.Setup(cf => cf.CreateConnection()).Returns(CreateConnectionMock);

            testee = new PooledSessionFactory(connectionFactoryMock.Object);
        }

        [Test]
        public void WhenGettingTwoSession_TheyShouldNotBeSame()
        {
            var session1 = testee.GetSession();
            var session2 = testee.GetSession();

            session1.Should().NotBeSameAs(session2);
        }

        [Test]
        public void WhenGettingTwoSession_EachShouldHaveItsOwnConnection()
        {
            var session1 = testee.GetSession();
            var session2 = testee.GetSession();

            connectionForSession[session1].Should().NotBeSameAs(connectionForSession[session2]);
        }

        [Test]
        public void WhenReleasingASession_ItShouldBeReusedOnNextGetSession()
        {
            var session1 = testee.GetSession();
            testee.Release(session1);
            var session2 = testee.GetSession();

            session1.Should().BeSameAs(session2);
        }

        private IConnection CreateConnectionMock()
        {
            var connectionMock = new Mock<IConnection>();

            connectionMock.Setup(c => c.CreateSession()).Returns(() => CreateSessionMock(connectionMock.Object));

            return connectionMock.Object;
        }

        private ISession CreateSessionMock(IConnection connection)
        {
            var session = new Mock<ISession>().Object;

            connectionForSession[session] = connection;

            return session;
        }
    }
}