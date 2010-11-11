using System;
using NServiceBus.ObjectBuilder;
using NServiceBus.ObjectBuilder.Common;
using NUnit.Framework;

namespace ObjectBuilder.Tests
{
    [TestFixture]
    public class When_querying_for_registered_components : BuilderFixture
    {
        [Test]
        public void Existing_components_should_return_true()
        {
            VerifyForAllBuilders(builder =>
                                 Assert.True(builder.HasComponent(typeof(ExistingComponent))));
        }
        [Test]
        public void Non_existing_components_should_return_false()
        {
            VerifyForAllBuilders(builder =>
                                 Assert.False(builder.HasComponent(typeof(NonExistingComponent))));
        }

        [Test]
        public void Builders_should_not_determine_existence_by_building_components()
        {
            VerifyForAllBuilders(builder =>
                                 Assert.True(builder.HasComponent(typeof(ExistingComponentWithUnsatisfiedDep))));
        }

        protected override Action<IContainer> InitializeBuilder()
        {
            return (c) =>
                       {
                           c.Configure(typeof (ExistingComponent), DependencyLifecycle.InstancePerCall);
                           c.Configure(typeof (ExistingComponentWithUnsatisfiedDep), DependencyLifecycle.InstancePerCall);
                       };
        }
        public class NonExistingComponent
        {
        }

        public class ExistingComponent
        {
        }
        public class ExistingComponentWithUnsatisfiedDep
        {
            public ExistingComponentWithUnsatisfiedDep(NonExistingComponent dependency)
            {

            }
        }
    }
   
}