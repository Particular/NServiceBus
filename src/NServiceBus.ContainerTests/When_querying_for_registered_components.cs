namespace NServiceBus.ContainerTests
{
    using MicrosoftExtensionsDependencyInjection;
    using NServiceBus;
    using NUnit.Framework;
    using ObjectBuilder;

    [TestFixture]
    public class When_querying_for_registered_components
    {
        [Test]
        public void Existing_components_should_return_true()
        {
            var serviceCollection = new ServiceCollection();
            var configureComponents = new CommonObjectBuilder(serviceCollection);
            InitializeBuilder(configureComponents);

            Assert.True(configureComponents.HasComponent(typeof(ExistingComponent)));
        }

        [Test]
        public void Non_existing_components_should_return_false()
        {
            var serviceCollection = new ServiceCollection();
            var configureComponents = new CommonObjectBuilder(serviceCollection);
            InitializeBuilder(configureComponents);

            Assert.False(configureComponents.HasComponent(typeof(NonExistingComponent)));
        }

        [Test]
        public void Builders_should_not_determine_existence_by_building_components()
        {
            var serviceCollection = new ServiceCollection();
            var configureComponents = new CommonObjectBuilder(serviceCollection);
            InitializeBuilder(configureComponents);

            Assert.True(configureComponents.HasComponent(typeof(ExistingComponentWithUnsatisfiedDependency)));
        }

        void InitializeBuilder(IConfigureComponents c)
        {
            c.ConfigureComponent(typeof(ExistingComponent), DependencyLifecycle.InstancePerCall);
            c.ConfigureComponent(typeof(ExistingComponentWithUnsatisfiedDependency), DependencyLifecycle.InstancePerCall);
        }

        public class NonExistingComponent
        {
        }

        public class ExistingComponent
        {
        }

        public class ExistingComponentWithUnsatisfiedDependency
        {
            // ReSharper disable once UnusedParameter.Local
            public ExistingComponentWithUnsatisfiedDependency(NonExistingComponent dependency)
            {

            }
        }
    }
}