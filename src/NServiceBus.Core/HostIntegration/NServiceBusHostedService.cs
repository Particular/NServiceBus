#nullable enable

namespace NServiceBus;

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;

sealed class NServiceBusHostedService(IEndpointStarter endpointStarter) : IHostedLifecycleService, IAsyncDisposable
{
    public async Task StartingAsync(CancellationToken cancellationToken) => await endpointStarter.GetOrStart(cancellationToken).ConfigureAwait(false);

    public Task StartedAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public Task StoppingAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public Task StoppedAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public async ValueTask DisposeAsync()
    {
        await endpointStarter.DisposeAsync().ConfigureAwait(false);
    }

    public Task StartAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}