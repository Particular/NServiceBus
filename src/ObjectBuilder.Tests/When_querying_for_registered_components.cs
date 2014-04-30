namespace ObjectBuilder.Tests
{
    using System;
    using NServiceBus;
    using NServiceBus.ObjectBuilder.Common;
    using NUnit.Framework;

    [TestFixture]
    public class When_querying_for_registered_components : BuilderFixture
    {
        [Test]
        public void Existing_components_should_return_true()
        {
            ForAllBuilders(builder =>
                Assert.True(builder.HasComponent(typeof(ExistingComponent))));
        }

        [Test]
        public void Non_existing_components_should_return_false()
        {
            ForAllBuilders(builder =>
                Assert.False(builder.HasComponent(typeof(NonExistingComponent))));
        }

        [Test]
        public void Builders_should_not_determine_existence_by_building_components()
        {
            ForAllBuilders(builder =>
                Assert.True(builder.HasComponent(typeof(ExistingComponentWithUnsatisfiedDependency))));
        }

        protected override Action<IContainer> InitializeBuilder()
        {
            return c =>
                {
                    c.Configure(typeof(ExistingComponent), DependencyLifecycle.InstancePerCall);
                    c.Configure(typeof(ExistingComponentWithUnsatisfiedDependency), DependencyLifecycle.InstancePerCall);
                };
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