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
            var config = Configure.With(o =>
            {
                o.EndpointName("myTests");
                o.UseTransport<MyTestTransport>();
            });

            Assert.IsInstanceOf<MyTestTransport>(config.Settings.Get<TransportDefinition>());
        }

        [Test]
        public void Should_default_to_msmq_if_no_other_transport_is_configured()
        {
            var config = Configure.With(o => o.EndpointName("myTests"));
            
            Assert.True(config.Settings.Get<TransportDefinition>() is Msmq);
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