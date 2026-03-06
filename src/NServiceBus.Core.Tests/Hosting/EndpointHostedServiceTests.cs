namespace NServiceBus.Core.Tests.EndpointHosting;

using System;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;

[TestFixture]
public class EndpointHostedServiceTests
{
    [Test]
    public async Task Should_forward_stop_cancellation_token_to_lifecycle()
    {
        var lifecycle = new RecordingEndpointLifecycle();
        var hostedService = new EndpointHostedService(lifecycle);
        using var tokenSource = new CancellationTokenSource();

        await hostedService.StopAsync(tokenSource.Token).ConfigureAwait(false);

        Assert.That(lifecycle.StopCalls, Is.EqualTo(1));
        Assert.That(lifecycle.LastStopToken, Is.EqualTo(tokenSource.Token));
    }

    [Test]
    public async Task Should_delegate_dispose_to_lifecycle()
    {
        var lifecycle = new RecordingEndpointLifecycle();
        var hostedService = new EndpointHostedService(lifecycle);

        await hostedService.DisposeAsync().ConfigureAwait(false);

        Assert.That(lifecycle.DisposeCalls, Is.EqualTo(1));
    }

    sealed class RecordingEndpointLifecycle : IEndpointLifecycle
    {
        public ValueTask Create(CancellationToken cancellationToken = default) => ValueTask.CompletedTask;

        public ValueTask Start(CancellationToken cancellationToken = default) => ValueTask.CompletedTask;

        public ValueTask Stop(CancellationToken cancellationToken = default)
        {
            LastStopToken = cancellationToken;
            StopCalls++;
            return ValueTask.CompletedTask;
        }

        public ValueTask<IEndpointInstance> CreateAndStart(CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public ValueTask DisposeAsync()
        {
            DisposeCalls++;
            return ValueTask.CompletedTask;
        }

        public CancellationToken LastStopToken { get; private set; }
        public int StopCalls { get; private set; }
        public int DisposeCalls { get; private set; }
    }
}
