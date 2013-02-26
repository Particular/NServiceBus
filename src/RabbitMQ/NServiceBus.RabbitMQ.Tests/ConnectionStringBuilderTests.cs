namespace NServiceBus.Transports.RabbitMQ.Tests
{
    using NUnit.Framework;
    using NServiceBus.Transports.RabbitMQ.Config;

    [TestFixture]
    public class ConnectionStringBuilderTests
    {
        RabbitMqConnectionStringBuilder builder;


        [Test]
        public void Should_parse_the_hostname()
        {
            builder = new RabbitMqConnectionStringBuilder("host=myHost");
            Assert.AreEqual("myHost", builder.BuildConnectionFactory().HostName);
        }

        [Test]
        public void Should_parse_the_username()
        {
            builder = new RabbitMqConnectionStringBuilder("username=test");
            Assert.AreEqual("test", builder.BuildConnectionFactory().UserName);
        }


        [Test]
        public void Should_parse_the_password()
        {
            builder = new RabbitMqConnectionStringBuilder("password=test");
            Assert.AreEqual("test", builder.BuildConnectionFactory().Password);
        }

        [Test]
        public void Should_parse_the_port()
        {
            builder = new RabbitMqConnectionStringBuilder("port=8181");
            Assert.AreEqual(8181, builder.BuildConnectionFactory().Port);
        }


        [Test]
        public void Should_default_the_port_if_not_set()
        {
            builder = new RabbitMqConnectionStringBuilder("host=myHost");
            Assert.AreEqual(-1, builder.BuildConnectionFactory().Port);
        }


        [Test]
        public void Should_parse_the_virtual_hostname()
        {
            builder = new RabbitMqConnectionStringBuilder("virtualHost=myVirtualHost");
            Assert.AreEqual("myVirtualHost", builder.BuildConnectionFactory().VirtualHost);
        }

        
        [Test]
        public void Should_parse_the_requestedHeartbeat()
        {
            builder = new RabbitMqConnectionStringBuilder("requestedHeartbeat=5");
            Assert.AreEqual(5, builder.BuildConnectionFactory().RequestedHeartbeat);
        }
    }
}