using System;
using NServiceBus;
using NServiceBus.ObjectBuilder.Spring;
using NUnit.Framework;

namespace ObjectBuilder.Tests
{
    using System.Collections.Generic;
    using System.Threading;
    using NServiceBus.ObjectBuilder.CastleWindsor;
    using NServiceBus.ObjectBuilder.Ninject;
    using StructureMap;
    using IContainer = NServiceBus.ObjectBuilder.Common.IContainer;

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
        public void Instance_per_uow__components_should_not_be_shared_across_child_containers()
        {
            ForAllBuilders(builder =>
            {
                builder.Configure(typeof(InstancePerUoWComponent), DependencyLifecycle.InstancePerUnitOfWork);
              
                results = new List<object>();
                var thread1 = new Thread(ResolveChildInstance);
                thread1.Start(builder);

                var thread2 = new Thread(ResolveChildInstance);
                thread2.Start(builder);

                thread1.Join();
                thread2.Join();


                Assert.AreNotSame(results[0], results[1]);

            },
            typeof(SpringObjectBuilder));

        }

        static List<object> results;

        void ResolveChildInstance(object container)
        {
            var mainContainer = (IContainer)container;

            results.Add(mainContainer.BuildChildContainer().Build(typeof(InstancePerUoWComponent)));
        }

        [Test]
        public void Instance_per_call_components_should_not_be_shared_across_child_containers()
        {
            ForAllBuilders(builder =>
            {
                builder.Configure(typeof(InstancePerCallComponent), DependencyLifecycle.InstancePerCall);

                var nestedContainer = builder.BuildChildContainer();
                var anotherNestedContainer = builder.BuildChildContainer();

                Assert.AreNotSame(nestedContainer.Build(typeof(InstancePerCallComponent)), anotherNestedContainer.Build(typeof(InstancePerCallComponent)));
            });

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
            typeof(SpringObjectBuilder));
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

    public class InstancePerCallComponent
    {
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