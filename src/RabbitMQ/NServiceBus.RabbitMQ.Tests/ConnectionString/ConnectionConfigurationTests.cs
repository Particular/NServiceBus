namespace NServiceBus.Transports.RabbitMQ.Tests.ConnectionString
{
    using System.Linq;
    using Config;
    using NUnit.Framework;

    [TestFixture]
    public class ConnectionConfigurationTests
    {
        #region Setup/Teardown

        [SetUp]
        public void Setup() {
            parser = new ConnectionStringParser();
            defaults = new ConnectionConfiguration();
        }

        #endregion

        ConnectionConfiguration defaults;
        ConnectionStringParser parser;
        string connectionString;
        IConnectionConfiguration connectionConfiguration;

        [Test]
        public void Should_default_the_port_if_not_set() {
            connectionString = ("host=myHost");
            connectionConfiguration = parser.Parse(connectionString);
            Assert.AreEqual(ConnectionConfiguration.DefaultPort, connectionConfiguration.Hosts.First().Port);
        }

        [Test]
        public void Should_default_the_prefetch_count() {
            connectionString = ("host=localhost");
            connectionConfiguration = parser.Parse(connectionString);
            Assert.AreEqual(ConnectionConfiguration.DefaultPrefetchCount, connectionConfiguration.PrefetchCount);
        }

        [Test]
        public void Should_default_the_requested_heartbeat() {
            connectionString = ("host=localhost");
            connectionConfiguration = parser.Parse(connectionString);
            Assert.AreEqual(ConnectionConfiguration.DefaultHeartBeatInSeconds, connectionConfiguration.RequestedHeartbeat);
        }

        [Test]
        public void Should_set_default_password() {
Assert.AreEqual(            defaults.Password,"guest");
        }

        [Test]
        public void Should_set_default_port() {
Assert.AreEqual(            defaults.Port,5672);
        }

        [Test]
        public void Should_set_default_username() {
Assert.AreEqual(            defaults.UserName,"guest");
        }

        [Test]
        public void Should_set_default_virtual_host() {
Assert.AreEqual(            defaults.VirtualHost,"/");
        }
    }
}