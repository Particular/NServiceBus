#nullable enable

namespace NServiceBus;

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;

sealed class EndpointHostedService(IEndpointLifecycle endpointLifecycle, InstallersOptions? installersOptions) : IHostedLifecycleService, IAsyncDisposable
{
    public async Task StartingAsync(CancellationToken cancellationToken = default)
    {
        var forceInstallers = installersOptions?.Enabled ?? false;
        await endpointLifecycle.Create(forceInstallers, cancellationToken).ConfigureAwait(false);
    }

    public Task StartedAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task StoppingAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task StoppedAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

    public ValueTask DisposeAsync() => endpointLifecycle.DisposeAsync();

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        if (installersOptions?.Enabled ?? false)
        {
            return;
        }

        await endpointLifecycle.Start(cancellationToken).ConfigureAwait(false);
    }

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        if (installersOptions?.Enabled ?? false)
        {
            return;
        }

        await endpointLifecycle.Stop(cancellationToken).ConfigureAwait(false);
    }
}