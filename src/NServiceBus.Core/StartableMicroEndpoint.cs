namespace NServiceBus
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Security.Principal;
    using System.Threading.Tasks;
    using NServiceBus.ObjectBuilder;
    using NServiceBus.Pipeline;
    using NServiceBus.Pipeline.Contexts;
    using NServiceBus.Settings;
    using NServiceBus.Transport;
    using NServiceBus.Transports;
    using NServiceBus.Unicast;

    /// <summary>
    /// Represents a node that can be started.
    /// </summary>
    public class StartableMicroEndpoint
    {
        SettingsHolder settings;
        IBuilder builder;
        PipelineConfiguration pipelineConfiguration;
        PushRuntimeSettings pushRuntimeSettings;

        internal StartableMicroEndpoint(SettingsHolder settings, IBuilder builder, PipelineConfiguration pipelineConfiguration, PushRuntimeSettings pushRuntimeSettings)
        {
            this.settings = settings;
            this.builder = builder;
            this.pipelineConfiguration = pipelineConfiguration;
            this.pushRuntimeSettings = pushRuntimeSettings;
        }

        /// <summary>
        /// Starts the node.
        /// </summary>
        /// <returns></returns>
        public async Task<IEndpointInstance> Start()
        {
            var pipelineCollection = CreateIncomingPipelines();

            var runningInstance = new RunningNode(builder, pipelineCollection);

            var queueCreator = builder.Build<ICreateQueues>();
            var queueBindings = new QueueBindings();
            queueBindings.BindReceiving(settings.LocalAddress());
            await queueCreator.CreateQueueIfNecessary(queueBindings, WindowsIdentity.GetCurrent().Name);

            builder.Build<CriticalError>().Endpoint = runningInstance;

            await pipelineCollection.Start().ConfigureAwait(false);

            return runningInstance;
        }

        PipelineCollection CreateIncomingPipelines()
        {
            var pipelines = BuildPipelines().ToArray();
            var pipelineCollection = new PipelineCollection(pipelines);
            return pipelineCollection;
        }

        IEnumerable<TransportReceiver> BuildPipelines()
        {
            var requiredTransactionSupport = settings.Get<InboundTransport>().Definition.GetTransactionSupport();

            var pushSettings = new PushSettings(settings.LocalAddress(), "error", settings.GetOrDefault<bool>("Transport.PurgeOnStartup"), requiredTransactionSupport);

            yield return BuildPipelineInstance(pipelineConfiguration.MainPipeline, "Main", pushSettings, pushRuntimeSettings);
        }

        TransportReceiver BuildPipelineInstance(PipelineModifications modifications, string name, PushSettings pushSettings, PushRuntimeSettings runtimeSettings)
        {
            var pipelineInstance = new PipelineBase<TransportReceiveContext>(builder, settings, modifications);
            var receiver = new TransportReceiver(
                name,
                builder,
                builder.Build<IPushMessages>(),
                pushSettings,
                pipelineInstance,
                runtimeSettings);

            return receiver;
        }
    }
}