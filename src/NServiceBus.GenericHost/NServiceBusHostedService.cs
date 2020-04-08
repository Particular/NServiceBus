﻿namespace NServiceBus.Extensions.Hosting
{
    using System;
    using System.Runtime.ExceptionServices;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Hosting;
    using Microsoft.Extensions.Logging;
    using NServiceBus;

    class NServiceBusHostedService : IHostedService
    {
        public NServiceBusHostedService(IStartableEndpointWithExternallyManagedContainer startableEndpoint, IServiceProvider serviceProvider, ExceptionDispatchInfo exception)
        {
            this.startableEndpoint = startableEndpoint;
            this.serviceProvider = serviceProvider;
            this.exception = exception;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            if (exception != null)
            {
                var logger = serviceProvider.GetRequiredService<ILogger<NServiceBusHostedService>>();

                logger.LogCritical(exception.SourceException, "Error starting the endpoint");

                exception.Throw();
            }

            endpoint = await startableEndpoint.Start(new ServiceProviderAdapter(serviceProvider))
                .ConfigureAwait(false);
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return endpoint.Stop();
        }

        IEndpointInstance endpoint;
        readonly IStartableEndpointWithExternallyManagedContainer startableEndpoint;
        readonly IServiceProvider serviceProvider;
        readonly ExceptionDispatchInfo exception;
    }
}