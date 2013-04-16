namespace NServiceBus.Transports.RabbitMQ.Tests.ClusteringTests
{
    using System;
    using System.Diagnostics;
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
        public void it_should_be_able_to_roundtrip_a_message() {
            roundTrippedMessage.Should().NotBeNull();
        }

        [Test]
        public void it_should_have_roundtripped_the_expected_message() {
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
            // TODO: stop node 1 manually
            DoSetup(ConnectionString, QueueName);
            var address = Address.Parse(QueueName);
            message = new TransportMessage();

            // act
            sender.Send(message, address);
            roundTrippedMessage = WaitForMessage();            
        }

        [Test]
        public void it_should_be_able_to_roundtrip_a_message() {
            roundTrippedMessage.Should().NotBeNull();
        }

        [Test]
        public void it_should_have_roundtripped_the_expected_message() {
            roundTrippedMessage.Id.Should().Be(message.Id);
        }
    }

    [TestFixture, Category(TestCategory.Integration)]
    public class when_connected_to_local_cluster_and_first_node_goes_down_while_working_against_it : RabbitMqContext
    {
        readonly string machineName = Environment.MachineName;
        TransportMessage roundTrippedMessage;
        TransportMessage message;
        const string QueueName = "testreceiver";

        [TestFixtureSetUp]
        public void TestFixtureSetup() {
            // arrange
            string connectionString = string.Format("host={0}:5673,{0}:5674,{0}:5675",machineName);
            DoSetup(connectionString, QueueName);
            var address = Address.Parse(QueueName);
            message = new TransportMessage();

            // act
            Logger.Info("Sending message 1");
            message = new TransportMessage();
            sender.Send(message, address);
            roundTrippedMessage = WaitForMessage();
            // reset
            Logger.Info("Sending message 2");
            roundTrippedMessage = null; 
            message = new TransportMessage();
            sender.Send(message, address);
            roundTrippedMessage = WaitForMessage();
            // stop node 1
            Logger.Warn("Stopping node 1");
            var args = string.Format("-n rabbit1@{0} stop_app", machineName);
            var output = RunRabbitMqCtl(args);
            Logger.Info("Stopped node 1: {0}",output);
            
            // reset
            Logger.Info("Sending message 3");
            roundTrippedMessage = null; 
            message = new TransportMessage();   
            sender.Send(message, address);
            roundTrippedMessage = WaitForMessage();            

            // clean up
            args = string.Format("-n rabbit1@{0} start_app", machineName);
            output = RunRabbitMqCtl(args);
            Logger.Info("Restarted node 1: {0}",output);
        }

        static string RunRabbitMqCtl(string args) {
            string program = @"C:\Program Files (x86)\RabbitMQ Server\rabbitmq_server-3.0.2\sbin\rabbitmqctl.bat";
            Process p = new Process {StartInfo = {UseShellExecute = false, RedirectStandardOutput = true, FileName = program, Arguments = args}};
            p.Start();
            // Read the output stream first and then wait.
            string output = p.StandardOutput.ReadToEnd().Replace("\n", ";");
            p.WaitForExit();
            return output;
        }

        [Test]
        public void it_should_be_able_to_roundtrip_a_message() {
            roundTrippedMessage.Should().NotBeNull();
        }

        [Test]
        public void it_should_have_roundtripped_the_expected_message() {
            roundTrippedMessage.Id.Should().Be(message.Id);
        }
    }
}