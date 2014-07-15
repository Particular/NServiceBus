namespace NServiceBus.Hosting.Tests
{
    using System.Collections.Generic;
    using System.Linq;
    using Features;
    using NUnit.Framework;
    using Persistence;
    using Roles;
    using Roles.Handlers;
    using Unicast.Subscriptions;
    using Unicast.Subscriptions.MessageDrivenSubscriptions;

    [TestFixture]
    public class With_persistence_tests
    {
        Configure config;

        [SetUp]
        public void SetUp()
        {
            config = Configure.With(o =>
            {
                o.EndpointName("myTests");
                o.TypesToScan(new[]
                {
                    typeof(PersistenceRoleHandler),
                    typeof(MyPersistenceConfigurer)
                });
            });

            roleManager = new RoleManager(new[] { typeof(PersistenceRoleHandler).Assembly });
        }

        RoleManager roleManager;

        [Test]
        public void Should_configure_requested_persistence()
        {
            roleManager.ConfigureBusForEndpoint(new ConfigWithCustomPersistence(), config);

            Assert.True(config.Settings.Get<EnabledPersistences>().GetEnabled().First().PersitenceType == typeof(MyTestPersistence));
        }

        [Test]
        public void Should_default_to_in_memory_if_no_other_persistence_is_configured()
        {
            var handler = new EnableSelectedPersistences();
            handler.Run(config);

            Assert.True(config.Settings.Get<EnabledPersistences>().GetEnabled().First().PersitenceType == typeof(InMemory));
        }

        [Test]
        public void Should_used_configured_persistence_if_one_is_configured()
        {
            var handler = new EnableSelectedPersistences();
            config.Configurer.ConfigureComponent<MyTestSubscriptionStorage>(DependencyLifecycle.SingleInstance);

            handler.Run(config);

            Assert.IsInstanceOf<MyTestSubscriptionStorage>(config.Builder.Build<ISubscriptionStorage>());
        }
    }

    public class MyTestSubscriptionStorage : ISubscriptionStorage
    {
        public void Subscribe(Address client, IEnumerable<MessageType> messageTypes)
        {
            
        }

        public void Unsubscribe(Address client, IEnumerable<MessageType> messageTypes)
        {
        }

        public IEnumerable<Address> GetSubscriberAddressesForMessage(IEnumerable<MessageType> messageTypes)
        {
            return new List<Address>();
        }

        public void Init()
        {
        }
    }

    public class ConfigWithCustomPersistence : IConfigureThisEndpoint, AsA_Server, UsingPersistence<MyTestPersistence>
    {
        public void Customize(ConfigurationBuilder builder)
        {
        }
    }


    public class MyTestPersistence : PersistenceDefinition
    {
    }

    public class MyPersistenceConfigurer : IConfigurePersistence<MyTestPersistence>
    {
        public void Enable(Configure config, List<Storage> storagesToEnable)
        {
            config.Settings.EnableFeatureByDefault<MyTestSubscriptionPersistence>();
        }
    }

    public class MyTestSubscriptionPersistence : Feature
    {
        protected internal override void Setup(FeatureConfigurationContext context)
        {
            context.Container.ConfigureComponent<MyTestSubscriptionStorage>(DependencyLifecycle.InstancePerCall);
        }
    }
}