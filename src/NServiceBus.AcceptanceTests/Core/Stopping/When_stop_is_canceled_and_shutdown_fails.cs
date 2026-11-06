namespace NServiceBus.AcceptanceTests.Core.Stopping;

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FakeTransport;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using NUnit.Framework;
using TransportSeam;

public class When_stop_is_canceled_and_shutdown_fails : NServiceBusAcceptanceTest
{
    [Test]
    [CancelAfter(30_000)]
    public async Task Should_report_both_the_cancellation_and_the_shutdown_failure(CancellationToken cancellationToken = default)
    {
        var fakeTransport = new FakeTransport();
        fakeTransport.RaiseExceptionOnReceiverStop(new OperationCanceledException());
        fakeTransport.RaiseExceptionOnTransportDispose(new InvalidOperationException("Shutdown failed"));

        var builder = Host.CreateApplicationBuilder();
        builder.Logging.ClearProviders();
        builder.Logging.AddFakeLogging();
        builder.Services.AddNServiceBusEndpoint(LifecycleRecordingEndpoint.Create(fakeTransport, "CanceledStopFailingShutdown"));
        using var host = builder.Build();
        await host.StartAsync(cancellationToken);

        using var stopTokenSource = new CancellationTokenSource();
        await stopTokenSource.CancelAsync();

        Assert.CatchAsync<OperationCanceledException>(async () => await host.StopAsync(stopTokenSource.Token), "A canceled stop should reach the caller");

        var shutdownErrors = host.Services.GetFakeLogCollector().GetSnapshot()
            .Where(record => record.Level == LogLevel.Error && record.Message.Contains("Shutdown of the transport infrastructure failed."))
            .ToList();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(fakeTransport.StartupSequence, Does.Contain(LifecycleRecordingEndpoint.TransportShutdown));
            Assert.That(shutdownErrors, Has.Count.EqualTo(1), "The shutdown failure should be logged");
            Assert.That(shutdownErrors.Single().Exception, Is.InstanceOf<InvalidOperationException>());
        }
    }
}