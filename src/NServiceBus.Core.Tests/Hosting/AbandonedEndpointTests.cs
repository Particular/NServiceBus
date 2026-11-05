#nullable enable

namespace NServiceBus.Core.Tests.Host;

using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using NServiceBus.Features;
using NUnit.Framework;

// Reproduction for https://github.com/Particular/NServiceBus/issues/7066.
#pragma warning disable CS0618 // Exercise the self-hosted API used in the original report.
[TestFixture]
[NonParallelizable]
public class AbandonedEndpointTests
{
    [TestCase(false, false, TestName = "Abandoned_endpoint_should_stop_and_release_resources_after_gc")]
    [TestCase(true, false, TestName = "Explicitly_stopped_endpoint_should_release_resources_control")]
    [TestCase(false, true, TestName = "Referenced_endpoint_should_continue_processing_control")]
    public async Task Should_release_abandoned_endpoint(bool stopBeforeDroppingReference, bool keepReference)
    {
        var directory = Path.Combine(Path.GetTempPath(), "nsb-7066-" + Guid.NewGuid().ToString("N"));
        var receiverName = "Receiver-" + Guid.NewGuid().ToString("N");
        var probe = new Probe();
        var senderConfiguration = Configure("Sender-" + Guid.NewGuid().ToString("N"), directory);
        senderConfiguration.SendOnly();
        var sender = await Endpoint.Start(senderConfiguration);
        WeakReference<IEndpointInstance>? receiver = null;
        IEndpointInstance? retainedEndpoint = null;
        try
        {
            receiver = StartAndDropReference(receiverName, directory, probe);
            if (keepReference)
            {
                Assert.That(receiver.TryGetTarget(out retainedEndpoint), Is.True);
            }
            Assert.That(probe.Started, Is.True, "The real endpoint must successfully start before testing collection.");

            await SendProbe(sender, receiverName);
            await probe.Messages.Reader.ReadAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(10));

            if (stopBeforeDroppingReference)
            {
                StopThroughWeakReference(receiver);
            }

            // No target is retrieved during collection; even a temporary strong reference can skew this test.
            for (var attempt = 0; attempt < 20; attempt++)
            {
                GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, blocking: true, compacting: true);
                GC.WaitForPendingFinalizers();
                GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, blocking: true, compacting: true);
                await Task.Delay(100);
            }

            await SendProbe(sender, receiverName);
            using var observationTimeout = new CancellationTokenSource(TimeSpan.FromSeconds(2));
            var cancellationToken = observationTimeout.Token;
            var stillProcessing = false;
            try
            {
                await probe.Messages.Reader.ReadAsync(cancellationToken);
                stillProcessing = true;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
            }

            var retained = IsAlive(receiver);
            TestContext.Out.WriteLine($"After 20 GC cycles: endpoint retained={retained}, feature stopped={probe.Stopped}, resource disposed={probe.Disposed}, processed message after GC={stillProcessing}");
            using (Assert.EnterMultipleScope())
            {
                Assert.That(retained, Is.EqualTo(keepReference), "Only an endpoint with a live application reference should remain alive after GC.");
                Assert.That(probe.Stopped, Is.EqualTo(!keepReference), "Abandoning the endpoint should stop its feature startup task (#7066).");
                Assert.That(probe.Disposed, Is.EqualTo(!keepReference), "Abandoning the endpoint should dispose its feature resource (#7066).");
                Assert.That(stillProcessing, Is.EqualTo(keepReference), "The abandoned endpoint still processes messages after forced GC (#7066).");
            }
        }
        finally
        {
            GC.KeepAlive(retainedEndpoint);
            if (receiver is not null)
            {
                StopThroughWeakReference(receiver);
            }
            await sender.Stop();
            Directory.Delete(directory, recursive: true);
        }
    }

    static EndpointConfiguration Configure(string name, string directory)
    {
        var configuration = new EndpointConfiguration(name);
        configuration.UseTransport(new LearningTransport { StorageDirectory = directory });
        configuration.UseSerialization<SystemJsonSerializer>();
        configuration.AssemblyScanner().Disable = true;
        configuration.EnableInstallers();
        return configuration;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static WeakReference<IEndpointInstance> StartAndDropReference(string name, string directory, Probe probe)
    {
        var configuration = Configure(name, directory);
        configuration.RegisterComponents(services => services.AddSingleton(probe));
        configuration.AddHandler<ProbeHandler>();
        configuration.EnableFeature<LifetimeFeature>();
        var endpoint = Endpoint.Start(configuration).GetAwaiter().GetResult();
        return new WeakReference<IEndpointInstance>(endpoint);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool IsAlive(WeakReference<IEndpointInstance> reference) => reference.TryGetTarget(out _);

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void StopThroughWeakReference(WeakReference<IEndpointInstance> reference)
    {
        if (reference.TryGetTarget(out var endpoint))
        {
            endpoint.Stop().GetAwaiter().GetResult();
        }
    }

    static Task SendProbe(IMessageSession sender, string destination, CancellationToken cancellationToken = default)
    {
        var options = new SendOptions();
        options.SetDestination(destination);
        return sender.Send(new ProbeMessage(), options, cancellationToken);
    }

    public class ProbeMessage : IMessage;

    public class Probe
    {
        public volatile bool Started;
        public volatile bool Stopped;
        public volatile bool Disposed;
        public Channel<bool> Messages { get; } = Channel.CreateUnbounded<bool>();
    }

    public class ProbeHandler(Probe probe) : IHandleMessages<ProbeMessage>
    {
        public Task Handle(ProbeMessage message, IMessageHandlerContext context)
        {
            probe.Messages.Writer.TryWrite(true);
            return Task.CompletedTask;
        }
    }

    public class LifetimeFeature : Feature
    {
        protected override void Setup(FeatureConfigurationContext context) =>
            context.RegisterStartupTask(provider => new LifetimeTask(provider.GetRequiredService<Probe>()));
    }

    sealed class LifetimeTask(Probe probe) : FeatureStartupTask, IDisposable
    {
        protected override Task OnStart(IMessageSession session, CancellationToken cancellationToken = default)
        {
            probe.Started = true;
            return Task.CompletedTask;
        }

        protected override Task OnStop(IMessageSession session, CancellationToken cancellationToken = default)
        {
            probe.Stopped = true;
            return Task.CompletedTask;
        }

        public void Dispose() => probe.Disposed = true;
    }
}
#pragma warning restore CS0618
