#nullable enable

namespace NServiceBus;

using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;

sealed class InstallerCoordinatorHostedService(IHostApplicationLifetime hostApplicationLifetime) : IHostedLifecycleService
{
    public Task StartingAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task StartedAsync(CancellationToken cancellationToken = default)
    {
        hostApplicationLifetime.StopApplication();
        return Task.CompletedTask;
    }

    public Task StartAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task StopAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task StoppingAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task StoppedAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
}