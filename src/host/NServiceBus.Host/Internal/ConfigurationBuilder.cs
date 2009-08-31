using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using System.Linq.Expressions;
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
        private readonly IConfigureThisEndpoint specifier;
        private Configure busConfiguration;

        private readonly List<IHandleProfileConfiguration> profileHandlers;

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
            profileHandlers.ForEach(ph => ph.ConfigureLogging());

            busConfiguration = null;

            if (specifier is ISpecify.TypesToScan)
                busConfiguration = Configure.With((specifier as ISpecify.TypesToScan).TypesToScan);
            else
                if (specifier is ISpecify.AssembliesToScan)
                    busConfiguration = Configure.With(new List<Assembly>((specifier as ISpecify.AssembliesToScan).AssembliesToScan).ToArray());
                else
                    if (specifier is ISpecify.ProbeDirectory)
                        busConfiguration = Configure.With((specifier as ISpecify.ProbeDirectory).ProbeDirectory);
                    else
                        busConfiguration = Configure.With();

            if (specifier is ISpecify.MyOwn.ConfigurationSource)
                busConfiguration.CustomConfigurationSource((specifier as ISpecify.MyOwn.ConfigurationSource).Source);

            IContainer container = null;


            if (specifier is ISpecify.ToUse.SpecificContainerInstance)
                container = (specifier as ISpecify.ToUse.SpecificContainerInstance).ContainerInstance;

            Type containerType = specifier.GetType().GetGenericallyContainedType(typeof(ISpecify.ToUse.ContainerType<>), typeof(IContainer));
            Type messageEndpointType = specifier.GetType().GetGenericallyContainedType(typeof(ISpecify.ToRun<>), typeof(IMessageEndpoint));

            if (container != null)
            {
                ObjectBuilder.Common.Config.ConfigureCommon.With(busConfiguration, container);
            }
            else if (containerType != null)
                ObjectBuilder.Common.Config.ConfigureCommon.With(
                    busConfiguration,
                    Activator.CreateInstance(containerType) as IContainer
                    );
            else
                busConfiguration.SpringBuilder();

            if (!(specifier is IDontWant.MsmqInitialization))
                Utils.MsmqInstallation.StartMsmqIfNecessary();

            if (!(specifier is IDontWant.Sagas))
            {
                busConfiguration.Sagas();

                profileHandlers.ForEach(ph => ph.ConfigureSagas(busConfiguration));
            }

            if (specifier is As.aClient && specifier is As.aServer)
                throw new InvalidOperationException("Cannot specify endpoint both as a client and as a server.");

            ConfigUnicastBus configUnicastBus = null;

            if (specifier is As.aClient)
                configUnicastBus = ConfigureClientRole();

            if (specifier is As.aServer)
                configUnicastBus = ConfigureServerRole();

            if (configUnicastBus != null)
            {
                if (specifier is ISpecify.MessageHandlerOrdering)
                    (specifier as ISpecify.MessageHandlerOrdering).SpecifyOrder(new Order { config = configUnicastBus });
                else
                    configUnicastBus.LoadMessageHandlers();

                if (specifier is IDontWant.ToSubscribeAutomatically)
                    configUnicastBus.DoNotAutoSubscribe();
            }

            ConfigureSerialization();

            if (specifier is IWantCustomInitialization)
                (specifier as IWantCustomInitialization).Init(busConfiguration);

            if (messageEndpointType != null)
                Configure.TypeConfigurer.ConfigureComponent(messageEndpointType, ComponentCallModelEnum.Singleton);
            
            return busConfiguration;
        }

        private void ConfigureSerialization()
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
            if (!(specifier is IDontWant.DtcInitialization))
                Utils.DtcUtil.StartDtcIfNecessary();

            var configUnicastBus = busConfiguration
                .MsmqTransport()
                .IsTransactional(true)
                .PurgeOnStartup(false)
                .UnicastBus()
                .ImpersonateSender(true);

            if (specifier is As.aPublisher)
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
    }
}