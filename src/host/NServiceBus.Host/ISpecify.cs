using System;
using System.Collections.Generic;
using System.Reflection;
using NServiceBus.Config.ConfigurationSource;
using NServiceBus.ObjectBuilder.Common;
using NServiceBus.Serialization;

namespace NServiceBus.Host
{
    /// <summary>
    /// Container class for sub-specifications.
    /// Implement the contained interfaces on the class which implements <see cref="IConfigureThisEndpoint"/>.
    /// </summary>
    public class ISpecify
    {
        /// <summary>
        /// Specify the name of the endpoint that will be used as the name of the installed Windows Service
        /// instead of the default name.
        /// </summary>
        public interface EndpointName
        {
            /// <summary>
            /// The name of the installed windows service.
            /// </summary>
            string EndpointName { get; }
        }
        
        /// <summary>
        /// Specify the types to be configured in the endpoint.
        /// </summary>
        public interface TypesToScan
        {
            /// <summary>
            /// The list of types that will be used by the rest of nServiceBus.
            /// </summary>
            IEnumerable<Type> TypesToScan { get; }
        }

        /// <summary>
        /// Specify the assemblies whose types will be configured in the endpoint.
        /// </summary>
        public interface AssembliesToScan
        {
            /// <summary>
            /// The list of assemblies whose types will be used by the rest of nServiceBus.
            /// </summary>
            IEnumerable<Assembly> AssembliesToScan { get; }
        }

        /// <summary>
        /// Specify the directory that will be scanned, and whose assembly files will be loaded and their types scanned.
        /// </summary>
        public interface ProbeDirectory
        {
            /// <summary>
            /// The directory to be scanned for assemblies.
            /// </summary>
            string ProbeDirectory { get; }
        }

        /// <summary>
        /// Specify additional code to be run at startup.
        /// </summary>
        public interface StartupAction
        {
            /// <summary>
            /// An action to be run at startup.
            /// </summary>
            Action StartupAction { get; }
        }

        /// <summary>
        /// Specify the type of endpoint that will be run after configuration is complete.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        public interface ToRun<T> where T : IMessageEndpoint { }

        /// <summary>
        /// Specify the XML serialization namespace that should be used.
        /// </summary>
        public interface XmlSerializationNamespace
        {
            /// <summary>
            /// The XML serialization namespace.
            /// </summary>
            string Namespace { get; }
        }

        /// <summary>
        /// Specify the level at which logging will be performed.
        /// </summary>
        public interface LoggingLevel
        {
            /// <summary>
            /// The logging level that will be set for this endpoint.
            /// </summary>
            log4net.Core.Level Level { get; }
        }

        /// <summary>
        /// Specify the order in which message handlers will be invoked.
        /// </summary>
        public interface MessageHandlerOrdering
        {
            /// <summary>
            /// In this method, use the order object to specify the order in which message handlers will be activated.
            /// </summary>
            /// <param name="order"></param>
            void SpecifyOrder(Order order);
        }

        /// <summary>
        /// Container class for sub-specifications
        /// </summary>
        public class ToUse
        {
            /// <summary>
            /// Specify the type of container that will be used for dependency injection in the endpoint.
            /// </summary>
            /// <typeparam name="T"></typeparam>
            public interface ContainerType<T> where T : IContainer { }

            /// <summary>
            /// Specify a container instance that will be used for dependency injection in the endpoint.
            /// </summary>
            public interface SpecificContainerInstance
            {
                /// <summary>
                /// Return an instance of the container the rest of nServiceBus will use.
                /// </summary>
                IContainer ContainerInstance { get; }
            }

            /// <summary>
            /// Specify that the XML serializer should be used.
            /// </summary>
            public interface XmlSerialization { }

            /// <summary>
            /// Specify that the given type will be used as the message serializer.
            /// </summary>
            public interface Serializer<T> where T : IMessageSerializer { }
        }

        /// <summary>
        /// Container class for sub-specifications
        /// </summary>
        public class MyOwn
        {
            /// <summary>
            /// Specify that serialization will be independently configured in the container.
            /// </summary>
            public interface Serialization : IWantCustomInitialization { }

            /// <summary>
            /// Specify that saga persistence will be independently configured.
            /// </summary>
            public interface SagaPersistence : IWantCustomInitialization { }

            /// <summary>
            /// Specify that Log4Net will be independently configured.
            /// </summary>
            public interface Log4NetConfiguration
            {
                /// <summary>
                /// In this method, do what you want to configure Log4Net.
                /// </summary>
                void ConfigureLog4Net();
            }

            /// <summary>
            /// Specify an alternate config source to use
            /// </summary>
            public interface ConfigurationSource
            {
                /// <summary>
                /// Source from which to pull configuration information.
                /// </summary>
                IConfigurationSource Source { get; }
            }
        }

    }
}
