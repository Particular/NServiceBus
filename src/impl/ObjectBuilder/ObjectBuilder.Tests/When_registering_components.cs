using System.Linq;
using NBehave.Spec.NUnit;
using NServiceBus.ObjectBuilder;
using NUnit.Framework;

namespace ObjectBuilder.Tests
{
    [TestFixture]
    public class When_registering_components : BuilderFixture
    {
        [Test]
        public void Multiple_registrations_of_the_same_component_should_be_allowed()
        {
            VerifyForAllBuilders((builder)=>
                                     {
                                         builder.Configure(typeof(DuplicateClass), ComponentCallModelEnum.Singlecall);
                                         builder.Configure(typeof(DuplicateClass), ComponentCallModelEnum.Singlecall);

                                         builder.BuildAll(typeof(DuplicateClass)).Count().ShouldEqual(1);
                                     });
        }

        [Test]
        public void Properties_set_on_duplicate_registrations_should_not_be_discarded()
        {
            VerifyForAllBuilders((builder) =>
            {
                builder.Configure(typeof(DuplicateClass), ComponentCallModelEnum.Singleton);
                builder.ConfigureProperty(typeof(DuplicateClass),"SomeProperty",true);
                
                builder.Configure(typeof(DuplicateClass), ComponentCallModelEnum.Singleton);
                builder.ConfigureProperty(typeof(DuplicateClass), "AnotherProperty", true);

                var component = (DuplicateClass)builder.Build(typeof(DuplicateClass));

                component.SomeProperty.ShouldBeTrue();

                component.AnotherProperty.ShouldBeTrue();
            });
            
        }
    }

    public class DuplicateClass
    {
        public bool SomeProperty { get; set; }
        public bool AnotherProperty { get; set; }
    }
}