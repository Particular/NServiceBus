namespace NServiceBus.Transports.RabbitMQ.Tests.ConnectionString
{
    using System;
    using System.Linq;
    using EasyNetQ;
    using NUnit.Framework;
    using NServiceBus.Transports.RabbitMQ.Config;

    [TestFixture]
    public class ConnectionStringParserTests
    {
        ConnectionStringParser parser;
        string connectionString;
        IConnectionConfiguration connectionConfiguration;

        [SetUp]
        public void Setup() {
            parser = new ConnectionStringParser();
        }

        [Test]
        public void Should_correctly_parse_full_connection_string()
        {
            connectionString = "virtualHost=Copa;username=Copa;host=192.168.1.1:1234,192.168.1.2:2345;password=abc_xyz;port=12345;requestedHeartbeat=3;prefetchcount=2;maxRetries=4;usePublisherConfirms=true;maxWaitTimeForConfirms=02:03:39;retryDelay=01:02:03";
            connectionConfiguration = parser.Parse(connectionString);

            connectionConfiguration.Hosts.First().Host.ShouldEqual("192.168.1.1");
            connectionConfiguration.Hosts.First().Port.ShouldEqual(1234);
            connectionConfiguration.Hosts.Last().Host.ShouldEqual("192.168.1.2");
            connectionConfiguration.Hosts.Last().Port.ShouldEqual(2345);
            connectionConfiguration.VirtualHost.ShouldEqual("Copa");
            connectionConfiguration.UserName.ShouldEqual("Copa");
            connectionConfiguration.Password.ShouldEqual("abc_xyz");
            connectionConfiguration.Port.ShouldEqual(12345);
            connectionConfiguration.RequestedHeartbeat.ShouldEqual(3);
            connectionConfiguration.PrefetchCount.ShouldEqual(2);
            connectionConfiguration.MaxRetries.ShouldEqual(4);
            connectionConfiguration.UsePublisherConfirms.ShouldEqual(true);
            connectionConfiguration.MaxWaitTimeForConfirms.ShouldEqual(new TimeSpan(2,3,39)); //02:03:39
            connectionConfiguration.DelayBetweenRetries.ShouldEqual(new TimeSpan(1,2,3)); //01:02:03
        }

        [Test]
        public void Should_parse_the_hostname() {
            connectionString = "host=myHost";
            connectionConfiguration = parser.Parse(connectionString);
            Assert.AreEqual("myHost", connectionConfiguration.Hosts.First().Host);
        }

        [Test]
        public void Should_parse_the_port()
        {
            connectionString = ("host=localhost;port=8181");
            connectionConfiguration = parser.Parse(connectionString);
            Assert.AreEqual(8181, connectionConfiguration.Hosts.First().Port);
        }

        [Test]
        public void Should_parse_the_username()
        {
            connectionString = ("host=localhost;username=test");
            connectionConfiguration = parser.Parse(connectionString);
            Assert.AreEqual("test", connectionConfiguration.UserName);
        }

        [Test]
        public void Should_parse_the_password()
        {
            connectionString = ("host=localhost;password=test");
            connectionConfiguration = parser.Parse(connectionString);
            Assert.AreEqual("test", connectionConfiguration.Password);
        }

        [Test]
        public void Should_default_the_port_if_not_set()
        {
            connectionString = ("host=myHost");
            connectionConfiguration = parser.Parse(connectionString);
            Assert.AreEqual(ConnectionConfiguration.DefaultPort, connectionConfiguration.Hosts.First().Port);
        }


        [Test]
        public void Should_parse_the_virtual_hostname()
        {
            connectionString = ("host=localhost;virtualHost=myVirtualHost");
            connectionConfiguration = parser.Parse(connectionString);
            Assert.AreEqual("myVirtualHost", connectionConfiguration.VirtualHost);
        }

        
        [Test]
        public void Should_parse_the_requestedHeartbeat()
        {
            connectionString = ("host=localhost;requestedHeartbeat=5");
            connectionConfiguration = parser.Parse(connectionString);
            Assert.AreEqual(5, connectionConfiguration.RequestedHeartbeat);
        }

        [Test]
        public void Should_parse_the_maxretries()
        {
            connectionString = ("host=localhost;maxRetries=5");
            connectionConfiguration = parser.Parse(connectionString);
            Assert.AreEqual(5, connectionConfiguration.MaxRetries);
        }

        [Test]
        public void Should_parse_the_retry_delay()
        {
            connectionString = ("host=localhost;retryDelay=00:00:10");
            connectionConfiguration = parser.Parse(connectionString);
            Assert.AreEqual(TimeSpan.FromSeconds(10), connectionConfiguration.DelayBetweenRetries);
        }

        [Test]
        [ExpectedException(typeof(FormatException))]
        public void Should_throw_if_given_badly_formatted_retry_delay()
        {
            connectionString = ("host=localhost;retryDelay=00:0d0:10");
            connectionConfiguration = parser.Parse(connectionString);
        }

        [Test]
        public void Should_parse_the_prefetch_count()
        {
            connectionString = ("host=localhost;prefetchcount=10");
            connectionConfiguration = parser.Parse(connectionString);
            Assert.AreEqual(10, connectionConfiguration.PrefetchCount);
        }


        [Test]
        public void Should_default_the_prefetch_count()
        {
            connectionString = ("host=localhost");
            connectionConfiguration = parser.Parse(connectionString);
            Assert.AreEqual(ConnectionConfiguration.DefaultPrefetchCount, connectionConfiguration.PrefetchCount);
        }

        [Test]
        public void Should_default_the_requested_heartbeat()
        {
            connectionString = ("host=localhost");
            connectionConfiguration = parser.Parse(connectionString);
            Assert.AreEqual(ConnectionConfiguration.DefaultHeartBeatInSeconds, connectionConfiguration.RequestedHeartbeat);
        }
    }
}