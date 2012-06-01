using System;
using NServiceBus.ObjectBuilder;
using NServiceBus.ObjectBuilder.Common;
using NServiceBus.ObjectBuilder.Ninject;
using NServiceBus.ObjectBuilder.Spring;
using NServiceBus.ObjectBuilder.Unity;
using NUnit.Framework;

namespace ObjectBuilder.Tests
{
    using NServiceBus;
    using NServiceBus.ObjectBuilder.CastleWindsor;

    [TestFixture]
    public class When_building_components : BuilderFixture
    {
        [Test]
        public void Singleton_components_should_yield_the_same_instance()
        {
            ForAllBuilders((builder) =>
               Assert.AreEqual(builder.Build(typeof(SingletonComponent)), builder.Build(typeof(SingletonComponent))));
        }

        [Test]
        public void Singlecall_components_should_yield_unique_instances()
        {
            ForAllBuilders((builder) =>
               Assert.AreNotEqual(builder.Build(typeof(SinglecallComponent)), builder.Build(typeof(SinglecallComponent))));
        }

        [Test]
        public void UoW_components_should_resolve_from_main_container()
        {
            ForAllBuilders((builder) =>
               Assert.NotNull(builder.Build(typeof(InstancePerUoWComponent)))
               , typeof(WindsorObjectBuilder));
        }

        [Test]
        public void Lambda_uow_components_should_resolve_from_main_container()
        {            
            ForAllBuilders((builder) =>
               Assert.NotNull(builder.Build(typeof(LambdaComponentUoW))),               
               typeof(WindsorObjectBuilder), typeof(SpringObjectBuilder), typeof(UnityObjectBuilder));
        }

        [Test]
        public void Lambda_singlecall_components_should_yield_unique_instances()
        {
            ForAllBuilders((builder) =>
               Assert.AreNotEqual(builder.Build(typeof(SingleCallLambdaComponent)), builder.Build(typeof(SingleCallLambdaComponent))),
               typeof(SpringObjectBuilder), typeof(UnityObjectBuilder));
        }

        [Test]
        public void Lambda_singleton_components_should_yield_the_same_instance()
        {
            ForAllBuilders((builder) =>
               Assert.AreEqual(builder.Build(typeof(SingletonLambdaComponent)), builder.Build(typeof(SingletonLambdaComponent))),
               typeof(SpringObjectBuilder), typeof(UnityObjectBuilder));
        }

        [Test]
        public void Reguesting_an_unregistered_component_should_throw()
        {

            ForAllBuilders((builder) =>
                Assert.That(() => builder.Build(typeof(UnregisteredComponent)),
                Throws.Exception));
        }

        [Test]
        public void Should_be_able_to_build_components_registered_after_first_build()
        {
            ForAllBuilders(builder =>
                               {
                                   builder.Build(typeof (SingletonComponent));
                                   builder.Configure(typeof (UnregisteredComponent), DependencyLifecycle.SingleInstance);

                                   var unregisteredComponent = builder.Build(typeof(UnregisteredComponent)) as UnregisteredComponent;
                                   Assert.NotNull(unregisteredComponent);
                                   Assert.NotNull(unregisteredComponent.SingletonComponent);
                               }
               ,typeof(SpringObjectBuilder));
        }

 
        protected override Action<IContainer> InitializeBuilder()
        {
            return (config) =>
                       {
                           config.Configure(typeof(SingletonComponent), DependencyLifecycle.SingleInstance);
                           config.Configure(typeof(SinglecallComponent), DependencyLifecycle.InstancePerCall);
                           config.Configure(typeof(InstancePerUoWComponent), DependencyLifecycle.InstancePerUnitOfWork);
                           config.Configure(() => new SingletonLambdaComponent(), DependencyLifecycle.SingleInstance);
                           config.Configure(() => new SingleCallLambdaComponent(), DependencyLifecycle.InstancePerCall);
                           config.Configure(() => new LambdaComponentUoW(), DependencyLifecycle.InstancePerUnitOfWork);
                       };
        }

        public class SingletonComponent
        {
        }
        public class SinglecallComponent
        {
        }
        public class UnregisteredComponent
        {
            public SingletonComponent SingletonComponent { get; set; }
        }
        public class SingletonLambdaComponent { }
        public class LambdaComponentUoW { }
        public class SingleCallLambdaComponent { }
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
}