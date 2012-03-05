using System;
using NServiceBus;
using NServiceBus.ObjectBuilder.Spring;
using NUnit.Framework;

namespace ObjectBuilder.Tests
{
    using NServiceBus.ObjectBuilder.Ninject;
    using NServiceBus.ObjectBuilder.Unity;
    using StructureMap;

    [TestFixture]
    public class When_using_nested_containers : BuilderFixture
    {

        [Test]
        public void Instance_per_uow__components_should_be_disposed_when_the_child_container_is_disposed()
        {
            ForAllBuilders(builder =>
            {
                builder.Configure(typeof(InstancePerUoWComponent), DependencyLifecycle.InstancePerUnitOfWork);
            
                using (var nestedContainer = builder.BuildChildContainer())
                    nestedContainer.Build(typeof(InstancePerUoWComponent));

                Assert.True(InstancePerUoWComponent.DisposeCalled);
            },
            typeof(SpringObjectBuilder));

        }

        [Test]
        public void UoW_components_in_the_parent_container_should_be_singletons_in_the_child_container()
        {
            ForAllBuilders(builder =>
            {
                builder.Configure(typeof(InstancePerUoWComponent), DependencyLifecycle.InstancePerUnitOfWork);

                var nestedContainer = builder.BuildChildContainer();

                Assert.AreEqual(nestedContainer.Build(typeof(InstancePerUoWComponent)), nestedContainer.Build(typeof(InstancePerUoWComponent)));
            },
            typeof(SpringObjectBuilder),
            typeof(NinjectObjectBuilder));
        }

       
        [Test]
        public void Should_not_dispose_singletons_when_container_goes_out_of_scope()
        {

            ForAllBuilders(builder =>
            {
                var singletonInMainContainer = new SingletonComponent();

                builder.RegisterSingleton(typeof(ISingletonComponent), singletonInMainContainer);
                builder.Configure(typeof(ComponentThatDependsOfSingleton), DependencyLifecycle.InstancePerUnitOfWork);

                using (var nestedContainer = builder.BuildChildContainer())
                    nestedContainer.Build(typeof(ComponentThatDependsOfSingleton));

                Console.WriteLine(ObjectFactory.WhatDoIHave());
                Assert.False(SingletonComponent.DisposeCalled);
            },
            typeof(SpringObjectBuilder));
        }
    }

    public class ComponentThatDependsOfSingleton : IDisposable
    {

        public ISingletonComponent SingletonComponent { get; set; }

        public static bool DisposeCalled;

        public void Dispose()
        {
            DisposeCalled = true;
        }
    }

    public class InstancePerUoWComponent : IDisposable
    {
        public static bool DisposeCalled;

        public void Dispose()
        {
            DisposeCalled = true;
        }
    }

    public class SingletonComponent : ISingletonComponent, IDisposable
    {
        public static bool DisposeCalled;

        public void Dispose()
        {
            DisposeCalled = true;
        }
    }

    public class AnotherSingletonComponent : ISingletonComponent, IDisposable
    {
        public static bool DisposeCalled;

        public void Dispose()
        {
            DisposeCalled = true;
        }
    }

    public interface ISingletonComponent
    {
    }
}