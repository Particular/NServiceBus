namespace NServiceBus.Hosting.Tests
{
    using System;
    using NUnit.Framework;
    using Roles;
    using Roles.Handlers;
    using Settings;
    using Transports;
    using Transports.Msmq;
    using Unicast.Config;

    [TestFixture]
    public class With_transport_tests
    {
        [SetUp]
        public void SetUp()
        {
            Configure.With(new[] {typeof (TransportRoleHandler), typeof (MyTransportConfigurer)})
                     .DefineEndpointName("myTests")
                     .DefaultBuilder();

            roleManager = new RoleManager(new[] {typeof (TransportRoleHandler).Assembly});
        }

        RoleManager roleManager;

        [Test]
        public void Should_configure_requested_transport()
        {
            roleManager.ConfigureBusForEndpoint(new ConfigWithCustomTransport());

            Assert.True(MyTransportConfigurer.Called);
        }

        [Test]
        public void Should_default_to_msmq_if_no_other_transport_is_configured()
        {
            var handler = new DefaultTransportForHost();
            handler.Run();

            Assert.True(SettingsHolder.Get<TransportDefinition>("NServiceBus.Transport.SelectedTransport") is Msmq);
        }

        [Test]
        public void Should_used_configured_transport_if_one_is_configured()
        {
            var handler = new DefaultTransportForHost();
            Configure.Instance.Configurer.ConfigureComponent<MyTestTransportSender>(DependencyLifecycle.SingleInstance);

            handler.Run();

            Assert.IsInstanceOf<MyTestTransportSender>(Configure.Instance.Builder.Build<ISendMessages>());
        }
    }

    public class MyTestTransportSender : ISendMessages
    {
        public void Send(TransportMessage message, Address address)
        {
        }
    }

    public class ConfigWithCustomTransport : IConfigureThisEndpoint, AsA_Server, UsingTransport<MyTestTransport>
    {
    }


    public class MyTestTransport : TransportDefinition
    {
    }

    public class MyTransportConfigurer : IConfigureTransport<MyTestTransport>
    {
        public static bool Called;

        public void Configure(Configure config)
        {
            Called = true;
        }
    }
}