namespace NServiceBus.ContainerTests
{
    using System;
    using MicrosoftExtensionsDependencyInjection;
    using NServiceBus;
    using NUnit.Framework;

    [TestFixture]
    public class When_disposing_the_builder
    {
        [Test]
        public void Should_dispose_all_IDisposable_components()
        {
            var serviceCollection = new ServiceCollection();
            var configureComponents = new CommonObjectBuilder(serviceCollection);
            
            DisposableComponent.DisposeCalled = false;
            AnotherSingletonComponent.DisposeCalled = false;

            configureComponents.ConfigureComponent(typeof(DisposableComponent), DependencyLifecycle.SingleInstance);
            configureComponents.RegisterSingleton(typeof(AnotherSingletonComponent), new AnotherSingletonComponent());

            var builder = TestContainerBuilder.CreateServiceProvider(serviceCollection);
            builder.GetService(typeof(DisposableComponent));
            builder.GetService(typeof(AnotherSingletonComponent));
            (builder as IDisposable)?.Dispose();

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