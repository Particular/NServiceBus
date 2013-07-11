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
            this.session = new Mock<ISession>();
            this.destination = new Mock<IDestination>();

            this.testee = new ActiveMqPurger();
        }

        [Test]
        public void Purge_WhenAlreadyPurged_ThenDontPurgeAgain()
        {
            this.testee.Purge(this.session.Object, this.destination.Object);

            this.testee.Purge(this.session.Object, this.destination.Object);

            this.session.Verify(s => s.DeleteDestination(this.destination.Object), Times.Once());
        }

        [Test]
        public void Purge_WhenNotYetPurged_ThenPurge()
        {
            this.testee.Purge(this.session.Object, this.destination.Object);

            this.session.Verify(s => s.DeleteDestination(this.destination.Object));
        }
    }
}