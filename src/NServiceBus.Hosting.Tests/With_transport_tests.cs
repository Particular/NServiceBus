namespace NServiceBus.Hosting.Tests
{
    using NUnit.Framework;
    using Roles;
    using Roles.Handlers;
    using Transports;
    using Unicast;
    using Unicast.Config;

    [TestFixture]
    public class With_transport_tests
    {
        Configure config;

        [SetUp]
        public void SetUp()
        {
            config= Configure.With(o =>
            {
                o.EndpointName("myTests");
                o.TypesToScan(new[]
                {
                    typeof(TransportRoleHandler),
                    typeof(MyTransportConfigurer)
                });
            })
                     .DefaultBuilder();

            roleManager = new RoleManager(new[] { typeof(TransportRoleHandler).Assembly });
        }

        RoleManager roleManager;

        [Test]
        public void Should_configure_requested_transport()
        {
            roleManager.ConfigureBusForEndpoint(new ConfigWithCustomTransport(), config);

            Assert.True(MyTransportConfigurer.Called);
        }

        [Test]
        public void Should_default_to_msmq_if_no_other_transport_is_configured()
        {
            var handler = new DefaultTransportForHost();
            handler.Run(config);

            Assert.True(config.Settings.Get<TransportDefinition>("NServiceBus.Transport.SelectedTransport") is Msmq);
        }

        [Test]
        public void Should_used_configured_transport_if_one_is_configured()
        {
            var handler = new DefaultTransportForHost();
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