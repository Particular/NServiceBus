#nullable enable

namespace NServiceBus;

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;

sealed class EndpointHostedService(IEndpointLifecycle endpointLifecycle) : IHostedLifecycleService, IAsyncDisposable
{
    public async Task StartingAsync(CancellationToken cancellationToken = default) => await endpointLifecycle.Create(cancellationToken).ConfigureAwait(false);

    public Task StartedAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task StoppingAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task StoppedAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

    public ValueTask DisposeAsync() => endpointLifecycle.DisposeAsync();

    public async Task StartAsync(CancellationToken cancellationToken = default) => await endpointLifecycle.Start(cancellationToken).ConfigureAwait(false);

    public Task StopAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
}