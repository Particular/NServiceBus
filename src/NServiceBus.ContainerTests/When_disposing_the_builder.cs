namespace NServiceBus.ContainerTests
{
    using System;
    using System.Diagnostics;
    using NServiceBus;
    using NUnit.Framework;

    [TestFixture]
    public class When_disposing_the_builder
    {
        [Test]
        public void Should_dispose_all_IDisposable_components()
        {
            var builder = TestContainerBuilder.ConstructBuilder();
            DisposableComponent.DisposeCalled = false;
            AnotherSingletonComponent.DisposeCalled = false;

            builder.Configure(typeof(DisposableComponent), DependencyLifecycle.SingleInstance);
            builder.RegisterSingleton(typeof(AnotherSingletonComponent), new AnotherSingletonComponent());

            builder.Build(typeof(DisposableComponent));
            builder.Build(typeof(AnotherSingletonComponent));
            builder.Dispose();

            Assert.True(DisposableComponent.DisposeCalled, "Dispose should be called on DisposableComponent");
            Assert.True(AnotherSingletonComponent.DisposeCalled, "Dispose should be called on AnotherSingletonComponent");
        }

        [Test]
        public void When_circular_ref_exists_between_container_and_builder_should_not_infinite_loop()
        {
            var builder = TestContainerBuilder.ConstructBuilder();
            Debug.WriteLine("Trying " + builder.GetType().Name);
            builder.RegisterSingleton(builder.GetType(), builder);
            builder.Dispose();
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