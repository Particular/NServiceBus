﻿namespace NServiceBus.ActiveMQ
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
        private Mock<INetTxConnectionFactory> connectionFactoryMock;
        private SessionFactory testee;

        private IDictionary<INetTxSession, INetTxConnection> connectionForSession;

        [SetUp]
        public void SetUp()
        {
            this.connectionForSession = new Dictionary<INetTxSession, INetTxConnection>();
            this.connectionFactoryMock = new Mock<INetTxConnectionFactory>();
            this.connectionFactoryMock.Setup(cf => cf.CreateNetTxConnection()).Returns(this.CreateConnectionMock);

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
            INetTxSession session2 = null;
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
            INetTxSession session1;
            INetTxSession session2;

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
            INetTxSession session1;
            INetTxSession session2;

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
            INetTxSession session1;
            INetTxSession session2;
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
        
        private INetTxConnection CreateConnectionMock()
        {
            var connectionMock = new Mock<INetTxConnection>();
            
            connectionMock.Setup(c => c.CreateNetTxSession()).Returns(() => this.CreateSessionMock(connectionMock.Object));

            return connectionMock.Object;
        }

        private INetTxSession CreateSessionMock(INetTxConnection connection)
        {
            var session = new Mock<INetTxSession>().Object;

            this.connectionForSession[session] = connection;

            return session;
        }
    }
}
