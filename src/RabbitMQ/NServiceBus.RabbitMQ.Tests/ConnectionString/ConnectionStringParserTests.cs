namespace NServiceBus.Transports.RabbitMQ.Tests.ConnectionString
{
    using System;
    using System.Linq;
    using Config;
    using NUnit.Framework;

    [TestFixture]
    public class ConnectionStringParserTests
    {
        const string connectionString =
            "virtualHost=Copa;username=Copa;host=192.168.1.1:1234,192.168.1.2:2345;password=abc_xyz;port=12345;requestedHeartbeat=3;prefetchcount=2;maxRetries=4;usePublisherConfirms=true;maxWaitTimeForConfirms=02:03:39;retryDelay=01:02:03";

        [Test]
        public void Should_correctly_parse_full_connection_string() {
            var parser = new ConnectionStringParser();
            var connectionConfiguration = parser.Parse(connectionString);

            Assert.AreEqual(connectionConfiguration.Hosts.First().Host, "192.168.1.1");
            Assert.AreEqual(connectionConfiguration.Hosts.First().Port, 1234);
            Assert.AreEqual(connectionConfiguration.Hosts.Last().Host, "192.168.1.2");
            Assert.AreEqual(connectionConfiguration.Hosts.Last().Port, 2345);
            Assert.AreEqual(connectionConfiguration.VirtualHost, "Copa");
            Assert.AreEqual(connectionConfiguration.UserName, "Copa");
            Assert.AreEqual(connectionConfiguration.Password, "abc_xyz");
            Assert.AreEqual(connectionConfiguration.Port, 12345);
            Assert.AreEqual(connectionConfiguration.RequestedHeartbeat, 3);
            Assert.AreEqual(connectionConfiguration.PrefetchCount, 2);
            Assert.AreEqual(connectionConfiguration.UsePublisherConfirms, true);
            Assert.AreEqual(connectionConfiguration.MaxWaitTimeForConfirms, new TimeSpan(2, 3, 39)); //02:03:39
            Assert.AreEqual(connectionConfiguration.RetryDelay, new TimeSpan(1, 2, 3)); //01:02:03
        }

        [Test]
        [ExpectedException(typeof(Exception))]
        public void Should_fail_if_host_is_not_present() {

            var parser = new ConnectionStringParser();
            parser.Parse("virtualHost=Copa;username=Copa;password=abc_xyz;port=12345;requestedHeartbeat=3");
        }

        [Test]
        public void Should_parse_list_of_hosts() {

            var parser = new ConnectionStringParser();
            var connectionConfiguration = parser.Parse("host=host.one:1001,host.two:1002,host.three:1003");
            var hosts = connectionConfiguration.Hosts;

            Assert.AreEqual(hosts.Count(), 3);
            Assert.AreEqual(hosts.ElementAt(0).Host, "host.one");
            Assert.AreEqual(hosts.ElementAt(0).Port, 1001);
            Assert.AreEqual(hosts.ElementAt(1).Host, "host.two");
            Assert.AreEqual(hosts.ElementAt(1).Port, 1002);
            Assert.AreEqual(hosts.ElementAt(2).Host, "host.three");
            Assert.AreEqual(hosts.ElementAt(2).Port, 1003);
        }

        [Test]
        public void Should_parse_list_of_hosts_with_single_port()
        {
            var parser = new ConnectionStringParser();
            var connectionConfiguration = parser.Parse("host=my.host.com,my.host2.com;port=1234");

            Assert.AreEqual(connectionConfiguration.Hosts.First().Host, "my.host.com");
            Assert.AreEqual(connectionConfiguration.Hosts.Last().Host, "my.host2.com");
            Assert.AreEqual(connectionConfiguration.Hosts.First().Port, 1234);
            Assert.AreEqual(connectionConfiguration.Hosts.Last().Port, 1234);
        }

        [Test]
        public void Should_parse_list_of_hosts_without_ports() {

            var parser = new ConnectionStringParser();
            var connectionConfiguration = parser.Parse("host=my.host.com,my.host2.com");

            Assert.AreEqual(connectionConfiguration.Hosts.First().Host, "my.host.com");
            Assert.AreEqual(connectionConfiguration.Hosts.Last().Host, "my.host2.com");
            Assert.AreEqual(connectionConfiguration.Hosts.First().Port, 5672);
            Assert.AreEqual(connectionConfiguration.Hosts.Last().Port, 5672);
        }

        [Test]
        public void Should_parse_the_hostname()
        {
            var parser = new ConnectionStringParser();
            var connectionConfiguration = parser.Parse("host=myHost");
            Assert.AreEqual("myHost", connectionConfiguration.Hosts.First().Host);
        }


        [Test]
        public void Should_parse_the_password()
        {
            var parser = new ConnectionStringParser();
            var connectionConfiguration = parser.Parse("host=localhost;password=test");
            Assert.AreEqual("test", connectionConfiguration.Password);
        }

        [Test]
        public void Should_parse_the_port()
        {
            var parser = new ConnectionStringParser();
            var connectionConfiguration = parser.Parse("host=localhost;port=8181");
            Assert.AreEqual(8181, connectionConfiguration.Hosts.First().Port);
        }

        [Test]
        public void Should_parse_the_prefetch_count()
        {
            var parser = new ConnectionStringParser();
            var connectionConfiguration = parser.Parse("host=localhost;prefetchcount=10");
            Assert.AreEqual(10, connectionConfiguration.PrefetchCount);
        }

        [Test]
        public void Should_parse_the_requestedHeartbeat()
        {
            var parser = new ConnectionStringParser();
            var connectionConfiguration = parser.Parse("host=localhost;requestedHeartbeat=5");
            Assert.AreEqual(5, connectionConfiguration.RequestedHeartbeat);
        }

        [Test]
        public void Should_parse_the_retry_delay()
        {
            var parser = new ConnectionStringParser();
            var connectionConfiguration = parser.Parse("host=localhost;retryDelay=00:00:10");
            Assert.AreEqual(TimeSpan.FromSeconds(10), connectionConfiguration.RetryDelay);
        }

        [Test]
        public void Should_parse_the_username()
        {
            var parser = new ConnectionStringParser();
            var connectionConfiguration = parser.Parse("host=localhost;username=test");
            Assert.AreEqual("test", connectionConfiguration.UserName);
        }

        [Test]
        public void Should_parse_the_virtual_hostname()
        {
            var parser = new ConnectionStringParser();
            var connectionConfiguration = parser.Parse("host=localhost;virtualHost=myVirtualHost");
            Assert.AreEqual("myVirtualHost", connectionConfiguration.VirtualHost);
        }

        [Test]
        public void Should_throw_if_given_badly_formatted_max_wait_time_for_confirms()
        {
            var parser = new ConnectionStringParser();
            var formatException = Assert.Throws<FormatException>(() => parser.Parse("host=localhost;maxWaitTimeForConfirms=00:0d0:10"));
            Assert.AreEqual("00:0d0:10 is not a valid value for TimeSpan.",formatException.Message);
        }

        [Test]
        public void Should_throw_if_given_badly_formatted_retry_delay()
        {
            var parser = new ConnectionStringParser();
            var formatException = Assert.Throws<FormatException>(() => parser.Parse("host=localhost;retryDelay=00:0d0:10"));
            Assert.AreEqual("00:0d0:10 is not a valid value for TimeSpan.", formatException.Message);
        }

        [Test]
        [ExpectedException(typeof(ArgumentException))]
        public void Should_throw_on_malformed_string()
        {
            var parser = new ConnectionStringParser();
            parser.Parse("not a well formed name value pair;");
        }
    }
}