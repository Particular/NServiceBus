namespace NServiceBus.GenericHost
{
    using System;
    using Extensions.Hosting;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Hosting;

    public static class NServiceBusHostBuilderExtensions
    {
        /// <summary>
        /// Configures the host to start an NServiceBus endpoint.
        /// </summary>
        public static IHostBuilder UseNServiceBus(this IHostBuilder hostBuilder, Func<HostBuilderContext, EndpointConfiguration> endpointConfigurationBuilder)
        {
            hostBuilder.ConfigureServices((ctx, serviceCollection) =>
            {
                var endpointConfiguration = endpointConfigurationBuilder(ctx);
                var (startableEndpoint, exception) = EndpointWithExternallyManagedContainer.Create(endpointConfiguration, new ServiceCollectionAdapter(serviceCollection));

                serviceCollection.AddSingleton(_ => startableEndpoint.MessageSession.Value);
                serviceCollection.AddSingleton<IHostedService>(serviceProvider => new NServiceBusHostedService(startableEndpoint, serviceProvider, exception));
            });

            return hostBuilder;
        }
    }
}