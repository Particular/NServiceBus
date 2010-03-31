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


        [Test]
        public void Setter_dependencies_should_be_supported()
        {
            VerifyForAllBuilders((builder) =>
            {
                builder.Configure(typeof(ClassWithSetterDependencies), ComponentCallModelEnum.Singleton);
                builder.ConfigureProperty(typeof(ClassWithSetterDependencies), "EnumDependency", SomeEnum.X);
                builder.ConfigureProperty(typeof(ClassWithSetterDependencies), "SimpleDependency",1);
                builder.ConfigureProperty(typeof(ClassWithSetterDependencies), "StringDependency", "Test");

                var component = (ClassWithSetterDependencies)builder.Build(typeof(ClassWithSetterDependencies));

                component.EnumDependency.ShouldEqual(SomeEnum.X);
                component.SimpleDependency.ShouldEqual(1);
                component.StringDependency.ShouldEqual("Test");
            });

        }

    }

    public class DuplicateClass
    {
        public bool SomeProperty { get; set; }
        public bool AnotherProperty { get; set; }
    }

    public class ClassWithSetterDependencies
    {
        public SomeEnum EnumDependency { get; set; }
        public int SimpleDependency { get; set; }
        public string StringDependency { get; set; }
    }

    public enum SomeEnum
    {
        X
    }
}