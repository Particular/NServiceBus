namespace NServiceBus.Transport.RabbitMQ.Tests
{
    using Config;
    using NUnit.Framework;

    [TestFixture]
    public class ConnectionStringBuilderTests
    {
        RabbitMqConnectionStringBuilder builder;


        [Test]
        public void Should_parse_the_hostname()
        {
            builder = new RabbitMqConnectionStringBuilder("host=myHost"); 
            Assert.AreEqual("myHost", builder.Host);
        }

        [Test]
        public void Should_set_the_hostname()
        {
            builder = new RabbitMqConnectionStringBuilder {Host = "myHost"};

            Assert.AreEqual("host=myHost", builder.ConnectionString);
        }

    }
}