#pragma warning disable 0618
namespace NServiceBus.ContainerTests
{
    using Microsoft.Extensions.DependencyInjection;
    using NServiceBus;
    using NUnit.Framework;
    using ServiceCollection = MicrosoftExtensionsDependencyInjection.ServiceCollection;

    [TestFixture]
    public class When_querying_for_registered_components
    {
        [Test]
        public void Existing_components_should_return_true()
        {
            var serviceCollection = new ServiceCollection();
            InitializeBuilder(serviceCollection);

            Assert.True(serviceCollection.HasComponent(typeof(ExistingComponent)));
        }

        [Test]
        public void Non_existing_components_should_return_false()
        {
            var serviceCollection = new ServiceCollection();
            InitializeBuilder(serviceCollection);

            Assert.False(serviceCollection.HasComponent(typeof(NonExistingComponent)));
        }

        [Test]
        public void Builders_should_not_determine_existence_by_building_components()
        {
            var serviceCollection = new ServiceCollection();
            InitializeBuilder(serviceCollection);

            Assert.True(serviceCollection.HasComponent(typeof(ExistingComponentWithUnsatisfiedDependency)));
        }

        void InitializeBuilder(IServiceCollection c)
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
            public ExistingComponentWithUnsatisfiedDependency(NonExistingComponent dependency)
            {

            }
        }
    }
}
#pragma warning restore 0618