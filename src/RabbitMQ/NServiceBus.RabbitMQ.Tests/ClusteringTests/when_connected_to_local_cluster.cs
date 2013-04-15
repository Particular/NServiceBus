namespace NServiceBus.Transports.RabbitMQ.Tests.ClusteringTests
{
    using FluentAssertions;
    using NServiceBus;
    using NUnit.Framework;

    [TestFixture, Category(TestCategory.Integration)]
    public class when_connected_to_local_cluster : RabbitMqContext
    {
        TransportMessage roundTrippedMessage;
        TransportMessage message;
        const string ConnectionString = "host=JustinT-Station:5673,JustinT-Station:5674,JustinT-Station:5675";
        const string QueueName = "testreceiver";

        [TestFixtureSetUp]
        public void TestFixtureSetup() {
            // arrange
            DoSetup(ConnectionString, QueueName);
            var address = Address.Parse(QueueName);
            message = new TransportMessage();

            // act
            sender.Send(message, address);
            roundTrippedMessage = WaitForMessage();            
        }

        [Test]
        public void should_be_able_to_roundtrip_a_message() {
            roundTrippedMessage.Should().NotBeNull();
        }

        [Test]
        public void should_have_roundtripped_the_expected_message() {
            roundTrippedMessage.Id.Should().Be(message.Id);
        }
    }

    [TestFixture, Category(TestCategory.Integration)]
    public class when_connected_to_local_cluster_and_first_node_is_unavailable : RabbitMqContext
    {
        TransportMessage roundTrippedMessage;
        TransportMessage message;
        const string ConnectionString = "host=JustinT-Station:5673,JustinT-Station:5674,JustinT-Station:5675";
        const string QueueName = "testreceiver";

        [TestFixtureSetUp]
        public void TestFixtureSetup() {
            // arrange
            DoSetup(ConnectionString, QueueName);
            var address = Address.Parse(QueueName);
            message = new TransportMessage();

            // act
            sender.Send(message, address);
            roundTrippedMessage = WaitForMessage();            
        }

        [Test]
        public void should_be_able_to_roundtrip_a_message() {
            roundTrippedMessage.Should().NotBeNull();
        }

        [Test]
        public void should_have_roundtripped_the_expected_message() {
            roundTrippedMessage.Id.Should().Be(message.Id);
        }
    }
}