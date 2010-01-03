using System;
using NBehave.Spec.NUnit;
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
                                 builder.HasComponent(typeof(ExistingComponent)).ShouldBeTrue());
        }
        [Test]
        public void Non_existing_components_should_return_false()
        {
            VerifyForAllBuilders(builder =>
                                 builder.HasComponent(typeof(NonExistingComponent)).ShouldBeFalse());
        }

        [Test]
        public void Builders_should_not_determine_existence_by_building_components()
        {
            VerifyForAllBuilders(builder =>
                                 builder.HasComponent(typeof(ExistingComponentWithUnsatisfiedDep)).ShouldBeTrue());
        }

        protected override Action<IContainer> InitializeBuilder()
        {
            return (c) =>
                       {
                           c.Configure(typeof (ExistingComponent), ComponentCallModelEnum.Singlecall);
                           c.Configure(typeof (ExistingComponentWithUnsatisfiedDep), ComponentCallModelEnum.Singlecall);
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