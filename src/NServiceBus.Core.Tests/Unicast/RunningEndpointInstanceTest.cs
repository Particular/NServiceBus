namespace NServiceBus.Unicast.Tests;

using System;
using System.Threading;
using System.Threading.Tasks;
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

    sealed class HangingTransportInfrastructure : TransportInfrastructure
    {
        public override Task Shutdown(CancellationToken cancellationToken = default) =>
            Task.Delay(Timeout.Infinite, cancellationToken);

        public override string ToTransportAddress(QueueAddress address) => address.BaseAddress;
    }
}