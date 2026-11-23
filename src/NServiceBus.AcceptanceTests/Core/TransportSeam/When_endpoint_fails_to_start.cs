#pragma warning disable CS0618 // Type or member is obsolete
namespace NServiceBus.AcceptanceTests.Core.TransportSeam;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AcceptanceTesting;
using FakeTransport;
using Configuration.AdvancedExtensibility;
using Features;
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
            Assert.That(sequence, Has.Exactly(1).EqualTo(DisposeThrowingTask), "A startup task that failed to start should be disposed once. Sequence: " + Describe(sequence));
            Assert.That(sequence, Has.Exactly(1).EqualTo(StopWorkingTask), "Started startup tasks should be stopped once. Sequence: " + Describe(sequence));
            Assert.That(sequence.ToList().IndexOf(StopMain), Is.LessThan(sequence.ToList().IndexOf(StopWorkingTask)), "Receivers should be stopped before features, as in a normal stop. Sequence: " + Describe(sequence));
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
    [CancelAfter(30_000)]
    public void Should_rethrow_the_start_exception_when_disposing_the_self_hosted_container_fails(CancellationToken cancellationToken = default)
    {
        var fakeTransport = new FakeTransport();
        var endpointConfiguration = Create(fakeTransport, "FailingStartFailingDispose");
        endpointConfiguration.EnableFeature<ServiceThatThrowsOnDisposeFeature>();

        var exception = Assert.ThrowsAsync<SimulatedException>(async () => await Endpoint.Start(endpointConfiguration, cancellationToken));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(exception.Message, Is.EqualTo("Startup task failed"), "A container disposal failure should not replace the start exception");
            Assert.That(fakeTransport.StartupSequence, Does.Contain(ServiceThatThrowsOnDispose.Disposed), "The container should have been disposed. Sequence: " + Describe(fakeTransport.StartupSequence));
        }
    }

    class ServiceThatThrowsOnDisposeFeature : Feature
    {
        protected override void Setup(FeatureConfigurationContext context)
        {
            var sequence = context.Settings.Get<FakeTransport.StartUpSequence>();
            context.Services.AddSingleton(_ => new ServiceThatThrowsOnDispose(sequence));
            context.RegisterStartupTask(provider => new ResolvingThrowingStartupTask(provider.GetRequiredService<ServiceThatThrowsOnDispose>()));
        }
    }

    sealed class ServiceThatThrowsOnDispose(FakeTransport.StartUpSequence sequence) : IDisposable
    {
        public const string Disposed = "ServiceThatThrowsOnDispose.Dispose";

        public void Dispose()
        {
            sequence.Add(Disposed);
            throw new InvalidOperationException("Disposing the service failed");
        }
    }

    // Resolves the service so that the container owns and disposes it.
    class ResolvingThrowingStartupTask(ServiceThatThrowsOnDispose service) : FeatureStartupTask
    {
        public ServiceThatThrowsOnDispose Service { get; } = service;

        protected override Task OnStart(IMessageSession session, CancellationToken cancellationToken = default) =>
            throw new SimulatedException("Startup task failed");

        protected override Task OnStop(IMessageSession session, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    [Test]
    [CancelAfter(30_000)]
    public async Task Should_not_let_a_critical_error_raised_while_starting_stop_the_endpoint_before_it_has_started(CancellationToken cancellationToken = default)
    {
        var fakeTransport = new FakeTransport();
        var endpointConfiguration = Create(fakeTransport, "CriticalErrorWhileStarting");
        endpointConfiguration.EnableFeature<CriticalErrorRaisingFeature>();
        var actionInvoked = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        endpointConfiguration.GetSettings().Set(actionInvoked);
        var stopped = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        endpointConfiguration.DefineCriticalErrorAction(async (context, token) =>
        {
            actionInvoked.TrySetResult();
            await context.Stop(token);
            stopped.TrySetResult();
        });

        _ = await Endpoint.Start(endpointConfiguration, cancellationToken);
        await stopped.Task.WaitAsync(cancellationToken);

        var sequence = fakeTransport.StartupSequence.ToList();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(sequence, Does.Not.Contain(ActionInvokedWhileStarting), "The critical error action should not run while the endpoint starts. Sequence: " + Describe(fakeTransport.StartupSequence));
            Assert.That(sequence, Does.Contain(StartSatellite).And.Contain(TransportShutdown), "Sequence: " + Describe(fakeTransport.StartupSequence));
            Assert.That(sequence, Has.Exactly(1).EqualTo(StopRaisingTask), "The task that raised the critical error should be stopped. Sequence: " + Describe(fakeTransport.StartupSequence));
            Assert.That(sequence.IndexOf(StartMain), Is.LessThan(sequence.IndexOf(StopMain)), "The endpoint should finish starting before the critical error action stops it. Sequence: " + Describe(fakeTransport.StartupSequence));
            Assert.That(sequence.IndexOf(StartSatellite), Is.LessThan(sequence.IndexOf(TransportShutdown)), "No receiver should start after the transport is shut down. Sequence: " + Describe(fakeTransport.StartupSequence));
        }
    }

    const string StopRaisingTask = "FeatureStartupTask.Stop Raising";
    const string ActionInvokedWhileStarting = "CriticalErrorAction invoked while starting";

    class CriticalErrorRaisingFeature : Feature
    {
        protected override void Setup(FeatureConfigurationContext context)
        {
            var sequence = context.Settings.Get<FakeTransport.StartUpSequence>();
            var actionInvoked = context.Settings.Get<TaskCompletionSource>();
            context.RegisterStartupTask(provider => new CriticalErrorRaisingStartupTask(provider.GetRequiredService<NServiceBus.CriticalError>(), sequence, actionInvoked));
        }
    }

    class CriticalErrorRaisingStartupTask(NServiceBus.CriticalError criticalError, FakeTransport.StartUpSequence sequence, TaskCompletionSource actionInvoked) : FeatureStartupTask
    {
        protected override async Task OnStart(IMessageSession session, CancellationToken cancellationToken = default)
        {
            criticalError.Raise("Simulated critical error while starting", new SimulatedException(), cancellationToken);

            // Gives an action that is dispatched right away time to run, so the test does not depend on scheduling.
            if (await Task.WhenAny(actionInvoked.Task, Task.Delay(TimeSpan.FromMilliseconds(500), cancellationToken)) == actionInvoked.Task)
            {
                sequence.Add(ActionInvokedWhileStarting);
            }
        }

        protected override Task OnStop(IMessageSession session, CancellationToken cancellationToken = default)
        {
            sequence.Add(StopRaisingTask);
            return Task.CompletedTask;
        }
    }

    [Test]
    public async Task Should_shut_down_the_transport_when_start_is_called_with_a_canceled_token()
    {
        var fakeTransport = new FakeTransport();
        var startableEndpoint = await Endpoint.Create(Create(fakeTransport, "PreCanceledStart"));

        Assert.CatchAsync<OperationCanceledException>(async () => await startableEndpoint.Start(new CancellationToken(true)));

        var sequence = fakeTransport.StartupSequence;
        using (Assert.EnterMultipleScope())
        {
            Assert.That(sequence, Does.Not.Contain(StartWorkingTask), "Nothing should start. Sequence: " + Describe(sequence));
            Assert.That(sequence, Does.Not.Contain(StartMain), "Nothing should start. Sequence: " + Describe(sequence));
            Assert.That(sequence, Does.Contain(TransportShutdown), "The transport should be shut down. Sequence: " + Describe(sequence));
        }

        Assert.ThrowsAsync<InvalidOperationException>(async () => await startableEndpoint.Start());
    }

    [Test]
    [CancelAfter(30_000)]
    public async Task Should_let_a_canceled_caller_stop_waiting_for_a_concurrent_start(CancellationToken cancellationToken = default)
    {
        var fakeTransport = new FakeTransport();
        var endpointConfiguration = Create(fakeTransport, "CanceledWhileWaitingForConcurrentStart");
        var gate = new BlockingStartupTaskGate();
        endpointConfiguration.GetSettings().Set(gate);
        endpointConfiguration.EnableFeature<BlockingStartupTaskFeature>();
        var startableEndpoint = await Endpoint.Create(endpointConfiguration, cancellationToken);

        var firstStart = startableEndpoint.Start(cancellationToken);
        try
        {
            await gate.Started.Task.WaitAsync(cancellationToken);

            using var waitingTokenSource = new CancellationTokenSource();
            var waitingStart = startableEndpoint.Start(waitingTokenSource.Token);
            await waitingTokenSource.CancelAsync();

            Assert.CatchAsync<OperationCanceledException>(async () => await waitingStart.WaitAsync(TimeSpan.FromSeconds(5), cancellationToken), "A caller waiting for a concurrent start should stop waiting when its token is canceled");
        }
        finally
        {
            gate.Release.TrySetResult();
        }

        var instance = await firstStart.WaitAsync(cancellationToken);

        Assert.That(fakeTransport.StartupSequence, Does.Not.Contain(TransportShutdown), "The canceled caller should not roll back the concurrent start");

        await instance.Stop(cancellationToken);
    }

    class BlockingStartupTaskGate
    {
        public TaskCompletionSource Started { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource Release { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    }

    class BlockingStartupTaskFeature : Feature
    {
        protected override void Setup(FeatureConfigurationContext context) =>
            context.RegisterStartupTask(new BlockingStartupTask(context.Settings.Get<BlockingStartupTaskGate>()));
    }

    class BlockingStartupTask(BlockingStartupTaskGate gate) : FeatureStartupTask
    {
        protected override async Task OnStart(IMessageSession session, CancellationToken cancellationToken = default)
        {
            gate.Started.TrySetResult();
            await gate.Release.Task.WaitAsync(cancellationToken);
        }

        protected override Task OnStop(IMessageSession session, CancellationToken cancellationToken = default) => Task.CompletedTask;
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