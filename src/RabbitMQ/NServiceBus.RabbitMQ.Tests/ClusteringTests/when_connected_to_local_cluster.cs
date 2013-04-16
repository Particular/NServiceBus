namespace NServiceBus.Transports.RabbitMQ.Tests.ClusteringTests
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using FluentAssertions;
    using NServiceBus;
    using NUnit.Framework;

//    [TestFixture, Category(TestCategory.Integration)]
//    public class when_connected_to_local_cluster : TestContext
//    {
//        TransportMessage roundTrippedMessage;
//        TransportMessage message;
//        const string ConnectionString = "host=JustinT-Station:5673,JustinT-Station:5674,JustinT-Station:5675";
//        const string QueueName = "testreceiver";
//
//        [TestFixtureSetUp]
//        public void TestFixtureSetup() {
//            // arrange
//            SetupQueueAndSenderAndListener(ConnectionString, QueueName);
//            var address = Address.Parse(QueueName);
//            message = new TransportMessage();
//
//            // act
//            sender.Send(message, address);
//            roundTrippedMessage = WaitForMessage();            
//        }
//
//        [Test]
//        public void it_should_be_able_to_roundtrip_a_message() {
//            roundTrippedMessage.Should().NotBeNull();
//        }
//
//        [Test]
//        public void it_should_have_roundtripped_the_expected_message() {
//            roundTrippedMessage.Id.Should().Be(message.Id);
//        }
//    }
//
//    [TestFixture, Category(TestCategory.Integration)]
//    public class when_connected_to_local_cluster_and_first_node_is_unavailable : TestContext
//    {
//        TransportMessage roundTrippedMessage;
//        TransportMessage message;
//        const string ConnectionString = "host=JustinT-Station:5673,JustinT-Station:5674,JustinT-Station:5675";
//        const string QueueName = "testreceiver";
//
//        [TestFixtureSetUp]
//        public void TestFixtureSetup() {
//            // arrange
//            // TODO: stop node 1 manually
//            SetupQueueAndSenderAndListener(ConnectionString, QueueName);
//            var address = Address.Parse(QueueName);
//            message = new TransportMessage();
//
//            // act
//            sender.Send(message, address);
//            roundTrippedMessage = WaitForMessage();            
//        }
//
//        [Test]
//        public void it_should_be_able_to_roundtrip_a_message() {
//            roundTrippedMessage.Should().NotBeNull();
//        }
//
//        [Test]
//        public void it_should_have_roundtripped_the_expected_message() {
//            roundTrippedMessage.Id.Should().Be(message.Id);
//        }
//    }
//
    [TestFixture, Category(TestCategory.Integration)]
    public class when_connected_to_local_cluster_and_first_node_goes_down_while_working_against_it : TestContext
    {
        TransportMessage messageWhenAllNodesUp;
        TransportMessage messageWhen1NodeIsDown;
        TransportMessage messageWhen2NodesAreDown;

        [TestFixtureSetUp]
        public void TestFixtureSetup() {
            // arrange
            var connectionString = GetConnectionString();
            SetupQueueAndSenderAndListener(connectionString);

            // act
            SendAndReceiveAMessage();
            messageWhenAllNodesUp = SendAndReceiveAMessage();
            Logger.Warn("Stopping node 1");
            InvokeRabbitMqCtl(RabbitNodes[1], "stop_app");
            messageWhen1NodeIsDown = SendAndReceiveAMessage();
            Logger.Warn("Stopping node 2");
            InvokeRabbitMqCtl(RabbitNodes[2], "stop_app");
            messageWhen2NodesAreDown = SendAndReceiveAMessage();
        }

        [Test]
        public void it_should_be_able_to_roundtrip_a_message_when_all_nodes_are_up() {
            messageWhenAllNodesUp.Should().NotBeNull();
        }

        [Test]
        public void it_should_be_able_to_roundtrip_a_message_when_node_1_is_down() {
            messageWhen1NodeIsDown.Should().NotBeNull();
        }

        [Test]
        public void it_should_be_able_to_roundtrip_a_message_when_nodes_1_and_2_are_down() {
            messageWhen2NodesAreDown.Should().NotBeNull();
        }
    }
}