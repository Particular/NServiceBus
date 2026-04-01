#nullable enable
namespace NServiceBus;

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;

class EndpointHostedInstallerService(IEndpointLifecycle endpointLifecycle) : IHostedService, IAsyncDisposable
{
    public async Task StartAsync(CancellationToken cancellationToken = default)
        // we only ever create but not start
        => await endpointLifecycle.Create(cancellationToken).ConfigureAwait(false);

    public Task StopAsync(CancellationToken cancellationToken= default) => Task.CompletedTask;

    public ValueTask DisposeAsync() => endpointLifecycle.DisposeAsync();
}