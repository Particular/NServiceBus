namespace NServiceBus.Pipeline
{
    using System.Collections.Generic;
    using System.Linq;
    using NServiceBus.Logging;
    using NServiceBus.ObjectBuilder;
    using NServiceBus.Satellites;
    using NServiceBus.Settings;
    using NServiceBus.Transports;
    using NServiceBus.Unicast.Transport;

    class SatellitePipelineFactory : PipelineFactory
    {
        public virtual IEnumerable<TransportReceiver> BuildPipelines(IBuilder builder, ReadOnlySettings settings, IExecutor executor)
        {
            var satellitesList = builder.BuildAll<ISatellite>()
                .ToList()
                .Where(s => !s.Disabled)
                .ToList();

            for (var index = 0; index < satellitesList.Count; index++)
            {
                var satellite = satellitesList[index];
                Logger.DebugFormat("Creating {1}/{2} {0} satellite", satellite.GetType().AssemblyQualifiedName,
                    index + 1, satellitesList.Count);

                if (satellite.InputAddress != null)
                {
                    var pipelineExecutor = BuildPipelineExecutor(builder);

                    var dequeueSettings = new DequeueSettings(satellite.InputAddress.Queue, 
                        settings.GetOrDefault<bool>("Transport.PurgeOnStartup"));


                    var pipeline = new SatelliteTransportReceiver(
                        satellite.GetType().AssemblyQualifiedName,
                        builder,
                        builder.Build<IDequeueMessages>(),
                        dequeueSettings,
                        pipelineExecutor,
                        executor,
                        satellite);

                    var advancedSatellite = satellite as IAdvancedSatellite;

                    if (advancedSatellite != null)
                    {
                        var receiverCustomization = advancedSatellite.GetReceiverCustomization();
                        receiverCustomization(pipeline);
                    }
                    yield return pipeline;
                }
                else
                {
                    Logger.DebugFormat("Skipping satellite {0} because its input queue is not configured.", satellite.GetType().AssemblyQualifiedName);
                }
            }
        }

        static PipelineExecutor BuildPipelineExecutor(IBuilder builder)
        {
            var pipelineModifications = new PipelineModifications();
            var pipelineSettings = new PipelineSettings(pipelineModifications);

            pipelineSettings.Register(builder.Build<TransportReceiveBehaviorDefinition>().Registration);
            pipelineSettings.RegisterConnector<TransportReceiveToPhysicalMessageProcessingConnector>("Allows to abort processing the message");
            
            pipelineSettings.Register<MoveFaultsToErrorQueueBehavior.Registration>();
            pipelineSettings.Register<FirstLevelRetriesBehavior.Registration>();
            pipelineSettings.Register<ExecuteSatelliteHandlerBehavior.Registration>();

            var pipelineExecutor = new PipelineExecutor(builder, builder.Build<BusNotifications>(), pipelineModifications, builder.Build<BehaviorContextStacker>());
            return pipelineExecutor;
        }

        static readonly ILog Logger = LogManager.GetLogger<SatellitePipelineFactory>();
    }
}