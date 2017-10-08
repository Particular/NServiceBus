﻿namespace NServiceBus.Features
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using ObjectBuilder;
    using Pipeline;
    using Settings;
    using Transport;

    /// <summary>
    /// The context available to features when they are activated.
    /// </summary>
    public partial class FeatureConfigurationContext
    {
        internal FeatureConfigurationContext(ReadOnlySettings settings, IConfigureComponents container, PipelineSettings pipelineSettings, RoutingComponent routing, ReceiveConfiguration receiving)
        {
            Settings = settings;
            Container = container;
            Pipeline = pipelineSettings;
            Routing = routing;
            Receiving = receiving;

            TaskControllers = new List<FeatureStartupTaskController>();
        }

        /// <summary>
        /// A read only copy of the settings.
        /// </summary>
        public ReadOnlySettings Settings { get; }

        /// <summary>
        /// Access to the container to allow for registrations.
        /// </summary>
        public IConfigureComponents Container { get; }

        /// <summary>
        /// Access to the pipeline in order to customize it.
        /// </summary>
        public PipelineSettings Pipeline { get; }

        internal RoutingComponent Routing { get; }

        internal ReceiveConfiguration Receiving { get; }

        internal List<FeatureStartupTaskController> TaskControllers { get; }

        /// <summary>
        /// Adds a new satellite receiver.
        /// </summary>
        /// <param name="name">Name of the satellite.</param>
        /// <param name="runtimeSettings">Transport runtime settings.</param>
        /// <param name="onMessage">The message func.</param>
        /// <param name="transportAddress">The autogenerated transport address to listen on.</param>
        /// <param name="recoverabilityPolicy">Recoverability policy to be if processing fails.</param>
        public void AddSatelliteReceiver(string name, string transportAddress, PushRuntimeSettings runtimeSettings, Func<RecoverabilityConfig, ErrorContext, RecoverabilityAction> recoverabilityPolicy, Func<IBuilder, MessageContext, Task> onMessage)
        {
            Guard.AgainstNullAndEmpty(nameof(name), name);
            Guard.AgainstNullAndEmpty(nameof(transportAddress), transportAddress);
            Guard.AgainstNull(nameof(runtimeSettings), runtimeSettings);
            Guard.AgainstNull(nameof(recoverabilityPolicy), recoverabilityPolicy);
            Guard.AgainstNull(nameof(onMessage), onMessage);

            Receiving.AddSatelliteReceiver(name, transportAddress, runtimeSettings, recoverabilityPolicy, onMessage);
        }

        /// <summary>
        /// Registers an instance of a feature startup task.
        /// </summary>
        /// <param name="startupTask">A startup task.</param>
        public void RegisterStartupTask<TTask>(TTask startupTask) where TTask : FeatureStartupTask
        {
            Guard.AgainstNull(nameof(startupTask), startupTask);
            RegisterStartupTask(() => startupTask);
        }

        /// <summary>
        /// Registers a startup task factory.
        /// </summary>
        /// <param name="startupTaskFactory">A startup task factory.</param>
        public void RegisterStartupTask<TTask>(Func<TTask> startupTaskFactory) where TTask : FeatureStartupTask
        {
            Guard.AgainstNull(nameof(startupTaskFactory), startupTaskFactory);
            TaskControllers.Add(new FeatureStartupTaskController(typeof(TTask).Name, _ => startupTaskFactory()));
        }

        /// <summary>
        /// Registers a startup task factory which gets access to the builder.
        /// </summary>
        /// <param name="startupTaskFactory">A startup task factory.</param>
        /// <remarks>Should only be used when really necessary. Usually a design smell.</remarks>
        public void RegisterStartupTask<TTask>(Func<IBuilder, TTask> startupTaskFactory) where TTask : FeatureStartupTask
        {
            Guard.AgainstNull(nameof(startupTaskFactory), startupTaskFactory);
            TaskControllers.Add(new FeatureStartupTaskController(typeof(TTask).Name, startupTaskFactory));
        }
    }
}