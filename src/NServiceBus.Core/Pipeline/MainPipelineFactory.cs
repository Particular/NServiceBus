namespace NServiceBus.Pipeline
{
    using System.Collections.Generic;
    using NServiceBus.ObjectBuilder;
    using NServiceBus.Settings;
    using NServiceBus.Transports;
    using NServiceBus.Unicast.Transport;

    class MainPipelineFactory : PipelineFactory
    {
        public virtual IEnumerable<TransportReceiver> BuildPipelines(IBuilder builder, ReadOnlySettings settings, IExecutor executor)
        {
            var dequeueSettings = new DequeueSettings(settings.LocalAddress().Queue, settings.GetOrDefault<bool>("Transport.PurgeOnStartup"));

            var incomingPipeline = new IncomingPipeline(builder, settings.Get<PipelineModifications>());


            var receiver = new TransportReceiver(
                "Main",
                builder,
                builder.Build<IDequeueMessages>(),
                dequeueSettings,
               incomingPipeline,
                executor);
            yield return receiver;
        }
    }
}