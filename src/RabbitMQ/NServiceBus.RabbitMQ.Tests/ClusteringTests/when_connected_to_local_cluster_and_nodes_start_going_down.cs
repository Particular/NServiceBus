namespace NServiceBus.Transports.RabbitMQ.Tests.ClusteringTests
{
    using FluentAssertions;
    using NUnit.Framework;

    [TestFixture]
    [Category(TestCategory.Integration)]
    [Explicit("Long running test")]
    public class when_connected_to_local_cluster_and_nodes_start_going_down : ClusteredTestContext
    {
        TransportMessage messageSentWhenAllNodesUp;
        TransportMessage messageSentWhen1NodeIsDown;
        TransportMessage messageSentWhen2NodesAreDown;
        TransportMessage messageReceivedWhenAllNodesUp;
        TransportMessage messageReceivedWhen1NodeIsDown;
        TransportMessage messageReceivedWhen2NodesAreDown;

        [TestFixtureSetUp]
        public void TestFixtureSetup() {
            // arrange
            var connectionString = GetConnectionString();
            SetupQueueAndSenderAndListener(connectionString);

            // act
            SendAndReceiveAMessage();
            messageReceivedWhenAllNodesUp = SendAndReceiveAMessage(out messageSentWhenAllNodesUp);
            StopNode(1);
            messageReceivedWhen1NodeIsDown = SendAndReceiveAMessage(out messageSentWhen1NodeIsDown);
            StopNode(2);
            messageReceivedWhen2NodesAreDown = SendAndReceiveAMessage(out messageSentWhen2NodesAreDown);
        }

        [Test]
        public void it_should_be_able_to_roundtrip_a_message_when_all_nodes_are_up() {
            messageReceivedWhenAllNodesUp.Should().NotBeNull();
            messageReceivedWhenAllNodesUp.Id.Should().Be(messageSentWhenAllNodesUp.Id);
        }

        [Test]
        public void it_should_be_able_to_roundtrip_a_message_when_node_1_is_down() {
            messageReceivedWhen1NodeIsDown.Should().NotBeNull();
            messageReceivedWhen1NodeIsDown.Id.Should().Be(messageSentWhen1NodeIsDown.Id);
        }

        [Test]
        public void it_should_be_able_to_roundtrip_a_message_when_nodes_1_and_2_are_down() {
            messageReceivedWhen2NodesAreDown.Should().NotBeNull();
            messageReceivedWhen2NodesAreDown.Id.Should().Be(messageSentWhen2NodesAreDown.Id);
        }
    }
}