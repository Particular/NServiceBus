namespace NServiceBus.Unicast.Tests;

using System;
using System.Reflection;
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
            null,
            new FeatureComponent(new FeatureComponent.Settings()),
            new TestableMessageSession(),
            null,
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
        // DisposeAsync is the cleanup path taken when an IHost is disposed without an
        // explicit StopAsync (e.g. `using var host = ...` or `await using var host = ...`
        // with no prior host.StopAsync()). It calls StopCore internally, which awaits
        // transport.Shutdown. If StopCore is invoked with CancellationToken.None then
        // the transport's shutdown wait is unbounded — a slow or stuck transport hangs
        // disposal forever instead of being aborted by the host's shutdown timeout.
        //
        // Repro from the field: NServiceBus.SqlServerTransport receive loop in flight
        // at the moment the test host is disposed; 5+ minute hang until the hangdump
        // timeout fires.

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

        Assert.That(capturingTransport.ShutdownWasCalled, Is.True,
            "transport.Shutdown was not called during DisposeAsync.");
        Assert.That(capturingTransport.ObservedToken.CanBeCanceled, Is.True,
            "DisposeAsync passed CancellationToken.None to StopCore (which then passes it to " +
            "transport.Shutdown). Disposal must be bounded by an internal cancellation token so " +
            "a stuck transport cannot hang shutdown indefinitely. " +
            "See RunningEndpointInstance.DisposeAsync — `await StopCore()` is missing a token.");
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

    // ReceiveComponent has a private constructor; reflection is the lightest way to
    // get a real instance for tests. An empty `receivers` list (the field default)
    // makes Stop a no-op, so we can reach transport.Shutdown without standing up
    // the full receive pipeline.
    static ReceiveComponent CreateEmptyReceiveComponent() =>
        (ReceiveComponent)Activator.CreateInstance(
            typeof(ReceiveComponent),
            BindingFlags.Instance | BindingFlags.NonPublic,
            binder: null,
            args: [null, null, null],
            culture: null)!;

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
}