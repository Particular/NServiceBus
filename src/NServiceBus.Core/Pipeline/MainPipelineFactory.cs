namespace NServiceBus.Pipeline
{
    using System.Collections.Generic;
    using System.Linq;
    using NServiceBus.ObjectBuilder;
    using NServiceBus.Pipeline.Contexts;
    using NServiceBus.Settings;
    using NServiceBus.Transports;
    using NServiceBus.Unicast.Transport;

    class MainPipelineFactory : PipelineFactory
    {
        public virtual IEnumerable<TransportReceiver> BuildPipelines(IBuilder builder, ReadOnlySettings settings, IExecutor executor)
        {
            var pipelinesCollection = settings.Get<PipelinesCollection>();

            yield return BuildPipelineInstance(builder, settings, executor, pipelinesCollection.MainPipeline, pipelinesCollection.ReceiveBehavior, "Main", settings.LocalAddress());

            foreach (var satellite in pipelinesCollection.SatellitePipelines)
            {
                yield return BuildPipelineInstance(builder, settings, executor, satellite, pipelinesCollection.ReceiveBehavior, satellite.Name, satellite.ReceiveAddress);
            }
        }

        static TransportReceiver BuildPipelineInstance(IBuilder builder, ReadOnlySettings settings, IExecutor executor, PipelineModifications modifications, RegisterStep receiveBehavior, string name, string address)
        {
            var dequeueSettings = new DequeueSettings(
                address,
                settings.GetOrDefault<string>("MasterNode.Address"),
                settings.GetOrDefault<bool>("Transport.PurgeOnStartup"));

            var pipelineInstance = new PipelineBase<IncomingContext>(builder, settings, modifications, receiveBehavior);
            var receiver = new TransportReceiver(
                name,
                builder,
                builder.Build<IDequeueMessages>(),
                dequeueSettings,
                pipelineInstance,
                executor);
            return receiver;
        }
    }
}