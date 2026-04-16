namespace NServiceBus.AcceptanceTests.Core.Installers;

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

static class ServiceProviderExtensions
{
    extension(IServiceProvider provider)
    {
        public async Task RunHostedServices(CancellationToken cancellationToken = default)
        {
            // Simulate the generic host lifecycle:
            // StartingAsync -> StartAsync -> StartedAsync -> (StopApplication) -> StoppingAsync -> StopAsync -> StoppedAsync
            var hostedServices = provider.GetServices<IHostedService>().ToList();

            foreach (var hostedService in hostedServices)
            {
                if (hostedService is IHostedLifecycleService lifecycleService)
                {
                    await lifecycleService.StartingAsync(cancellationToken);
                }
            }

            foreach (var hostedService in hostedServices)
            {
                await hostedService.StartAsync(cancellationToken);
            }

            foreach (var hostedService in hostedServices)
            {
                if (hostedService is IHostedLifecycleService lifecycleService)
                {
                    await lifecycleService.StartedAsync(cancellationToken);
                }
            }

            hostedServices.Reverse();
            foreach (var hostedService in hostedServices)
            {
                if (hostedService is IHostedLifecycleService lifecycleService)
                {
                    await lifecycleService.StoppingAsync(cancellationToken);
                }
            }

            foreach (var hostedService in hostedServices)
            {
                await hostedService.StopAsync(cancellationToken);
            }

            foreach (var hostedService in hostedServices)
            {
                if (hostedService is IHostedLifecycleService lifecycleService)
                {
                    await lifecycleService.StoppedAsync(cancellationToken);
                }
            }

            foreach (var hostedService in hostedServices)
            {
                if (hostedService is IAsyncDisposable asyncDisposable)
                {
                    await asyncDisposable.DisposeAsync();
                }
            }
        }
    }
}