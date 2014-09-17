namespace NServiceBus.ContainerTests
{
    using NServiceBus;
    using NServiceBus.ObjectBuilder.Common;
    using NUnit.Framework;

    [TestFixture]
    public class When_querying_for_registered_components 
    {
        [Test]
        public void Existing_components_should_return_true()
        {
            using (var builder = TestContainerBuilder.ConstructBuilder())
            {
                InitializeBuilder(builder);
                Assert.True(builder.HasComponent(typeof(ExistingComponent)));
            }
        }

        [Test]
        public void Non_existing_components_should_return_false()
        {
            using (var builder = TestContainerBuilder.ConstructBuilder())
            {
                InitializeBuilder(builder);
                Assert.False(builder.HasComponent(typeof(NonExistingComponent)));
            }
        }

        [Test]
        public void Builders_should_not_determine_existence_by_building_components()
        {
            using (var builder = TestContainerBuilder.ConstructBuilder())
            {
                InitializeBuilder(builder);
                Assert.True(builder.HasComponent(typeof(ExistingComponentWithUnsatisfiedDependency)));
            }
        }

        void InitializeBuilder(IContainer c)
        {
            c.Configure(typeof(ExistingComponent), DependencyLifecycle.InstancePerCall);
            c.Configure(typeof(ExistingComponentWithUnsatisfiedDependency), DependencyLifecycle.InstancePerCall);
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