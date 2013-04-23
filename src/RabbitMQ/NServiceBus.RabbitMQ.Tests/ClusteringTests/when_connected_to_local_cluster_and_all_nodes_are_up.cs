namespace NServiceBus.Transports.RabbitMQ.Tests.ClusteringTests
{
    using FluentAssertions;
    using NUnit.Framework;

    [TestFixture]
    [Category(TestCategory.Integration)]
    [Explicit("Long running test")]
    public class when_connected_to_local_cluster_and_all_nodes_are_up : ClusteredTestContext
    {
        TransportMessage sentMessage;
        TransportMessage receivedMessage;

        [TestFixtureSetUp]
        public void TestFixtureSetup() {
            // arrange
            var connectionString = GetConnectionString();
            SetupQueueAndSenderAndListener(connectionString);

            // act
            receivedMessage = SendAndReceiveAMessage(out sentMessage);
        }

        [Test]
        public void it_should_be_able_to_roundtrip_a_message() {
            receivedMessage.Should().NotBeNull();
            receivedMessage.Id.Should().Be(sentMessage.Id);
        }
    }
}