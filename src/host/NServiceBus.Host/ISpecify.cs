using System;
using System.Collections.Generic;
using System.Reflection;
using NServiceBus.ObjectBuilder.Common;

namespace NServiceBus.Host
{
    /// <summary>
    /// Container class for interface specifications.
    /// Implement the contained interfaces on the class which implements <see cref="IConfigureThisEndpoint"/>.
    /// </summary>
    public class ISpecify
    {
        /// <summary>
        /// Specify the types to be configured in the endpoint.
        /// </summary>
        public interface TypesToScan
        {
            IEnumerable<Type> TypesToScan { get; }
        }

        /// <summary>
        /// Specify the assemblies whose types will be configured in the endpoint.
        /// </summary>
        public interface AssembliesToScan
        {
            IEnumerable<Assembly> AssembliesToScan { get; }
        }

        /// <summary>
        /// Specify the directory that will be scanned, and whose assembly files will be loaded and their types scanned.
        /// </summary>
        public interface ProbeDirectory
        {
            string ProbeDirectory { get; }
        }

        /// <summary>
        /// Specify the type of container that will be used for dependency injection in the endpoint.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        public interface ToUseContainer<T> where T : IContainer
        {
            
        }

        /// <summary>
        /// Specify the type of endpoint that will be run after configuration is complete.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        public interface ToRun<T> where T : IMessageEndpoint
        {
            
        }
    }
}
