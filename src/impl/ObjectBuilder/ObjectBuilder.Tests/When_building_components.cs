using System;
using NServiceBus.ObjectBuilder;
using NServiceBus.ObjectBuilder.Common;
using NServiceBus.ObjectBuilder.Spring;
using NUnit.Framework;

namespace ObjectBuilder.Tests
{
    [TestFixture]
    public class When_building_components:BuilderFixture
    {
        [Test]
        public void Singleton_components_should_yield_the_same_instance()
        {
            ForAllBuilders((builder) =>
               Assert.AreEqual(builder.Build(typeof(SingletonComponent)),builder.Build(typeof(SingletonComponent))));
        }

        [Test]
        public void Singlecall_components_should_yield_unique_instances()
        {
            ForAllBuilders((builder) =>
               Assert.AreNotEqual(builder.Build(typeof(SinglecallComponent)),builder.Build(typeof(SinglecallComponent))));
        }

        [Test]
        public void Reguesting_an_unregistered_component_should_throw()
        {
            
            ForAllBuilders((builder)=> 
                Assert.That(() => builder.Build(typeof (UnregisteredComponent)),
                Throws.Exception));
        }

        [Test]
        public void Should_be_able_to_build_components_registered_after_first_build()
        {
            //first build call
            ForAllBuilders(builder=>builder.Build(typeof(SingletonComponent)));

            //register new component
            ForAllBuilders(builder=>builder.Configure(typeof(UnregisteredComponent), DependencyLifecycle.SingleInstance));

            //should be able to build the newly registered component
            ForAllBuilders((builder) =>
               Assert.AreEqual(builder.Build(typeof(UnregisteredComponent)), builder.Build(typeof(UnregisteredComponent))), 
               typeof(SpringObjectBuilder));
        }

        protected override Action<IContainer> InitializeBuilder()
        {
            return (config) =>
                       {
                           config.Configure(typeof(SingletonComponent),DependencyLifecycle.SingleInstance);
                           config.Configure(typeof(SinglecallComponent), DependencyLifecycle.InstancePerCall);
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
        }
    }
}