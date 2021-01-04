using NServiceBus.Transport;

namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Transactions;
    using Configuration.AdvancedExtensibility;
    using Microsoft.Extensions.DependencyInjection;
    using Pipeline;
    using Settings;

    /// <summary>
    /// Configuration used to create an endpoint instance.
    /// </summary>
    public partial class EndpointConfiguration : ExposeSettings
    {
        /// <summary>
        /// Initializes the endpoint configuration builder.
        /// </summary>
        /// <param name="endpointName">The name of the endpoint being configured.</param>
        public EndpointConfiguration(string endpointName)
            : base(new SettingsHolder())
        {
            ValidateEndpointName(endpointName);

            Settings.Set("NServiceBus.Routing.EndpointName", endpointName);

            Settings.SetDefault("Endpoint.SendOnly", false);
            Settings.SetDefault("Transactions.IsolationLevel", IsolationLevel.ReadCommitted);
            Settings.SetDefault("Transactions.DefaultTimeout", TransactionManager.DefaultTimeout);

            Settings.Set(new AssemblyScanningComponent.Configuration(Settings));
            Settings.Set(new HostingComponent.Settings(Settings));
            Settings.Set(new TransportSeam.Settings(Settings));
            Settings.Set(new RoutingComponent.Settings(Settings));
            Settings.Set(new ReceiveComponent.Settings(Settings));
            Settings.Set(new RecoverabilityComponent.Configuration());
            Settings.Set(Pipeline = new PipelineSettings(Settings));

            Notifications = new Notifications();
            Settings.Set(Notifications);

            ConventionsBuilder = new ConventionsBuilder(Settings);
        }

        /// <summary>
        /// Access to the current endpoint <see cref="Notifications" />.
        /// </summary>
        public Notifications Notifications { get; }

        /// <summary>
        /// Access to the pipeline configuration.
        /// </summary>
        public PipelineSettings Pipeline { get; }

        /// <summary>
        /// Used to configure components in the container.
        /// </summary>
        public void RegisterComponents(Action<IServiceCollection> registration)
        {
            Guard.AgainstNull(nameof(registration), registration);

            Settings.Get<HostingComponent.Settings>().UserRegistrations.Add(registration);
        }

        /// <summary>
        /// Configures the endpoint to be send-only.
        /// </summary>
        public void SendOnly()
        {
            Settings.Set("Endpoint.SendOnly", true);
        }

        /// <summary>
        /// Defines the conventions to use for this endpoint.
        /// </summary>
        public ConventionsBuilder Conventions()
        {
            return ConventionsBuilder;
        }

        /// <summary>
        /// Configures NServiceBus to use the given transport.
        /// </summary>
        public void UseTransport(TransportDefinition transportDefinition)
        {
            Settings.Get<TransportSeam.Settings>().TransportDefinition = transportDefinition;
        }

        //This needs to be here since we have downstreams that use reflection to access this property
        internal void TypesToScanInternal(IEnumerable<Type> typesToScan)
        {
            Settings.Get<AssemblyScanningComponent.Configuration>().UserProvidedTypes = typesToScan.ToList();
        }

        internal void FinalizeConfiguration(List<Type> availableTypes)
        {
            Settings.SetDefault(ConventionsBuilder.Conventions);

            ActivateAndInvoke<INeedInitialization>(availableTypes, t => t.Customize(this));
            ActivateAndInvoke<IWantToRunBeforeConfigurationIsFinalized>(availableTypes, t => t.Run(Settings));
        }

        internal ConventionsBuilder ConventionsBuilder;

        static void ValidateEndpointName(string endpointName)
        {
            if (string.IsNullOrWhiteSpace(endpointName))
            {
                throw new ArgumentException("Endpoint name must not be empty", nameof(endpointName));
            }

            if (endpointName.Contains("@"))
            {
                throw new ArgumentException("Endpoint name must not contain an '@' character.", nameof(endpointName));
            }
        }

        static void ActivateAndInvoke<T>(IList<Type> types, Action<T> action) where T : class
        {
            ForAllTypes<T>(types, t =>
            {
                if (!HasDefaultConstructor(t))
                {
                    throw new Exception($"Unable to create the type '{t.Name}'. Types implementing '{typeof(T).Name}' must have a public parameterless (default) constructor.");
                }

                var instanceToInvoke = (T)Activator.CreateInstance(t);
                action(instanceToInvoke);
            });
        }

        static bool HasDefaultConstructor(Type type) => type.GetConstructor(Type.EmptyTypes) != null;

        static void ForAllTypes<T>(IEnumerable<Type> types, Action<Type> action) where T : class
        {
            foreach (var type in types.Where(t => typeof(T).IsAssignableFrom(t) && !(t.IsAbstract || t.IsInterface)))
            {
                action(type);
            }
        }

        static bool IsIWantToRunBeforeConfigurationIsFinalized(Type type)
        {
            return typeof(IWantToRunBeforeConfigurationIsFinalized).IsAssignableFrom(type);
        }
    }
}