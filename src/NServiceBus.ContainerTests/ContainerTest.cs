namespace NServiceBus.ContainerTests
{
    using System;
    using LightInject;
    using LightInject.Microsoft.DependencyInjection;
    using Microsoft.Extensions.DependencyInjection;
    using NUnit.Framework;

    [TestFixtureSource("Containers")]
    public abstract class ContainerTest
    {
        public static TestFixtureData[] Containers =
        {
            new TestFixtureData((Func<IServiceCollection, IServiceProvider>)(serviceCollection =>
            {
                var containerOptions = new ContainerOptions
                {
                    EnableVariance = false
                }.WithMicrosoftSettings();
                return serviceCollection.CreateLightInjectServiceProvider(containerOptions);
            })).SetArgDisplayNames("LightInject"),
            new TestFixtureData((Func<IServiceCollection, IServiceProvider>)(serviceCollection =>
            {
                return serviceCollection.BuildServiceProvider();
            })).SetArgDisplayNames("Microsoft.Extensions.DependencyInjection")
        };

        internal Func<IServiceCollection, IServiceProvider> BuildContainer { get; }

        protected ContainerTest(Func<IServiceCollection, IServiceProvider> buildContainer)
        {
            BuildContainer = buildContainer;
        }
    }
}