#pragma warning disable CS0618 // Type or member is obsolete
namespace NServiceBus.AcceptanceTests.Core.TransportSeam;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AcceptanceTesting;
using FakeTransport;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
using static LifecycleRecordingEndpoint;

public class When_endpoint_fails_to_start : NServiceBusAcceptanceTest
{
    public enum HostingApi
    {
        GenericHost,
        ExternallyManagedContainer,
        SelfHosted
    }

    [TestCase(HostingApi.GenericHost)]
    [TestCase(HostingApi.ExternallyManagedContainer)]
    [TestCase(HostingApi.SelfHosted)]
    [CancelAfter(30_000)]
    public async Task Should_shut_down_the_transport_when_a_feature_startup_task_throws(HostingApi hostingApi, CancellationToken cancellationToken = default)
    {
        var fakeTransport = new FakeTransport();
        var endpointConfiguration = Create(fakeTransport, $"FailingStartupTask{hostingApi}", throwInStartupTask: true);

        var exception = await StartExpectingFailure(hostingApi, endpointConfiguration, cancellationToken);

        var sequence = fakeTransport.StartupSequence;
        using (Assert.EnterMultipleScope())
        {
            Assert.That(exception.ToString(), Does.Contain("Startup task failed"), "The start exception should reach the caller");
            Assert.That(sequence, Does.Contain(StartThrowingTask));
            Assert.That(sequence, Does.Not.Contain(StartMain), "Receivers should not start after a feature fails");
            Assert.That(sequence, Does.Not.Contain(StopThrowingTask), "A startup task that failed to start should not be stopped");
            Assert.That(sequence, Has.Exactly(1).EqualTo(StopWorkingTask), "Started startup tasks should be stopped once. Sequence: " + Describe(sequence));
            Assert.That(sequence, Does.Contain(TransportShutdown), "The transport should be shut down. Sequence: " + Describe(sequence));
        }
    }

    [TestCase(HostingApi.GenericHost)]
    [TestCase(HostingApi.ExternallyManagedContainer)]
    [TestCase(HostingApi.SelfHosted)]
    [CancelAfter(30_000)]
    public async Task Should_stop_started_receivers_features_and_transport_when_a_satellite_fails_to_start(HostingApi hostingApi, CancellationToken cancellationToken = default)
    {
        var fakeTransport = new FakeTransport();
        fakeTransport.ThrowOnReceiverStart("Satellite", new SimulatedException("Satellite failed to start"));
        var endpointConfiguration = Create(fakeTransport, $"FailingSatellite{hostingApi}");

        var exception = await StartExpectingFailure(hostingApi, endpointConfiguration, cancellationToken);

        var sequence = fakeTransport.StartupSequence;
        using (Assert.EnterMultipleScope())
        {
            Assert.That(exception.ToString(), Does.Contain("Satellite failed to start"), "The start exception should reach the caller");
            Assert.That(sequence, Does.Contain(StartMain));
            Assert.That(sequence, Does.Contain(StartSatellite));
            Assert.That(sequence, Does.Contain(StopMain), "The started main receiver should be stopped. Sequence: " + Describe(sequence));
            Assert.That(sequence, Has.Exactly(1).EqualTo(StopWorkingTask), "Started startup tasks should be stopped once. Sequence: " + Describe(sequence));
            Assert.That(sequence, Does.Contain(TransportShutdown), "The transport should be shut down. Sequence: " + Describe(sequence));
        }
    }

    [Test]
    [CancelAfter(30_000)]
    public void Should_not_log_an_error_when_cleanup_aborts_because_start_was_canceled(CancellationToken cancellationToken = default)
    {
        using var startTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var fakeTransport = new FakeTransport();
        fakeTransport.RaiseExceptionOnReceiverStop(new OperationCanceledException());

        var builder = Host.CreateApplicationBuilder();
        builder.Logging.ClearProviders();
        builder.Logging.AddFakeLogging();
        builder.Services.AddNServiceBusEndpoint(Create(fakeTransport, "CanceledStart", cancelInStartupTask: startTokenSource));
        using var host = builder.Build();

        Assert.CatchAsync<OperationCanceledException>(async () => await host.StartAsync(startTokenSource.Token), "The start cancellation should reach the caller");

        var sequence = fakeTransport.StartupSequence;
        using (Assert.EnterMultipleScope())
        {
            Assert.That(sequence, Does.Contain(StopMain), "Cleanup should run. Sequence: " + Describe(sequence));
            Assert.That(ErrorsFrom(host, "NServiceBus.StartableEndpoint"), Is.Empty, "Cleanup that aborts because the caller canceled should not be logged as an error");
        }
    }

    [Test]
    [CancelAfter(30_000)]
    public void Should_shut_down_the_transport_when_a_receiver_stop_is_canceled_after_start_was_canceled(CancellationToken cancellationToken = default)
    {
        using var startTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var fakeTransport = new FakeTransport();
        fakeTransport.RaiseExceptionOnReceiverStop(new OperationCanceledException());

        var builder = Host.CreateApplicationBuilder();
        builder.Services.AddNServiceBusEndpoint(Create(fakeTransport, "CanceledStartCanceledStop", cancelInStartupTask: startTokenSource));
        using var host = builder.Build();

        Assert.CatchAsync<OperationCanceledException>(async () => await host.StartAsync(startTokenSource.Token));

        Assert.That(fakeTransport.StartupSequence, Does.Contain(TransportShutdown), "Sequence: " + Describe(fakeTransport.StartupSequence));
    }

    [Test]
    [CancelAfter(30_000)]
    public void Should_shut_down_the_transport_when_a_receiver_stop_is_canceled_while_disposing_an_endpoint_that_never_started(CancellationToken cancellationToken = default)
    {
        var fakeTransport = new FakeTransport();
        fakeTransport.RaiseExceptionOnReceiverStop(new OperationCanceledException());

        var builder = Host.CreateApplicationBuilder();
        // Registered first so that its StartAsync fails before the endpoint starts, and the host only disposes the endpoint.
        builder.Services.AddHostedService<FailingHostedService>();
        builder.Services.AddNServiceBusEndpoint(Create(fakeTransport, "NeverStartedCanceledStop"));
        using var host = builder.Build();

        Assert.CatchAsync<SimulatedException>(async () => await host.RunAsync(cancellationToken));

        var sequence = fakeTransport.StartupSequence;
        using (Assert.EnterMultipleScope())
        {
            Assert.That(sequence, Does.Not.Contain(StartMain));
            Assert.That(sequence, Does.Contain(TransportShutdown), "Sequence: " + Describe(sequence));
        }
    }

    class FailingHostedService : IHostedService
    {
        public Task StartAsync(CancellationToken cancellationToken = default) => throw new SimulatedException("Another hosted service failed to start");

        public Task StopAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    [Test]
    public async Task Should_not_start_again()
    {
        var fakeTransport = new FakeTransport();
        fakeTransport.ThrowOnReceiverStart("Satellite", new SimulatedException("Satellite failed to start"));
        var startableEndpoint = await Endpoint.Create(Create(fakeTransport, "FailingSatelliteStartedAgain"));

        Assert.ThrowsAsync<SimulatedException>(async () => await startableEndpoint.Start());
        Assert.ThrowsAsync<InvalidOperationException>(async () => await startableEndpoint.Start());

        Assert.That(fakeTransport.StartupSequence, Has.Exactly(1).EqualTo(StartMain), "Should not start the receivers again");
    }

    static async Task<Exception> StartExpectingFailure(HostingApi hostingApi, EndpointConfiguration endpointConfiguration, CancellationToken cancellationToken)
    {
        switch (hostingApi)
        {
            case HostingApi.GenericHost:
                {
                    var builder = Host.CreateApplicationBuilder();
                    builder.Services.AddNServiceBusEndpoint(endpointConfiguration);
                    using var host = builder.Build();
                    // RunAsync disposes the host when StartAsync throws.
                    return Assert.CatchAsync(async () => await host.RunAsync(cancellationToken));
                }
            case HostingApi.ExternallyManagedContainer:
                {
                    var services = new ServiceCollection();
                    var startableEndpoint = EndpointWithExternallyManagedContainer.Create(endpointConfiguration, services);
                    await using var provider = services.BuildServiceProvider();
                    return Assert.CatchAsync(async () => await startableEndpoint.Start(provider, cancellationToken));
                }
            case HostingApi.SelfHosted:
                return Assert.CatchAsync(async () => await Endpoint.Start(endpointConfiguration, cancellationToken));
            default:
                throw new ArgumentOutOfRangeException(nameof(hostingApi));
        }
    }

    static string Describe(FakeTransport.StartUpSequence sequence) => string.Join(" -> ", sequence);

    static IEnumerable<string> ErrorsFrom(IHost host, string category) =>
        host.Services.GetFakeLogCollector().GetSnapshot()
            .Where(record => record.Category == category && record.Level >= LogLevel.Error)
            .Select(record => record.Message);
}