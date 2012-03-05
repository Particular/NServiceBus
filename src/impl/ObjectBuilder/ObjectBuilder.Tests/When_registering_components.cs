using System.Linq;
using NServiceBus;
using NUnit.Framework;

namespace ObjectBuilder.Tests
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using NServiceBus.ObjectBuilder.Spring;
    using NServiceBus.ObjectBuilder.Unity;

    [TestFixture]
    public class When_registering_components : BuilderFixture
    {
        [Test]
        public void Multiple_registrations_of_the_same_component_should_be_allowed()
        {
           
            ForAllBuilders((builder) =>
                                     {
                                         builder.Configure(typeof(DuplicateClass), DependencyLifecycle.InstancePerCall);
                                         builder.Configure(typeof(DuplicateClass), DependencyLifecycle.InstancePerCall);
            
                                         Assert.AreEqual(1,builder.BuildAll(typeof(DuplicateClass)).Count());
                                     });
        }


        [Test]
        public void A_registration_should_be_allowed_to_be_updated()
        {
         
            ForAllBuilders((builder) =>
            {
                builder.RegisterSingleton(typeof(ISingletonComponent), new SingletonComponent());
                builder.RegisterSingleton(typeof(ISingletonComponent), new AnotherSingletonComponent());

                Assert.IsInstanceOf<AnotherSingletonComponent>(builder.Build(typeof(ISingletonComponent)));
            }, typeof(SpringObjectBuilder), typeof(UnityObjectBuilder));
        }


        [Test]
        public void Register_singleton_should_be_supported()
        {
            ForAllBuilders((builder) =>
            {
                var singleton = new SingletonComponent();
                builder.RegisterSingleton(typeof(ISingletonComponent), singleton);
                builder.RegisterSingleton(typeof(SingletonComponent), singleton);
                Assert.AreEqual(builder.Build(typeof(SingletonComponent)), singleton);
                Assert.AreEqual(builder.Build(typeof(ISingletonComponent)), singleton);
            });
        }

        [Test]
        public void Properties_set_on_duplicate_registrations_should_not_be_discarded()
        {
            ForAllBuilders((builder) =>
            {
                builder.Configure(typeof(DuplicateClass), DependencyLifecycle.SingleInstance);
                builder.ConfigureProperty(typeof(DuplicateClass), "SomeProperty", true);

                builder.Configure(typeof(DuplicateClass), DependencyLifecycle.SingleInstance);
                builder.ConfigureProperty(typeof(DuplicateClass), "AnotherProperty", true);

                var component = (DuplicateClass)builder.Build(typeof(DuplicateClass));

                Assert.True(component.SomeProperty);

                Assert.True(component.AnotherProperty);
            }, typeof(UnityObjectBuilder));

        }


        [Test]
        public void Setter_dependencies_should_be_supported()
        {
            ForAllBuilders((builder) =>
            {
                builder.Configure(typeof(SomeClass), DependencyLifecycle.InstancePerCall);
                builder.Configure(typeof(ClassWithSetterDependencies), DependencyLifecycle.SingleInstance);
                builder.ConfigureProperty(typeof(ClassWithSetterDependencies), "EnumDependency", SomeEnum.X);
                builder.ConfigureProperty(typeof(ClassWithSetterDependencies), "SimpleDependency", 1);
                builder.ConfigureProperty(typeof(ClassWithSetterDependencies), "StringDependency", "Test");

                var component = (ClassWithSetterDependencies)builder.Build(typeof(ClassWithSetterDependencies));

                Assert.AreEqual(component.EnumDependency, SomeEnum.X);
                Assert.AreEqual(component.SimpleDependency, 1);
                Assert.AreEqual(component.StringDependency, "Test");
                Assert.NotNull(component.ConcreteDependency, "Concrete classed should be property injected");
                Assert.NotNull(component.InterfaceDependency, "Interfaces should be property injected");
            }, typeof(UnityObjectBuilder));

        }

        [Test]
        public void Concrete_classes_should_get_the_same_lifecycle_as_their_interfaces()
        {
            ForAllBuilders(builder =>
            {
                builder.Configure(typeof(SingletonComponent), DependencyLifecycle.SingleInstance);

                Assert.AreSame(builder.Build(typeof(SingletonComponent)), builder.Build(typeof(ISingletonComponent)));
            });


        }

        [Test]
        public void All_implemented_interfaces_should_be_registered()
        {
            ForAllBuilders(builder =>
            {
                builder.Configure(typeof(ComponentWithMultipleInterfaces), DependencyLifecycle.InstancePerCall);

                Assert.True(builder.HasComponent(typeof(ISomeInterface)));

                Assert.True(builder.HasComponent(typeof(ISomeOtherInterface)));

                Assert.True(builder.HasComponent(typeof(IYetAnotherInterface)));

                Assert.AreEqual(1, builder.BuildAll(typeof(IYetAnotherInterface)).Count());
            }
            , typeof(UnityObjectBuilder));


        }

        [Test]
        public void Generic_interfaces_should_be_registered()
        {
            ForAllBuilders(builder =>
            {
                builder.Configure(typeof(ComponentWithGenericInterface), DependencyLifecycle.InstancePerCall);

                Assert.True(builder.HasComponent(typeof(ISomeGenericInterface<string>)));
            }
            , typeof(UnityObjectBuilder));
        }

        [Test,Ignore("Not sure that we should enforce this")]
        public void System_interfaces_should_not_be_autoregistered()
        {
            ForAllBuilders(builder =>
            {
                builder.Configure(typeof(ComponentWithSystemInterface), DependencyLifecycle.InstancePerCall);

                Assert.False(builder.HasComponent(typeof(IGrouping<string, string>)));
                Assert.False(builder.HasComponent(typeof(IDisposable)));
            }
            , typeof(UnityObjectBuilder));
        }
    }

    public class ComponentWithMultipleInterfaces : ISomeInterface, ISomeOtherInterface, IYetAnotherInterface
    {
    }

    public class ComponentWithGenericInterface : ISomeGenericInterface<string>
    {
    }

    public class ComponentWithSystemInterface : IGrouping<string, string>,IDisposable
    {
        public IEnumerator<string> GetEnumerator()
        {
            throw new NotImplementedException();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public string Key
        {
            get { throw new NotImplementedException(); }
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }
    }

    public interface ISomeGenericInterface<T>
    {
    }

    public interface ISomeOtherInterface : IYetAnotherInterface
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
        public SomeClass ConcreteDependency { get; set; }
        public SomeClass ConcreteDependencyWithPrivateSet { get; private set; }
    }

    public class SomeClass : ISomeInterface
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