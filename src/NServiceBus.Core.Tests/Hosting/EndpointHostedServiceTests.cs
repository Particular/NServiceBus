namespace NServiceBus.Core.Tests.Host;

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
        var hostedService = new EndpointHostedService(lifecycle, null);
        using var tokenSource = new CancellationTokenSource();

        await hostedService.StopAsync(tokenSource.Token).ConfigureAwait(false);

        Assert.That(lifecycle.StopCalls, Is.EqualTo(1));
        Assert.That(lifecycle.LastStopToken, Is.EqualTo(tokenSource.Token));
    }

    [Test]
    public async Task Should_delegate_dispose_to_lifecycle()
    {
        var lifecycle = new RecordingEndpointLifecycle();
        var hostedService = new EndpointHostedService(lifecycle, null);

        await hostedService.DisposeAsync().ConfigureAwait(false);

        Assert.That(lifecycle.DisposeCalls, Is.EqualTo(1));
    }

    [Test]
    public async Task Should_create_and_start_endpoint_when_installers_not_enabled()
    {
        var lifecycle = new RecordingEndpointLifecycle();
        var hostedService = new EndpointHostedService(lifecycle, new InstallersOptions());

        await hostedService.StartingAsync().ConfigureAwait(false);
        await hostedService.StartAsync().ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(lifecycle.CreateCalls, Is.EqualTo(1));
            Assert.That(lifecycle.StartCalls, Is.EqualTo(1));
        }
    }

    [Test]
    public async Task Should_create_but_not_start_endpoint_when_installers_enabled()
    {
        var lifecycle = new RecordingEndpointLifecycle();
        var hostedService = new EndpointHostedService(lifecycle, new InstallersOptions { Enabled = true });

        await hostedService.StartingAsync().ConfigureAwait(false);
        await hostedService.StartAsync().ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(lifecycle.CreateCalls, Is.EqualTo(1));
            Assert.That(lifecycle.StartCalls, Is.Zero);
        }
    }

    [Test]
    public async Task Should_forward_stop_to_lifecycle_when_installers_enabled()
    {
        var lifecycle = new RecordingEndpointLifecycle();
        var hostedService = new EndpointHostedService(lifecycle, new InstallersOptions { Enabled = true });

        await hostedService.StopAsync().ConfigureAwait(false);

        Assert.That(lifecycle.StopCalls, Is.EqualTo(1));
    }

    sealed class RecordingEndpointLifecycle : IEndpointLifecycle
    {
        public ValueTask Create(CancellationToken cancellationToken = default)
        {
            CreateCalls++;
            return ValueTask.CompletedTask;
        }

        public ValueTask Start(CancellationToken cancellationToken = default)
        {
            StartCalls++;
            return ValueTask.CompletedTask;
        }

        public ValueTask Stop(CancellationToken cancellationToken = default)
        {
            LastStopToken = cancellationToken;
            StopCalls++;
            return ValueTask.CompletedTask;
        }

        public ValueTask<RunningEndpointInstance> CreateAndStart(CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public ValueTask DisposeAsync()
        {
            DisposeCalls++;
            return ValueTask.CompletedTask;
        }

        public CancellationToken LastStopToken { get; private set; }
        public int CreateCalls { get; private set; }
        public int StartCalls { get; private set; }
        public int StopCalls { get; private set; }
        public int DisposeCalls { get; private set; }
    }
}