using System.Linq;
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
                                         builder.Configure(typeof(DuplicateClass), DependencyLifecycle.InstancePerCall);
                                         builder.Configure(typeof(DuplicateClass), DependencyLifecycle.InstancePerCall);

                                         Assert.AreEqual(builder.BuildAll(typeof(DuplicateClass)).Count(),1);
                                     });
        }

        [Test]
        public void Properties_set_on_duplicate_registrations_should_not_be_discarded()
        {
            VerifyForAllBuilders((builder) =>
            {
                builder.Configure(typeof(DuplicateClass), DependencyLifecycle.SingleInstance);
                builder.ConfigureProperty(typeof(DuplicateClass),"SomeProperty",true);
                
                builder.Configure(typeof(DuplicateClass), DependencyLifecycle.SingleInstance);
                builder.ConfigureProperty(typeof(DuplicateClass), "AnotherProperty", true);

                var component = (DuplicateClass)builder.Build(typeof(DuplicateClass));

                Assert.True(component.SomeProperty);

                Assert.True(component.AnotherProperty);
            });
            
        }


        [Test]
        public void Setter_dependencies_should_be_supported()
        {
            VerifyForAllBuilders((builder) =>
            {
                builder.Configure(typeof(ClassWithSetterDependencies), DependencyLifecycle.SingleInstance);
                builder.ConfigureProperty(typeof(ClassWithSetterDependencies), "EnumDependency", SomeEnum.X);
                builder.ConfigureProperty(typeof(ClassWithSetterDependencies), "SimpleDependency",1);
                builder.ConfigureProperty(typeof(ClassWithSetterDependencies), "StringDependency", "Test");

                var component = (ClassWithSetterDependencies)builder.Build(typeof(ClassWithSetterDependencies));

                Assert.AreEqual(component.EnumDependency,SomeEnum.X);
                Assert.AreEqual(component.SimpleDependency,1);
                Assert.AreEqual(component.StringDependency,"Test");
            });

        }

        [Test]
        public void Concrete_classes_should_get_the_same_lifecycle_as_their_interfaces()
        {
            VerifyForAllBuilders(builder =>
            {
                builder.Configure(typeof(SingletonComponent), DependencyLifecycle.SingleInstance);

                Assert.AreSame(builder.Build(typeof(SingletonComponent)), builder.Build(typeof(ISingletonComponent)));
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