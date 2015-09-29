namespace NServiceBus.ContainerTests
{
    using NServiceBus;
    using NServiceBus.ObjectBuilder.Common;
    using NUnit.Framework;

    [TestFixture]
    public class When_building_components
    {
        [Test]
        public void Singleton_components_should_get_their_dependencies_autowired()
        {
            using (var builder = TestContainerBuilder.ConstructBuilder())
            {
                builder.RegisterSingleton(typeof(ISingletonComponentWithPropertyDependency), new SingletonComponentWithPropertyDependency());
                builder.RegisterSingleton(typeof(SingletonComponent), new SingletonComponent());

                var singleton = (SingletonComponentWithPropertyDependency)builder.Build(typeof(ISingletonComponentWithPropertyDependency));
                Assert.IsNotNull(singleton.Dependency);
            }
        }

        [Test]
        public void Singleton_components_should_yield_the_same_instance()
        {
            using (var builder = TestContainerBuilder.ConstructBuilder())
            {
                InitializeBuilder(builder);
                Assert.AreEqual(builder.Build(typeof(SingletonComponent)), builder.Build(typeof(SingletonComponent)));
            }
        }

        [Test]
        public void Singlecall_components_should_yield_unique_instances()
        {
            using (var builder = TestContainerBuilder.ConstructBuilder())
            {
                InitializeBuilder(builder);
                Assert.AreNotEqual(builder.Build(typeof(SinglecallComponent)), builder.Build(typeof(SinglecallComponent)));
            }
        }

        [Test]
        public void UoW_components_should_resolve_from_main_container()
        {
            using (var builder = TestContainerBuilder.ConstructBuilder())
            {
                InitializeBuilder(builder);
                Assert.NotNull(builder.Build(typeof(InstancePerUoWComponent)));
            }
            //Not supported by typeof(WindsorObjectBuilder));
        }

        [Test]
        public void Lambda_uow_components_should_resolve_from_main_container()
        {
            using (var builder = TestContainerBuilder.ConstructBuilder())
            {
                InitializeBuilder(builder);
                Assert.NotNull(builder.Build(typeof(LambdaComponentUoW)));
            }
            //Not supported by typeof(WindsorObjectBuilder));
        }

        [Test]
        public void Lambda_singlecall_components_should_yield_unique_instances()
        {
            using (var builder = TestContainerBuilder.ConstructBuilder())
            {
                InitializeBuilder(builder);
                Assert.AreNotEqual(builder.Build(typeof(SingleCallLambdaComponent)), builder.Build(typeof(SingleCallLambdaComponent)));
            }
        }

        [Test]
        public void Lambda_singleton_components_should_yield_the_same_instance()
        {
            using (var builder = TestContainerBuilder.ConstructBuilder())
            {
                InitializeBuilder(builder);
                Assert.AreEqual(builder.Build(typeof(SingletonLambdaComponent)), builder.Build(typeof(SingletonLambdaComponent)));
            }
        }

        [Test]
        public void Requesting_an_unregistered_component_should_throw()
        {
            using (var builder = TestContainerBuilder.ConstructBuilder())
            {
                InitializeBuilder(builder);
                Assert.That(() => builder.Build(typeof(UnregisteredComponent)),Throws.Exception);
            }
        }

        [Test]
        public void Should_be_able_to_build_components_registered_after_first_build()
        {
            using (var builder = TestContainerBuilder.ConstructBuilder())
            {
                InitializeBuilder(builder);
                builder.Build(typeof(SingletonComponent));
                builder.Configure(typeof(UnregisteredComponent), DependencyLifecycle.SingleInstance);

                var unregisteredComponent = builder.Build(typeof(UnregisteredComponent)) as UnregisteredComponent;
                Assert.NotNull(unregisteredComponent);
                Assert.NotNull(unregisteredComponent.SingletonComponent);
            }
            //Not supported by,typeof(SpringObjectBuilder));
        }

        [Test]
        public void Should_support_mixed_dependency_styles()
        {
            using (var builder = TestContainerBuilder.ConstructBuilder())
            {
                InitializeBuilder(builder);
                builder.Configure(typeof(ComponentWithBothConstructorAndSetterInjection), DependencyLifecycle.InstancePerCall);
                builder.Configure(typeof(ConstructorDependency), DependencyLifecycle.InstancePerCall);
                builder.Configure(typeof(SetterDependency), DependencyLifecycle.InstancePerCall);

                var component = (ComponentWithBothConstructorAndSetterInjection) builder.Build(typeof(ComponentWithBothConstructorAndSetterInjection));
                Assert.NotNull(component.ConstructorDependency);
                Assert.NotNull(component.SetterDependency);
            }

            //Not supported by, typeof(SpringObjectBuilder));
        }


        void InitializeBuilder(IContainer container)
        {
            container.Configure(typeof(SingletonComponent), DependencyLifecycle.SingleInstance);
            container.Configure(typeof(SinglecallComponent), DependencyLifecycle.InstancePerCall);
            container.Configure(typeof(InstancePerUoWComponent), DependencyLifecycle.InstancePerUnitOfWork);
            container.Configure(() => new SingletonLambdaComponent(), DependencyLifecycle.SingleInstance);
            container.Configure(() => new SingleCallLambdaComponent(), DependencyLifecycle.InstancePerCall);
            container.Configure(() => new LambdaComponentUoW(), DependencyLifecycle.InstancePerUnitOfWork);
        }

        public class SingletonComponent
        {
        }

        public interface ISingletonComponentWithPropertyDependency
        {
             
        }

        public class SingletonComponentWithPropertyDependency : ISingletonComponentWithPropertyDependency
        {
            public SingletonComponent Dependency { get; set; }
        }

        public class SinglecallComponent
        {
        }

        public class UnregisteredComponent
        {
            public SingletonComponent SingletonComponent { get; set; }
        }

        public class SingletonLambdaComponent
        {
        }

        public class LambdaComponentUoW
        {
        }

        public class SingleCallLambdaComponent
        {
        }
    }

    public class StaticFactory
    {
        public ComponentCreatedByFactory Create()
        {
            return new ComponentCreatedByFactory();
        }
    }

    public class ComponentCreatedByFactory
    {
    }

    public class ComponentWithBothConstructorAndSetterInjection
    {
        public ComponentWithBothConstructorAndSetterInjection(ConstructorDependency constructorDependency)
        {
            ConstructorDependency = constructorDependency;
        }

        public ConstructorDependency ConstructorDependency { get; }

        public SetterDependency SetterDependency { get; set; }
    }

    public class ConstructorDependency
    {
    }

    public class SetterDependency
    {
    }
}