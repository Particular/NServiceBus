namespace NServiceBus.Transports.ActiveMQ.Tests.Receivers
{
    using Apache.NMS;
    using Moq;
    using NUnit.Framework;
    using NServiceBus.Transports.ActiveMQ.Receivers;

    public class ActiveMqPurgerTest
    {
        private Mock<ISession> session;

        private Mock<IDestination> destination;

        private ActiveMqPurger testee;

        [SetUp]
        public void SetUp()
        {
            session = new Mock<ISession>();
            destination = new Mock<IDestination>();

            testee = new ActiveMqPurger();
        }

        [Test]
        public void Purge_WhenAlreadyPurged_ThenDontPurgeAgain()
        {
            testee.Purge(session.Object, destination.Object);

            testee.Purge(session.Object, destination.Object);

            session.Verify(s => s.DeleteDestination(destination.Object), Times.Once());
        }

        [Test]
        public void Purge_WhenNotYetPurged_ThenPurge()
        {
            testee.Purge(session.Object, destination.Object);

            session.Verify(s => s.DeleteDestination(destination.Object));
        }
    }
}