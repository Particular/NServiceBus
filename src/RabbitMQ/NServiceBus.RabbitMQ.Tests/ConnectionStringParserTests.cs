namespace NServiceBus.Transports.RabbitMQ.Tests
{
    using System;
    using NUnit.Framework;
    using Config;

    [TestFixture]
    public class ConnectionStringParserTests
    {
        RabbitMqConnectionStringParser parser;


        [Test]
        public void Should_parse_the_hostname()
        {
            parser = new RabbitMqConnectionStringParser("host=myHost");
            Assert.AreEqual("myHost", parser.BuildConnectionFactory().HostName);
        }

        [Test]
        public void Should_parse_the_username()
        {
            parser = new RabbitMqConnectionStringParser("username=test");
            Assert.AreEqual("test", parser.BuildConnectionFactory().UserName);
        }


        [Test]
        public void Should_parse_the_password()
        {
            parser = new RabbitMqConnectionStringParser("password=test");
            Assert.AreEqual("test", parser.BuildConnectionFactory().Password);
        }

        [Test]
        public void Should_parse_the_port()
        {
            parser = new RabbitMqConnectionStringParser("port=8181");
            Assert.AreEqual(8181, parser.BuildConnectionFactory().Port);
        }


        [Test]
        public void Should_default_the_port_if_not_set()
        {
            parser = new RabbitMqConnectionStringParser("host=myHost");
            Assert.AreEqual(-1, parser.BuildConnectionFactory().Port);
        }


        [Test]
        public void Should_parse_the_virtual_hostname()
        {
            parser = new RabbitMqConnectionStringParser("virtualHost=myVirtualHost");
            Assert.AreEqual("myVirtualHost", parser.BuildConnectionFactory().VirtualHost);
        }

        
        [Test]
        public void Should_parse_the_requestedHeartbeat()
        {
            parser = new RabbitMqConnectionStringParser("requestedHeartbeat=5");
            Assert.AreEqual(5, parser.BuildConnectionFactory().RequestedHeartbeat);
        }

        [Test]
        public void Should_parse_the_maxretries()
        {
            parser = new RabbitMqConnectionStringParser("maxretries=5");
            Assert.AreEqual(5, parser.BuildConnectionRetrySettings().MaxRetries);
        }

        [Test]
        public void Should_parse_the_retry_delay()
        {
            parser = new RabbitMqConnectionStringParser("retry_delay=00:00:10");
            Assert.AreEqual(TimeSpan.FromSeconds(10), parser.BuildConnectionRetrySettings().DelayBetweenRetries);
        }
    }
}