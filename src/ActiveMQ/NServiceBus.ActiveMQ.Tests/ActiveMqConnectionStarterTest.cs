namespace NServiceBus.ActiveMQ
{
    using Apache.NMS;

    using Moq;

    using NUnit.Framework;

    [TestFixture]
    public class ActiveMqConnectionStarterTest
    {
        private Mock<INetTxConnection> connection;

        private ActiveMqConnectionStarter testee;

        [SetUp]
        public void SetUp()
        {
            this.connection = new Mock<INetTxConnection>();

            this.testee = new ActiveMqConnectionStarter { Connection = this.connection.Object };
        }

        [Test]
        public void Start_ShouldStartConnection()
        {
            this.testee.Start();

            this.connection.Verify(c => c.Start());
        }

        [Test]
        public void Stop_ShouldStopAndDisposeConnection()
        {
            this.testee.Stop();

            this.connection.Verify(c => c.Stop());
            this.connection.Verify(c => c.Dispose());
        }
    }
}