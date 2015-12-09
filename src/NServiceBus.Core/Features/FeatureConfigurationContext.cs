namespace NServiceBus.Features
{
    using System;
    using System.Collections.Generic;
    using NServiceBus.ObjectBuilder;
    using NServiceBus.Pipeline;
    using NServiceBus.Settings;
    using NServiceBus.Transports;

    /// <summary>
    ///     The context available to features when they are activated.
    /// </summary>
    public class FeatureConfigurationContext
    {
        internal FeatureConfigurationContext(ReadOnlySettings settings, IConfigureComponents container, PipelineSettings pipelineSettings)
        {
            Settings = settings;
            Container = container;
            Pipeline = pipelineSettings;

            TaskControllers = new List<FeatureStartupTaskController>();
        }

        /// <summary>
        ///     A read only copy of the settings.
        /// </summary>
        public ReadOnlySettings Settings { get; }

        /// <summary>
        ///     Access to the container to allow for registrations.
        /// </summary>
        public IConfigureComponents Container { get; }

        /// <summary>
        ///     Access to the pipeline in order to customize it.
        /// </summary>
        public PipelineSettings Pipeline { get; }

        internal List<FeatureStartupTaskController> TaskControllers { get; } 

        /// <summary>
        ///     Creates a new satellite processing pipeline.
        /// </summary>
        public PipelineSettings AddSatellitePipeline(string name, string qualifier, TransportTransactionMode requiredTransportTransactionMode, PushRuntimeSettings runtimeSettings, out string transportAddress)
        {
            var instanceName = Settings.EndpointInstanceName();
            var satelliteLogicalAddress = new LogicalAddress(instanceName, qualifier);
            var addressTranslation = Settings.Get<LogicalToTransportAddressTranslation>();
            transportAddress = addressTranslation.Translate(satelliteLogicalAddress);

            var pipelineModifications = new SatellitePipelineModifications(name, transportAddress, requiredTransportTransactionMode, runtimeSettings);
            Settings.Get<PipelineConfiguration>().SatellitePipelines.Add(pipelineModifications);
            var newPipeline = new PipelineSettings(pipelineModifications);

            newPipeline.RegisterConnector<TransportReceiveToPhysicalMessageProcessingConnector>("Allows to abort processing the message");
            Settings.Get<QueueBindings>().BindReceiving(transportAddress);

            return newPipeline;
        }

        /// <summary>
        /// Registers an instance of a feature startup task.
        /// </summary>
        /// <param name="startupTask">A startup task.</param>
        public void RegisterStartupTask<TTask>(TTask startupTask) where TTask : FeatureStartupTask
        {
            RegisterStartupTask(() => startupTask);
        }

        /// <summary>
        /// Registers a startup task factory.
        /// </summary>
        /// <param name="startupTaskFactory">A startup task factory.</param>
        public void RegisterStartupTask<TTask>(Func<TTask> startupTaskFactory) where TTask : FeatureStartupTask
        {
            TaskControllers.Add(new FeatureStartupTaskController(typeof(TTask).Name, _ => startupTaskFactory()));
        }

        /// <summary>
        /// Registers a startup task factory which gets access to the builder.
        /// </summary>
        /// <param name="startupTaskFactory">A startup task factory.</param>
        /// <remarks>Should only be used when really necessary. Usually a design smell.</remarks>
        public void RegisterStartupTask<TTask>(Func<IBuilder, TTask> startupTaskFactory) where TTask : FeatureStartupTask
        {
            TaskControllers.Add(new FeatureStartupTaskController(typeof(TTask).Name, startupTaskFactory));
        }
    }
}