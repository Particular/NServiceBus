namespace ObjectBuilder.Tests
{
    using System;
    using System.Diagnostics;
    using System.Threading.Tasks;
    using NServiceBus;
    using NServiceBus.ObjectBuilder.Common;
    using NServiceBus.ObjectBuilder.Spring;
    using NUnit.Framework;

    [TestFixture]
    public class When_disposing_the_builder : BuilderFixture
    {
        [Test]
        public void Should_dispose_all_IDisposable_components()
        {
            ForAllBuilders(builder =>
                {
                    DisposableComponent.DisposeCalled = false;
                    AnotherSingletonComponent.DisposeCalled = false;

                    builder.Configure(typeof(DisposableComponent), DependencyLifecycle.SingleInstance);
                    builder.RegisterSingleton(typeof(AnotherSingletonComponent), new AnotherSingletonComponent());

                    builder.Build(typeof(DisposableComponent));
                    builder.Build(typeof(AnotherSingletonComponent));
                    builder.Dispose();

                    Assert.True(DisposableComponent.DisposeCalled, "Dispose should be called on DisposableComponent");
                    Assert.True(AnotherSingletonComponent.DisposeCalled, "Dispose should be called on AnotherSingletonComponent");
                });
        }
        [Test]
        public void When_circular_ref_exists_between_container_and_builder_should_not_infinite_loop()
        {
            ForAllBuilders(builder =>
                {
                        Debug.WriteLine("Trying " + builder.GetType().Name);
                        builder.RegisterSingleton(builder.GetType(), builder);
                        builder.Dispose();
                });
        }

        [Test]
        public void Should_dispose_all_IDisposable_components_in_child_container()
        {
            ForAllBuilders(main =>
            {
                DisposableComponent.DisposeCalled = false;
                AnotherSingletonComponent.DisposeCalled = false;

                main.RegisterSingleton(typeof(AnotherSingletonComponent), new AnotherSingletonComponent());
                main.Configure(typeof(DisposableComponent), DependencyLifecycle.InstancePerUnitOfWork);

                using (var builder = main.BuildChildContainer())
                {
                    builder.Build(typeof(DisposableComponent));
                }

                Assert.False(AnotherSingletonComponent.DisposeCalled, "Dispose should not be called on AnotherSingletonComponent because it belongs to main container");
                Assert.True(DisposableComponent.DisposeCalled, "Dispose should be called on DisposableComponent");
            }, typeof(SpringObjectBuilder));
        }

        [Test, Ignore]
        public void Spring_only_Should_dispose_all_IDisposable_components_only_when_then_main_container_is_disposed()
        {
            using (var container = (IContainer) new SpringObjectBuilder())
            {
                DisposableComponent.DisposeCalled = false;
                AnotherSingletonComponent.DisposeCalled = false;

                container.RegisterSingleton(typeof(AnotherSingletonComponent), new AnotherSingletonComponent());
                container.Configure(typeof(DisposableComponent), DependencyLifecycle.InstancePerUnitOfWork);

                Task.Factory.StartNew(
                    () =>
                        {
                            using (var builder = container.BuildChildContainer())
                            {
                                builder.Build(typeof (DisposableComponent));
                            }
                        }, TaskCreationOptions.LongRunning).Wait();

                Assert.False(DisposableComponent.DisposeCalled, "Dispose should not be called on DisposableComponent because Spring does not support child containers!");
            }

            Assert.True(DisposableComponent.DisposeCalled, "Dispose should be called on DisposableComponent");
            Assert.True(AnotherSingletonComponent.DisposeCalled, "Dispose should be called on AnotherSingletonComponent");
        }

        public class DisposableComponent : IDisposable
        {
            public static bool DisposeCalled;

            public void Dispose()
            {
                DisposeCalled = true;
            }
        }

        public class AnotherSingletonComponent : IDisposable
        {
            public static bool DisposeCalled;

            public void Dispose()
            {
                DisposeCalled = true;
            }
        }
    }
}