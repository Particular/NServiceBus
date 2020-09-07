namespace NServiceBus.ContainerTests
{
    using System;
    using Microsoft.Extensions.DependencyInjection;
    using NServiceBus;
    using NUnit.Framework;
    using ServiceCollection = MicrosoftExtensionsDependencyInjection.ServiceCollection;

    public class When_disposing_the_builder : ContainerTest
    {
        [Ignore("LightInject also disposes externally provided singleton instances")]
        [Test]
        public void Should_dispose_all_IDisposable_components()
        {
            var serviceCollection = new ServiceCollection();

            DisposableComponent.DisposeCalled = false;
            AnotherSingletonComponent.DisposeCalled = false;

            serviceCollection.ConfigureComponent(typeof(DisposableComponent), DependencyLifecycle.SingleInstance);
            serviceCollection.AddSingleton(typeof(AnotherSingletonComponent), new AnotherSingletonComponent());

            var builder = BuildContainer(serviceCollection);
            builder.GetService(typeof(DisposableComponent));
            builder.GetService(typeof(AnotherSingletonComponent));
            (builder as IDisposable)?.Dispose();

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

        public When_disposing_the_builder(Func<IServiceCollection, IServiceProvider> buildContainer) : base(buildContainer)
        {
        }
    }
}