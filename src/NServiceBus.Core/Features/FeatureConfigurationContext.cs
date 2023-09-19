﻿namespace NServiceBus.Features
{
    using System;
    using System.Collections.Generic;
    using Microsoft.Extensions.DependencyInjection;
    using Pipeline;
    using Settings;
    using Transport;

    /// <summary>
    /// The context available to features when they are activated.
    /// </summary>
    public partial class FeatureConfigurationContext
    {
        internal FeatureConfigurationContext(
            IReadOnlySettings settings,
            IServiceCollection container,
            PipelineSettings pipelineSettings,
            RoutingComponent.Configuration routing,
            ReceiveComponent.Configuration receiveConfiguration)
        {
            Settings = settings;
            Services = container;
            Pipeline = pipelineSettings;
            Routing = routing;
            this.receiveConfiguration = receiveConfiguration;

            TaskControllers = [];
        }

        /// <summary>
        /// A read only copy of the settings.
        /// </summary>
        public IReadOnlySettings Settings { get; }

        /// <summary>
        /// Access to the <see cref="IServiceCollection"/> to allow additional service registrations.
        /// </summary>
        public IServiceCollection Services { get; }

        /// <summary>
        /// Access to the pipeline in order to customize it.
        /// </summary>
        public PipelineSettings Pipeline { get; }

        internal RoutingComponent.Configuration Routing { get; }

        internal ReceiveComponent.Configuration Receiving => receiveConfiguration ?? throw new InvalidOperationException("Receive component is not enabled since this endpoint is configured to run in send-only mode.");

        internal List<FeatureStartupTaskController> TaskControllers { get; }

        /// <summary>
        /// Adds a new satellite receiver.
        /// </summary>
        /// <param name="name">Name of the satellite.</param>
        /// <param name="transportAddress">The autogenerated transport address to listen on.</param>
        /// <param name="runtimeSettings">Transport runtime settings.</param>
        /// <param name="recoverabilityPolicy">Recoverability policy to be if processing fails.</param>
        /// <param name="onMessage">The message func.</param>
        public void AddSatelliteReceiver(string name, QueueAddress transportAddress, PushRuntimeSettings runtimeSettings, Func<RecoverabilityConfig, ErrorContext, RecoverabilityAction> recoverabilityPolicy, OnSatelliteMessage onMessage)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name);
            ArgumentNullException.ThrowIfNull(transportAddress);
            ArgumentNullException.ThrowIfNull(runtimeSettings);
            ArgumentNullException.ThrowIfNull(recoverabilityPolicy);
            ArgumentNullException.ThrowIfNull(onMessage);

            Receiving.AddSatelliteReceiver(name, transportAddress, runtimeSettings, recoverabilityPolicy, onMessage);
        }

        /// <summary>
        /// Registers an instance of a feature startup task.
        /// </summary>
        /// <param name="startupTask">A startup task.</param>
        public void RegisterStartupTask<TTask>(TTask startupTask) where TTask : FeatureStartupTask
        {
            ArgumentNullException.ThrowIfNull(startupTask);
            RegisterStartupTask(() => startupTask);
        }

        /// <summary>
        /// Registers a startup task factory.
        /// </summary>
        /// <param name="startupTaskFactory">A startup task factory.</param>
        public void RegisterStartupTask<TTask>(Func<TTask> startupTaskFactory) where TTask : FeatureStartupTask
        {
            ArgumentNullException.ThrowIfNull(startupTaskFactory);
            TaskControllers.Add(new FeatureStartupTaskController(typeof(TTask).Name, _ => startupTaskFactory()));
        }

        /// <summary>
        /// Registers a startup task factory which gets access to the builder.
        /// </summary>
        /// <param name="startupTaskFactory">A startup task factory.</param>
        /// <remarks>Should only be used when really necessary. Usually a design smell.</remarks>
        public void RegisterStartupTask<TTask>(Func<IServiceProvider, TTask> startupTaskFactory) where TTask : FeatureStartupTask
        {
            ArgumentNullException.ThrowIfNull(startupTaskFactory);
            TaskControllers.Add(new FeatureStartupTaskController(typeof(TTask).Name, startupTaskFactory));
        }

        readonly ReceiveComponent.Configuration receiveConfiguration;
    }
}