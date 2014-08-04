namespace NServiceBus.Hosting.Tests
{
    using System;
    using NUnit.Framework;
    using Transports;
    using Unicast;

    [TestFixture]
    public class With_transport_tests
    {

        [Test]
        public void Should_configure_requested_transport()
        {
            var config = Configure.With(o => o.EndpointName("myTests"));
            RoleManager.ConfigureBusForEndpoint(new ConfigWithCustomTransport(), config);

            Assert.AreEqual(typeof(MyTransportConfigurer), config.Settings.Get<Type>("TransportConfigurer"));
            Assert.IsInstanceOf<MyTestTransport>(config.Settings.Get<TransportDefinition>());
        }

        [Test]
        public void Should_default_to_msmq_if_no_other_transport_is_configured()
        {
            var config = Configure.With(o => o.EndpointName("myTests"));
            var handler = new EnableSelectedTransport();
            handler.Run(config);

            Assert.True(config.Settings.Get<TransportDefinition>() is Msmq);
        }

        [Test]
        public void Should_used_configured_transport_if_one_is_configured()
        {
            var config = Configure.With(o => o.EndpointName("myTests"));
            var handler = new EnableSelectedTransport();
            config.Configurer.ConfigureComponent<MyTestTransportSender>(DependencyLifecycle.SingleInstance);

            handler.Run(config);

            Assert.IsInstanceOf<MyTestTransportSender>(config.Builder.Build<ISendMessages>());
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

    public class MyTransportConfigurer : IConfigureTransport<MyTestTransport>
    {
        public void Configure(Configure config)
        {
        }
    }
}