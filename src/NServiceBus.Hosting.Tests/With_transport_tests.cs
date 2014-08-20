namespace NServiceBus.Hosting.Tests
{
    using NUnit.Framework;
    using Transports;
    using Unicast;

    [TestFixture]
    public class With_transport_tests
    {
        [Test]
        public void Should_configure_requested_transport()
        {
            var builder = new ConfigurationBuilder();

            builder.EndpointName("myTests");
            builder.UseTransport<MyTestTransport>();

            var config = builder.BuildConfiguration();

            Assert.IsInstanceOf<MyTestTransport>(config.Settings.Get<TransportDefinition>());
        }

        [Test]
        public void Should_default_to_msmq_if_no_other_transport_is_configured()
        {
            var builder = new ConfigurationBuilder();
            builder.EndpointName("myTests");

            Assert.True(builder.BuildConfiguration().Settings.Get<TransportDefinition>() is Msmq);
        }
    }

    public class MyTestTransportSender : ISendMessages
    {
        public void Send(TransportMessage message, SendOptions sendOptions)
        {
        }
    }

    public class ConfigWithCustomTransport : IConfigureThisEndpoint, AsA_Server, UsingTransport<MyTestTransport>
    {
        public void Customize(ConfigurationBuilder builder)
        {
        }
    }
    class SecondConfigureThisEndpoint : IConfigureThisEndpoint
    {
        public void Customize(ConfigurationBuilder builder)
        {
        }
    }

    public class MyTestTransport : TransportDefinition
    {
    }
}