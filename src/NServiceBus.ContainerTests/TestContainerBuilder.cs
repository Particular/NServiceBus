namespace NServiceBus.ContainerTests
{
    using System;
    using LightInject;
    using LightInject.Microsoft.DependencyInjection;
    using Microsoft.Extensions.DependencyInjection;

    public static class TestContainerBuilder
    {
        public static Func<IServiceCollection, IServiceProvider> CreateServiceProvider = serviceCollection =>
        {
            var containerOptions = new ContainerOptions
            {
                EnableVariance = false
            }.WithMicrosoftSettings();
            return serviceCollection.CreateLightInjectServiceProvider(containerOptions);
        };
    }
}