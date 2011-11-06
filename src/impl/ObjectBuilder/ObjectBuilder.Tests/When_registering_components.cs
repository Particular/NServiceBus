using System.Linq;
using NServiceBus.ObjectBuilder;
using NUnit.Framework;

namespace ObjectBuilder.Tests
{
    using NServiceBus.ObjectBuilder.Unity;

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
        public void Register_singleton_should_be_supported()
        {
            VerifyForAllBuilders((builder) =>
            {
                builder.RegisterSingleton(typeof(SingletonComponent),new SingletonComponent());
                Assert.AreEqual(builder.Build(typeof(SingletonComponent)), builder.Build(typeof(SingletonComponent)));
            }, typeof(UnityObjectBuilder));
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
            },typeof(UnityObjectBuilder));
            
        }


        [Test]
        public void Setter_dependencies_should_be_supported()
        {
            VerifyForAllBuilders((builder) =>
            {
                builder.Configure(typeof(SomeClass), DependencyLifecycle.InstancePerCall);
                builder.Configure(typeof(ClassWithSetterDependencies), DependencyLifecycle.SingleInstance);
                builder.ConfigureProperty(typeof(ClassWithSetterDependencies), "EnumDependency", SomeEnum.X);
                builder.ConfigureProperty(typeof(ClassWithSetterDependencies), "SimpleDependency",1);
                builder.ConfigureProperty(typeof(ClassWithSetterDependencies), "StringDependency", "Test");

                var component = (ClassWithSetterDependencies)builder.Build(typeof(ClassWithSetterDependencies));

                Assert.AreEqual(component.EnumDependency,SomeEnum.X);
                Assert.AreEqual(component.SimpleDependency,1);
                Assert.AreEqual(component.StringDependency,"Test");
                Assert.NotNull(component.ConcreteDependecy, "Concrete classed should be property injected");
                Assert.NotNull(component.InterfaceDependency,"Interfaces should be property injected");
            }, typeof(UnityObjectBuilder));

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

        [Test]
        public void All_implemented_interfaces_should_be_registered()
        {
            VerifyForAllBuilders(builder =>
            {
                builder.Configure(typeof(ComponentWithMultipleInterfaces), DependencyLifecycle.InstancePerCall);

                Assert.True(builder.HasComponent(typeof(ISomeInterface)));

                Assert.True(builder.HasComponent(typeof(ISomeOtherInterface)));

                Assert.True(builder.HasComponent(typeof(IYetAnotherInterface)));

                Assert.AreEqual(1,builder.BuildAll(typeof(IYetAnotherInterface)).Count());
            }
            ,typeof(NServiceBus.ObjectBuilder.Unity.UnityObjectBuilder));


        }
    }

    public class ComponentWithMultipleInterfaces:ISomeInterface,ISomeOtherInterface,IYetAnotherInterface
    {
    }

    public interface ISomeOtherInterface:IYetAnotherInterface
    {
    }

    public interface IYetAnotherInterface
    {
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
        public ISomeInterface InterfaceDependency { get; set; }
        public SomeClass ConcreteDependecy { get; set; }
    }

    public class SomeClass:ISomeInterface
    {
    }

    public interface ISomeInterface
    {
    }

    public enum SomeEnum
    {
        X
    }
}