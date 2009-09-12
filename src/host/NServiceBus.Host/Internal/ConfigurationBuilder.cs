using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using NServiceBus.ObjectBuilder;
using NServiceBus.ObjectBuilder.Common;
using NServiceBus.Unicast.Config;
using NServiceBus.Serialization;
using NServiceBus.Utils.Reflection;

namespace NServiceBus.Host.Internal
{
    /// <summary>
    /// Utility class used to build up the configuration.
    /// </summary>
    public class ConfigurationBuilder
    {
        /// <summary>
        /// Constructs the builder passing in the given specifier object.
        /// </summary>
        /// <param name="specifier"></param>
        /// <param name="handlers"></param>
        public ConfigurationBuilder(IConfigureThisEndpoint specifier, IEnumerable<IHandleProfileConfiguration> handlers)
        {
            this.specifier = specifier;
            profileHandlers = handlers.ToList();
        }

        /// <summary>
        /// Uses information in the specifier to build up the configuration object returned
        /// </summary>
        /// <returns></returns>
        public Configure Build()
        {
            busConfiguration = DoConfigureWith(specifier);

            if (specifier is ISpecify.MyOwn.ConfigurationSource)
                busConfiguration.CustomConfigurationSource((specifier as ISpecify.MyOwn.ConfigurationSource).Source);

            ProcessContainer();

            busConfiguration.Sagas();

            profileHandlers.ForEach(ph => ph.ConfigureSagas(busConfiguration));

            if (specifier is AsA_Client && specifier is AsA_Server)
                throw new InvalidOperationException("Cannot specify endpoint both as a client and as a server.");

            ConfigUnicastBus configUnicastBus = null;

            if (specifier is AsA_Client)
                configUnicastBus = ConfigureClientRole();

            if (specifier is AsA_Server)
                configUnicastBus = ConfigureServerRole();

            if (configUnicastBus != null)
            {
                if (specifier is ISpecify.MessageHandlerOrdering)
                    (specifier as ISpecify.MessageHandlerOrdering).SpecifyOrder(new Order { config = configUnicastBus });
                else
                    configUnicastBus.LoadMessageHandlers();

                if (specifier is IDontWantToSubscribeAutomatically)
                    configUnicastBus.DoNotAutoSubscribe();
            }

            ProcessSerialization();

            ProcessThingsToRunAtStartup();

            if (specifier is IWantCustomInitialization)
                (specifier as IWantCustomInitialization).Init(busConfiguration);

            return busConfiguration;
        }

        private void ProcessThingsToRunAtStartup()
        {
            Configure.TypesToScan.Where(t => typeof(IWantToRunAtStartup).IsAssignableFrom(t) && !t.IsInterface)
                .ToList()
                .ForEach(t =>
                    Configure.TypeConfigurer.ConfigureComponent(t, ComponentCallModelEnum.Singleton)
                );
                
        }

        private void ProcessContainer()
        {
            if (specifier is ISpecify.ToUse.SpecificContainerInstance)
            {
                var container = (specifier as ISpecify.ToUse.SpecificContainerInstance).ContainerInstance;

                if (container != null)
                    ObjectBuilder.Common.Config.ConfigureCommon.With(busConfiguration, container);
            }
            else
            {
                var containerType = specifier.GetType().GetGenericallyContainedType(typeof(ISpecify.ToUse.ContainerType<>), typeof(IContainer));
 
                if (containerType != null)
                    ObjectBuilder.Common.Config.ConfigureCommon.With(
                        busConfiguration,
                        Activator.CreateInstance(containerType) as IContainer
                        );
                else
                    busConfiguration.SpringBuilder();
            }
        }

        private static Configure DoConfigureWith(IConfigureThisEndpoint specifier)
        {
            if (specifier is ISpecify.TypesToScan)
                return Configure.With((specifier as ISpecify.TypesToScan).TypesToScan);
            
            if (specifier is ISpecify.AssembliesToScan)
                return Configure.With(new List<Assembly>((specifier as ISpecify.AssembliesToScan).AssembliesToScan).ToArray());

            if (specifier is ISpecify.ProbeDirectory)
                return Configure.With((specifier as ISpecify.ProbeDirectory).ProbeDirectory);

            return Configure.With();
        }

        private void ProcessSerialization()
        {
            if (specifier is ISpecify.MyOwn.Serialization)
                return;

            if (specifier is ISpecify.ToUse.XmlSerialization)
            {
                if (specifier is ISpecify.XmlSerializationNamespace)
                    busConfiguration.XmlSerializer((specifier as ISpecify.XmlSerializationNamespace).Namespace);
                else
                    busConfiguration.XmlSerializer();
            }
            else
            {
                var serializerType = specifier.GetType().GetGenericallyContainedType(typeof(ISpecify.ToUse.Serializer<>), typeof(IMessageSerializer));

                if (serializerType != null)
                    Configure.TypeConfigurer
                        .ConfigureComponent(serializerType, ComponentCallModelEnum.Singleton);
                else
                    busConfiguration.BinarySerializer();
            }
        }

        private ConfigUnicastBus ConfigureServerRole()
        {
            var configUnicastBus = busConfiguration
                .MsmqTransport()
                .IsTransactional(true)
                .PurgeOnStartup(false)
                .UnicastBus()
                .ImpersonateSender(true);

            if (specifier is AsA_Publisher)
                profileHandlers.ForEach(ph => ph.ConfigureSubscriptionStorage(busConfiguration));

            return configUnicastBus;
        }

        private ConfigUnicastBus ConfigureClientRole()
        {
            var configUnicastBus = busConfiguration
                .MsmqTransport()
                .IsTransactional(false)
                .PurgeOnStartup(true)
                .UnicastBus()
                .ImpersonateSender(false);

            return configUnicastBus;
        }

        private readonly IConfigureThisEndpoint specifier;
        private Configure busConfiguration;

        private readonly List<IHandleProfileConfiguration> profileHandlers;

    }
}