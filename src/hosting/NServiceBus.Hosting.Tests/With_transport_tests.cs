namespace NServiceBus.Hosting.Tests
{
    using Config;
    using NUnit.Framework;
    using ObjectBuilder;
    using Roles;
    using Unicast.Queuing;
    using Unicast.Queuing.Msmq;
    using Unicast.Transport;
    using Windows.Roles;
    using Windows.Roles.Handlers;

    [TestFixture]
    public class With_transport_tests
    {
        private RoleManager roleManager;

        [SetUp]
        public void SetUp()
        {
            Configure.With(new[] { typeof(TransportRoleHandler), typeof(MyTransportConfigurer) })
                .DefineEndpointName("myTests")
                .DefaultBuilder();

            roleManager = new RoleManager(new[] { typeof(TransportRoleHandler).Assembly });
        }

        [Test]
        public void Should_configure_requested_transport()
        {
            roleManager.ConfigureBusForEndpoint(new ConfigWithCustomTransport());

            Assert.True(MyTransportConfigurer.Called);
        }

        [Test]
        public void Should_default_to_msmq_if_no_other_transport_is_configured()
        {
            var handler = (IWantToRunBeforeConfigurationIsFinalized)new TransportRoleHandler();
            handler.Run();

            Assert.IsInstanceOf<MsmqMessageSender>(Configure.Instance.Builder.Build<ISendMessages>());
        }

        [Test]
        public void Should_used_configured_transport_if_one_is_configured()
        {
            var handler = (IWantToRunBeforeConfigurationIsFinalized)new TransportRoleHandler();
            Configure.Instance.Configurer.ConfigureComponent<MyTestTransportReceiver>(DependencyLifecycle.SingleInstance);

            handler.Run();

            Assert.IsInstanceOf<MsmqMessageSender>(Configure.Instance.Builder.Build<ISendMessages>());
        }

    }

    public class MyTestTransportReceiver : IReceiveMessages
    {
        public void Init(Address address, bool transactional)
        {
            throw new System.NotImplementedException();
        }

        public TransportMessage Receive()
        {
            throw new System.NotImplementedException();
        }
    }

    public class ConfigWithCustomTransport : IConfigureThisEndpoint, AsA_Server, UsingTransport<MyTestTransport>
    {
    }


    public class MyTestTransport : ITransportDefinition
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