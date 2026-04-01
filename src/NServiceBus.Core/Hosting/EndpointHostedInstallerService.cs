#nullable enable
namespace NServiceBus;

using System;
using System.Threading;
using System.Threading.Tasks;
using Installation;
using Microsoft.Extensions.Hosting;

class EndpointHostedInstallerService(InstallerWithExternallyManagedContainer externallyManagedInstallerHost, IServiceProvider sp) : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken = default) => externallyManagedInstallerHost.Setup(sp, cancellationToken);

    public Task StopAsync(CancellationToken cancellationToken= default) => Task.CompletedTask;
}