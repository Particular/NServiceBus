namespace NServiceBus.ContainerTests
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using NServiceBus;
    using NUnit.Framework;

    [TestFixture]
    public class When_registering_components
    {
        [Test]
        public void Multiple_registrations_of_the_same_component_should_be_allowed()
        {
            using (var builder = TestContainerBuilder.ConstructBuilder())
            {
                builder.Configure(typeof(DuplicateClass), DependencyLifecycle.InstancePerCall);
                builder.Configure(typeof(DuplicateClass), DependencyLifecycle.InstancePerCall);

                Assert.AreEqual(1, builder.BuildAll(typeof(DuplicateClass)).Count());
            }
        }

        [Test]
        public void Should_support_lambdas_that_uses_other_components_registered_later()
        {
            using (var builder = TestContainerBuilder.ConstructBuilder())
            {
                builder.Configure(() => ((StaticFactory) builder.Build(typeof(StaticFactory))).Create(), DependencyLifecycle.InstancePerCall);
                builder.Configure(() => new StaticFactory(), DependencyLifecycle.SingleInstance);

                Assert.NotNull(builder.Build(typeof(ComponentCreatedByFactory)));
            }
        }

        [Test]
        public void A_registration_should_be_allowed_to_be_updated()
        {
            using (var builder = TestContainerBuilder.ConstructBuilder())
            {
                builder.RegisterSingleton(typeof(ISingletonComponent), new SingletonComponent());
                builder.RegisterSingleton(typeof(ISingletonComponent), new AnotherSingletonComponent());

                Assert.IsInstanceOf<AnotherSingletonComponent>(builder.Build(typeof(ISingletonComponent)));
            }

            //Not supported by, typeof(SpringObjectBuilder));
        }

        [Test]
        [Explicit]
        public void A_registration_should_update_default_component_for_interface()
        {
            using (var builder = TestContainerBuilder.ConstructBuilder())
            {
                builder.Configure(typeof(SomeClass), DependencyLifecycle.InstancePerCall);
                builder.Configure(typeof(SomeOtherClass), DependencyLifecycle.InstancePerCall);

                Assert.IsInstanceOf<SomeOtherClass>(builder.Build(typeof(ISomeInterface)));
            }
        }

        [Test]
        public void Register_singleton_should_be_supported()
        {
            using (var builder = TestContainerBuilder.ConstructBuilder())
            {
                var singleton = new SingletonComponent();
                builder.RegisterSingleton(typeof(ISingletonComponent), singleton);
                builder.RegisterSingleton(typeof(SingletonComponent), singleton);
                Assert.AreEqual(builder.Build(typeof(SingletonComponent)), singleton);
                Assert.AreEqual(builder.Build(typeof(ISingletonComponent)), singleton);
            }
        }

        [Test]
        public void Registering_the_same_singleton_for_different_interfaces_should_be_supported()
        {
            using (var builder = TestContainerBuilder.ConstructBuilder())
            {
                var singleton = new SingletonThatImplementsToInterfaces();
                builder.RegisterSingleton(typeof(ISingleton1), singleton);
                builder.RegisterSingleton(typeof(ISingleton2), singleton);

                builder.Configure(typeof(ComponentThatDependsOnMultiSingletons), DependencyLifecycle.InstancePerCall);

                var dependency = (ComponentThatDependsOnMultiSingletons) builder.Build(typeof(ComponentThatDependsOnMultiSingletons));

                Assert.NotNull(dependency.Singleton1);
                Assert.NotNull(dependency.Singleton2);

                Assert.AreEqual(builder.Build(typeof(ISingleton1)), singleton);
                Assert.AreEqual(builder.Build(typeof(ISingleton2)), singleton);
            }

            //Not supported by,typeof(SpringObjectBuilder));
        }

        [Test]
        public void Properties_set_on_duplicate_registrations_should_not_be_discarded()
        {
            using (var builder = TestContainerBuilder.ConstructBuilder())
            {
                builder.Configure(typeof(DuplicateClass), DependencyLifecycle.SingleInstance);
                builder.ConfigureProperty(typeof(DuplicateClass), "SomeProperty", true);

                builder.Configure(typeof(DuplicateClass), DependencyLifecycle.SingleInstance);
                builder.ConfigureProperty(typeof(DuplicateClass), "AnotherProperty", true);

                var component = (DuplicateClass) builder.Build(typeof(DuplicateClass));
                Assert.True(component.SomeProperty);

                Assert.True(component.AnotherProperty);
            }
        }

        [Test]
        public void Properties_configured_multiple_times_should_retain_only_the_last_configuration()
        {
            using (var builder = TestContainerBuilder.ConstructBuilder())
            {
                builder.Configure(typeof(DuplicateClass), DependencyLifecycle.SingleInstance);
                builder.ConfigureProperty(typeof(DuplicateClass), "SomeProperty", false);
                builder.ConfigureProperty(typeof(DuplicateClass), "SomeProperty", true); // this should remove/override the previous property setting

                var component = (DuplicateClass) builder.Build(typeof(DuplicateClass));
                Assert.True(component.SomeProperty);
            }
        }

        [Test]
        public void Setter_dependencies_should_be_supported_when_resolving_interfaces()
        {
            using (var builder = TestContainerBuilder.ConstructBuilder())
            {
                builder.Configure(typeof(SomeClass), DependencyLifecycle.InstancePerCall);
                builder.RegisterSingleton(typeof(IWithSetterDependencies), new ClassWithSetterDependencies());

                var component = (ClassWithSetterDependencies)builder.Build(typeof(IWithSetterDependencies));
                Assert.NotNull(component.ConcreteDependency, "Concrete classed should be property injected");
                Assert.NotNull(component.InterfaceDependency, "Interfaces should be property injected");
                Assert.NotNull(component.concreteDependencyWithSetOnly, "Set only properties should be supported");
            }
        }


        [Test]
        public void Setter_dependencies_should_be_supported()
        {
            using (var builder = TestContainerBuilder.ConstructBuilder())
            {
                builder.Configure(typeof(SomeClass), DependencyLifecycle.InstancePerCall);
                builder.Configure(typeof(ClassWithSetterDependencies), DependencyLifecycle.SingleInstance);
                builder.ConfigureProperty(typeof(ClassWithSetterDependencies), "EnumDependency", SomeEnum.X);
                builder.ConfigureProperty(typeof(ClassWithSetterDependencies), "SimpleDependency", 1);
                builder.ConfigureProperty(typeof(ClassWithSetterDependencies), "StringDependency", "Test");

                var component = (ClassWithSetterDependencies) builder.Build(typeof(ClassWithSetterDependencies));
                Assert.AreEqual(component.EnumDependency, SomeEnum.X);
                Assert.AreEqual(component.SimpleDependency, 1);
                Assert.AreEqual(component.StringDependency, "Test");
                Assert.NotNull(component.ConcreteDependency, "Concrete classed should be property injected");
                Assert.NotNull(component.InterfaceDependency, "Interfaces should be property injected");
                Assert.NotNull(component.concreteDependencyWithSetOnly, "Set only properties should be supported");
            }
        }

        [Test]
        public void Setter_dependencies_should_override_container_defaults()
        {
            using (var builder = TestContainerBuilder.ConstructBuilder())
            {
                builder.Configure(typeof(SomeClass), DependencyLifecycle.InstancePerCall);
                builder.Configure(typeof(ClassWithSetterDependencies), DependencyLifecycle.SingleInstance);
                builder.ConfigureProperty(typeof(ClassWithSetterDependencies), "InterfaceDependency", new SomeOtherClass());

                var component = (ClassWithSetterDependencies) builder.Build(typeof(ClassWithSetterDependencies));
                Assert.IsInstanceOf(typeof(SomeOtherClass), component.InterfaceDependency, "Explicitly set dependency should be injected, not container's default type");
            }
        }

        [Test]
        public void Concrete_classes_should_get_the_same_lifecycle_as_their_interfaces()
        {
            using (var builder = TestContainerBuilder.ConstructBuilder())
            {
                builder.Configure(typeof(SingletonComponent), DependencyLifecycle.SingleInstance);

                Assert.AreSame(builder.Build(typeof(SingletonComponent)), builder.Build(typeof(ISingletonComponent)));
            }
        }

        [Test]
        public void All_implemented_interfaces_should_be_registered()
        {
            using (var builder = TestContainerBuilder.ConstructBuilder())
            {
                builder.Configure(typeof(ComponentWithMultipleInterfaces),
                    DependencyLifecycle.InstancePerCall);

                Assert.True(builder.HasComponent(typeof(ISomeInterface)));

                Assert.True(builder.HasComponent(typeof(ISomeOtherInterface)));

                Assert.True(builder.HasComponent(typeof(IYetAnotherInterface)));

                Assert.AreEqual(1, builder.BuildAll(typeof(IYetAnotherInterface)).Count());
            }
        }

        [Test]
        public void All_implemented_interfaces_should_be_registered_for_func()
        {
            using (var builder = TestContainerBuilder.ConstructBuilder())
            {
                builder.Configure(() => new ComponentWithMultipleInterfaces(), DependencyLifecycle.InstancePerCall);

                Assert.True(builder.HasComponent(typeof(ISomeInterface)));
                Assert.True(builder.HasComponent(typeof(ISomeOtherInterface)));
                Assert.True(builder.HasComponent(typeof(IYetAnotherInterface)));
                Assert.AreEqual(1, builder.BuildAll(typeof(IYetAnotherInterface)).Count());
            }
        }

        [Test]
        public void Multiple_implementations_should_be_supported()
        {
            using (var builder = TestContainerBuilder.ConstructBuilder())
            {
                builder.Configure(typeof(SomeClass), DependencyLifecycle.InstancePerUnitOfWork);
                builder.Configure(typeof(SomeOtherClass), DependencyLifecycle.InstancePerUnitOfWork);

                Assert.NotNull(builder.Build(typeof(SomeClass)));
                Assert.AreEqual(2, builder.BuildAll(typeof(ISomeInterface)).Count());

                using (var childBuilder = builder.BuildChildContainer())
                {
                    Assert.NotNull(childBuilder.Build(typeof(SomeClass)));
                    Assert.AreEqual(2, childBuilder.BuildAll(typeof(ISomeInterface)).Count());
                }
            }
            //Not supported by,typeof(WindsorObjectBuilder));
        }

        [Test]
        public void Given_lookupType_should_be_used_as_service_in_the_registration_when_RegisterSingleton()
        {
            using (var builder = TestContainerBuilder.ConstructBuilder())
            {
                var expected = new InheritedFromSomeClass();
                builder.RegisterSingleton(typeof(SomeClass), expected);

                Assert.NotNull(builder.Build(typeof(SomeClass)));
                Assert.AreEqual(expected, builder.Build(typeof(SomeClass)));

                using (var childBuilder = builder.BuildChildContainer())
                {
                    Assert.NotNull(childBuilder.Build(typeof(SomeClass)));
                    Assert.AreEqual(expected, childBuilder.Build(typeof(SomeClass)));
                }
            }
        }

        [Test]
        public void Generic_interfaces_should_be_registered()
        {
            using (var builder = TestContainerBuilder.ConstructBuilder())
            {
                builder.Configure(typeof(ComponentWithGenericInterface),
                    DependencyLifecycle.InstancePerCall);

                Assert.True(builder.HasComponent(typeof(ISomeGenericInterface<string>)));
            }
        }

        [Test, Ignore("Not sure that we should enforce this")]
        public void System_interfaces_should_not_be_auto_registered()
        {
            using (var builder = TestContainerBuilder.ConstructBuilder())
            {
                builder.Configure(typeof(ComponentWithSystemInterface),
                    DependencyLifecycle.InstancePerCall);

                Assert.False(builder.HasComponent(typeof(IGrouping<string, string>)));
                Assert.False(builder.HasComponent(typeof(IDisposable)));
            }
        }
    }

    public class ComponentThatDependsOnMultiSingletons
    {
        public ISingleton1 Singleton1 { get; set; }
        public ISingleton2 Singleton2 { get; set; }
    }

    public class SingletonThatImplementsToInterfaces : ISingleton2
    {
    }

    public interface ISingleton2 : ISingleton1
    {
    }

    public interface ISingleton1
    {
    }

    public class ComponentWithMultipleInterfaces : ISomeInterface, ISomeOtherInterface
    {
    }

    public class ComponentWithGenericInterface : ISomeGenericInterface<string>
    {
    }

    public class ComponentWithSystemInterface : IGrouping<string, string>, IDisposable
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

    public interface IWithSetterDependencies
    {
    }

    public class ClassWithSetterDependencies : IWithSetterDependencies
    {
        public SomeEnum EnumDependency { get; set; }
        public int SimpleDependency { get; set; }
        public string StringDependency { get; set; }
        public ISomeInterface InterfaceDependency { get; set; }
        public SomeClass ConcreteDependency { get; set; }

        public SomeClass ConcreteDependencyWithSetOnly
        {
            set { concreteDependencyWithSetOnly = value; }
        }

        public SomeClass ConcreteDependencyWithPrivateSet { get; private set; }

        public SomeClass concreteDependencyWithSetOnly;
    }

    public class SomeClass : ISomeInterface
    {
    }

    public class InheritedFromSomeClass : SomeClass
    {
    }

    public class SomeOtherClass : ISomeInterface
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