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
            // We don't have host support in the acceptance tests, so we need to manually start/stop the services
            var hostedServices = provider.GetServices<IHostedService>().ToList();
            foreach (var hostedService in hostedServices)
            {
                await hostedService.StartAsync(cancellationToken);
            }

            hostedServices.Reverse();
            foreach (var hostedService in hostedServices)
            {
                await hostedService.StopAsync(cancellationToken);

                if (hostedService is IAsyncDisposable asyncDisposable)
                {
                    await asyncDisposable.DisposeAsync();
                }
            }
        }
    }
}