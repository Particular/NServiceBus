namespace NServiceBus.Transports.ActiveMQ.Tests.SessionFactories
{
    using System.Collections.Generic;
    using Apache.NMS;
    using FluentAssertions;
    using Moq;
    using NUnit.Framework;
    using NServiceBus.Transports.ActiveMQ.SessionFactories;

    [TestFixture]
    public class PooledSessionFactoryTest
    {
        private Mock<IConnectionFactory> connectionFactoryMock;

        private PooledSessionFactory testee;

        private IDictionary<ISession, IConnection> connectionForSession;

        [SetUp]
        public void SetUp()
        {
            this.connectionForSession = new Dictionary<ISession, IConnection>();
            this.connectionFactoryMock = new Mock<IConnectionFactory>();
            this.connectionFactoryMock.Setup(cf => cf.CreateConnection()).Returns(this.CreateConnectionMock);

            this.testee = new PooledSessionFactory(this.connectionFactoryMock.Object);
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

            this.connectionForSession[session1].Should().NotBeSameAs(this.connectionForSession[session2]);
        }

        [Test]
        public void WhenReleasingASession_ItShouldBeReusedOnNextGetSession()
        {
            var session1 = this.testee.GetSession();
            this.testee.Release(session1);
            var session2 = this.testee.GetSession();

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