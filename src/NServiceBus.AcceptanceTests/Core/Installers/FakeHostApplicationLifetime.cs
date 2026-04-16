namespace NServiceBus.AcceptanceTests.Core.Installers;

using System.Threading;
using Microsoft.Extensions.Hosting;

class FakeHostApplicationLifetime : IHostApplicationLifetime
{
    readonly CancellationTokenSource stoppingSource = new();

    public CancellationToken ApplicationStarted => CancellationToken.None;
    public CancellationToken ApplicationStopping => stoppingSource.Token;
    public CancellationToken ApplicationStopped => CancellationToken.None;

    public void StopApplication() => stoppingSource.Cancel();
}