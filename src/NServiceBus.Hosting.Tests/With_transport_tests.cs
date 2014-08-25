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
            var builder = new BusConfiguration();

            builder.EndpointName("myTests");
            builder.UseTransport<MyTestTransport>();

            var config = builder.BuildConfiguration();

            Assert.IsInstanceOf<MyTestTransport>(config.Settings.Get<TransportDefinition>());
        }

        [Test]
        public void Should_default_to_msmq_if_no_other_transport_is_configured()
        {
            var builder = new BusConfiguration();
            builder.EndpointName("myTests");

            Assert.True(builder.BuildConfiguration().Settings.Get<TransportDefinition>() is MsmqTransport);
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
        public void Customize(BusConfiguration configuration)
        {
        }
    }
    class SecondConfigureThisEndpoint : IConfigureThisEndpoint
    {
        public void Customize(BusConfiguration configuration)
        {
        }
    }

    public class MyTestTransport : TransportDefinition
    {
        public override string GetSubScope(string address, string qualifier)
        {
            return address + "." + qualifier;
        }
    }
}