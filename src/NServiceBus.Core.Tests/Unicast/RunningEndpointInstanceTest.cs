namespace NServiceBus.Unicast.Tests;

using System;
using System.Threading;
using System.Threading.Tasks;
using Core.Tests.Features;
using Features;
using NUnit.Framework;
using Settings;
using Testing;
using NServiceBus.Transport;

[TestFixture]
public class RunningEndpointInstanceTest
{
    static RunningEndpointInstance Create()
    {
        var settings = new SettingsHolder();

        var testInstance = new RunningEndpointInstance(
            settings,
            null!,
            new FeatureComponent(new FeatureComponent.Settings()),
            new TestableMessageSession(),
            null!,
            new CancellationTokenSource(),
            NoOpAsyncDisposable.Instance,
            new EndpointLogSlot("RunningEndpointInstanceTest", endpointIdentifier: null));
        return testInstance;
    }

    [Test]
    public async Task ShouldAllowMultipleStops()
    {
        var testInstance = Create();

        await testInstance.Stop();

        Assert.That(async () => await testInstance.Stop(), Throws.Nothing);
    }

    [Test]
    public async Task ShouldAllowMultipleDispose()
    {
        var testInstance = Create();

        await testInstance.DisposeAsync();

        Assert.That(async () => await testInstance.DisposeAsync(), Throws.Nothing);
    }

    [Test]
    public async Task DisposeAsync_should_pass_a_bounded_cancellation_token_to_transport_Shutdown()
    {
        var capturingTransport = new TokenCapturingTransportInfrastructure();

        var testInstance = new RunningEndpointInstance(
            new SettingsHolder(),
            CreateEmptyReceiveComponent(),
            new FeatureComponent(new FeatureComponent.Settings()),
            new TestableMessageSession(),
            capturingTransport,
            new CancellationTokenSource(),
            NoOpAsyncDisposable.Instance,
            new EndpointLogSlot("RunningEndpointInstanceTest", endpointIdentifier: null));

        await testInstance.DisposeAsync();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(capturingTransport.ShutdownWasCalled, Is.True,
                    "transport.Shutdown was not called during DisposeAsync.");
            Assert.That(capturingTransport.ObservedToken.CanBeCanceled, Is.True,
                "DisposeAsync passed CancellationToken.None to StopCore (which then passes it to " +
                "transport.Shutdown). Disposal must be bounded by an internal cancellation token so " +
                "a stuck transport cannot hang shutdown indefinitely. " +
                "See RunningEndpointInstance.DisposeAsync — `await StopCore()` is missing a token.");
        }
    }

    [Test]
    public async Task DisposeAsync_should_complete_even_when_transport_Shutdown_hangs()
    {
        var hangingTransport = new HangingTransportInfrastructure();

        var testInstance = new RunningEndpointInstance(
            new SettingsHolder(),
            CreateEmptyReceiveComponent(),
            new FeatureComponent(new FeatureComponent.Settings()),
            new TestableMessageSession(),
            hangingTransport,
            new CancellationTokenSource(),
            NoOpAsyncDisposable.Instance,
            new EndpointLogSlot("RunningEndpointInstanceTest", endpointIdentifier: null));

        var dispose = testInstance.DisposeAsync().AsTask();
        var winner = await Task.WhenAny(dispose, Task.Delay(TimeSpan.FromSeconds(5)));

        Assert.That(winner, Is.SameAs(dispose),
            "DisposeAsync did not complete within 5s of a 250ms internal timeout — " +
            "the disposeShutdownTimeout must fire and let disposal proceed past a stuck transport.Shutdown.");
        Assert.DoesNotThrowAsync(() => dispose, "DisposeAsync threw an exception when transport.Shutdown hung. DisposeAsync should complete successfully even if transport.Shutdown does not.");
    }

    [Test]
    public async Task Stop_should_not_throw_when_transport_Shutdown_is_canceled()
    {
        var testInstance = new RunningEndpointInstance(
            new SettingsHolder(),
            CreateEmptyReceiveComponent(),
            new FeatureComponent(new FeatureComponent.Settings()),
            new TestableMessageSession(),
            new HangingTransportInfrastructure(),
            new CancellationTokenSource(),
            NoOpAsyncDisposable.Instance,
            new EndpointLogSlot("RunningEndpointInstanceTest", endpointIdentifier: null));

        using var stopTokenSource = new CancellationTokenSource();
        var stop = testInstance.Stop(stopTokenSource.Token);
        await stopTokenSource.CancelAsync();

        Assert.DoesNotThrowAsync(() => stop);
    }

    [Test]
    public async Task Stop_should_shut_down_the_transport_without_throwing_when_stopping_features_is_canceled()
    {
        var settings = new SettingsHolder();
        var featureSettings = new FeatureComponent.Settings(new FakeFeatureFactory());
        settings.Set(featureSettings);
        var feature = new FeatureWithStopThatWaitsForCancellation();
        featureSettings.Add(feature);
        var featureComponent = new FeatureComponent(featureSettings);
        featureComponent.SetupFeatures(new FakeFeatureConfigurationContext(), settings);
        await featureComponent.StartFeatures(null, null);

        var capturingTransport = new TokenCapturingTransportInfrastructure();
        var testInstance = new RunningEndpointInstance(
            settings,
            CreateEmptyReceiveComponent(),
            featureComponent,
            new TestableMessageSession(),
            capturingTransport,
            new CancellationTokenSource(),
            NoOpAsyncDisposable.Instance,
            new EndpointLogSlot("RunningEndpointInstanceTest", endpointIdentifier: null));

        Assert.DoesNotThrowAsync(async () => await testInstance.Stop(new CancellationToken(true)));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(feature.StopCalled, Is.True, "The feature task should have been stopped.");
            Assert.That(capturingTransport.ShutdownWasCalled, Is.True, "A canceled feature stop must not skip transport Shutdown.");
        }
    }

    [Test]
    public async Task ShouldThrowExceptionAfterInvokingStop()
    {
        var testInstance = Create();

        await testInstance.Stop();

        Assert.Throws<InvalidOperationException>(() => testInstance.Send(new object(), new SendOptions()), "Invoking messaging operations on the endpoint instance after it has been triggered to stop is not supported.");
        Assert.Throws<InvalidOperationException>(() => testInstance.Send<object>(_ => { }, new SendOptions()), "Invoking messaging operations on the endpoint instance after it has been triggered to stop is not supported.");
        Assert.Throws<InvalidOperationException>(() => testInstance.Publish(new object(), new PublishOptions()), "Invoking messaging operations on the endpoint instance after it has been triggered to stop is not supported.");
        Assert.Throws<InvalidOperationException>(() => testInstance.Publish<object>(_ => { }, new PublishOptions()), "Invoking messaging operations on the endpoint instance after it has been triggered to stop is not supported.");
        Assert.Throws<InvalidOperationException>(() => testInstance.Subscribe(typeof(object), new SubscribeOptions()), "Invoking messaging operations on the endpoint instance after it has been triggered to stop is not supported.");
        Assert.Throws<InvalidOperationException>(() => testInstance.Unsubscribe(typeof(object), new UnsubscribeOptions()), "Invoking messaging operations on the endpoint instance after it has been triggered to stop is not supported.");
    }

    // Empty ReceiveComponent (no receivers) so Stop is a no-op and StopCore reaches
    // transport.Shutdown without us having to stand up the full receive pipeline.
    static ReceiveComponent CreateEmptyReceiveComponent() =>
        new(configuration: null!, activityFactory: null!, endpointLogSlot: null!);

    sealed class TokenCapturingTransportInfrastructure : TransportInfrastructure
    {
        public bool ShutdownWasCalled { get; private set; }
        public CancellationToken ObservedToken { get; private set; }

        public override Task Shutdown(CancellationToken cancellationToken = default)
        {
            ShutdownWasCalled = true;
            ObservedToken = cancellationToken;
            return Task.CompletedTask;
        }

        public override string ToTransportAddress(QueueAddress address) => address.BaseAddress;
    }

    sealed class FeatureWithStopThatWaitsForCancellation : TestFeature
    {
        public FeatureWithStopThatWaitsForCancellation() => IsEnabled = true;

        public bool StopCalled { get; private set; }

        protected override void Setup(FeatureConfigurationContext context) => context.RegisterStartupTask(new Runner(this));

        sealed class Runner(FeatureWithStopThatWaitsForCancellation parentFeature) : FeatureStartupTask
        {
            protected override Task OnStart(IMessageSession session, CancellationToken cancellationToken = default) => Task.CompletedTask;

            protected override Task OnStop(IMessageSession session, CancellationToken cancellationToken = default)
            {
                parentFeature.StopCalled = true;
                return Task.Delay(Timeout.Infinite, cancellationToken);
            }
        }
    }

    sealed class HangingTransportInfrastructure : TransportInfrastructure
    {
        public override Task Shutdown(CancellationToken cancellationToken = default) =>
            Task.Delay(Timeout.Infinite, cancellationToken);

        public override string ToTransportAddress(QueueAddress address) => address.BaseAddress;
    }
}