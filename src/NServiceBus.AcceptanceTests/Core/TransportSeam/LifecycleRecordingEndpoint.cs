namespace NServiceBus.AcceptanceTests.Core.TransportSeam;

using System.Threading;
using System.Threading.Tasks;
using AcceptanceTesting;
using Configuration.AdvancedExtensibility;
using Features;
using FakeTransport;
using Transport;

static class LifecycleRecordingEndpoint
{
    public const string TransportInitialize = "TransportDefinition.Initialize";
    public const string TransportShutdown = "TransportInfrastructure.Shutdown";
    public const string StartMain = "IMessageReceiver.StartReceive for receiver Main";
    public const string StopMain = "IMessageReceiver.StopReceive for receiver Main";
    public const string StartSatellite = "IMessageReceiver.StartReceive for receiver " + SatelliteFeature.SatelliteName;
    public const string StartWorkingTask = "FeatureStartupTask.Start Working";
    public const string StopWorkingTask = "FeatureStartupTask.Stop Working";
    public const string StartThrowingTask = "FeatureStartupTask.Start Throwing";

    public static EndpointConfiguration Create(FakeTransport fakeTransport, string endpointName, bool throwInStartupTask = false)
    {
        var endpointConfiguration = new EndpointConfiguration(endpointName);
        endpointConfiguration.AssemblyScanner().Disable = true;
        endpointConfiguration.UsePersistence<AcceptanceTestingPersistence>();
        endpointConfiguration.UseTransport(fakeTransport);
        endpointConfiguration.UseSerialization<SystemJsonSerializer>();
        endpointConfiguration.GetSettings().Set(fakeTransport.StartupSequence);
        endpointConfiguration.GetSettings().Set(ThrowInStartupTaskKey, throwInStartupTask);
        endpointConfiguration.EnableFeature<SatelliteFeature>();
        endpointConfiguration.EnableFeature<StartupTaskFeature>();
        return endpointConfiguration;
    }

    const string ThrowInStartupTaskKey = "LifecycleRecordingEndpoint.ThrowInStartupTask";

    class SatelliteFeature : Feature
    {
        public const string SatelliteName = "Satellite";

        protected override void Setup(FeatureConfigurationContext context) =>
            context.AddSatelliteReceiver(SatelliteName, new QueueAddress("SatelliteQueue"), PushRuntimeSettings.Default,
                (config, _) => RecoverabilityAction.MoveToError(config.Failed.ErrorQueue),
                (_, _, _) => Task.CompletedTask);
    }

    class StartupTaskFeature : Feature
    {
        protected override void Setup(FeatureConfigurationContext context)
        {
            var sequence = context.Settings.Get<FakeTransport.StartUpSequence>();

            context.RegisterStartupTask(new RecordingStartupTask("Working", sequence, throwOnStart: false));

            if (context.Settings.Get<bool>(ThrowInStartupTaskKey))
            {
                context.RegisterStartupTask(new RecordingStartupTask("Throwing", sequence, throwOnStart: true));
            }
        }
    }

    class RecordingStartupTask(string name, FakeTransport.StartUpSequence sequence, bool throwOnStart) : FeatureStartupTask
    {
        protected override Task OnStart(IMessageSession session, CancellationToken cancellationToken = default)
        {
            sequence.Add($"FeatureStartupTask.Start {name}");

            if (throwOnStart)
            {
                throw new SimulatedException("Startup task failed");
            }

            return Task.CompletedTask;
        }

        protected override Task OnStop(IMessageSession session, CancellationToken cancellationToken = default)
        {
            sequence.Add($"FeatureStartupTask.Stop {name}");
            return Task.CompletedTask;
        }
    }
}