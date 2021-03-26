namespace NServiceBus.ContainerTests
{
    using System;
    using Microsoft.Extensions.DependencyInjection;
    using NUnit.Framework;

    public class When_disposing_the_builder
    {
        [Test]
        public void Should_dispose_all_IDisposable_components()
        {
            var serviceCollection = new ServiceCollection();

            DisposableComponent.DisposeCalled = false;
            AnotherSingletonComponent.DisposeCalled = false;

            serviceCollection.AddSingleton(typeof(DisposableComponent));
            serviceCollection.AddSingleton(typeof(AnotherSingletonComponent), new AnotherSingletonComponent());

            var serviceProvider = serviceCollection.BuildServiceProvider();
            serviceProvider.GetService(typeof(DisposableComponent));
            serviceProvider.GetService(typeof(AnotherSingletonComponent));
            (serviceProvider as IDisposable)?.Dispose();

            Assert.True(DisposableComponent.DisposeCalled, "Dispose should be called on DisposableComponent");
            Assert.False(AnotherSingletonComponent.DisposeCalled, "Dispose should not be called on AnotherSingletonComponent");
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