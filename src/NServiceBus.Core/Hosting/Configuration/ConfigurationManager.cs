namespace NServiceBus.Hosting.Configuration
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using Helpers;

    /// <summary>
    /// Configures the host upon startup
    /// </summary>
    public class ConfigManager
    {
        /// <summary>
        /// Constructs the manager with the given user configuration and the list of assemblies that should be scanned
        /// </summary>
        public ConfigManager(List<Assembly> assembliesToScan, IConfigureThisEndpoint specifier)
        {
            this.specifier = specifier;

            toInitialize = assembliesToScan
                .AllTypesAssignableTo<IWantCustomInitialization>()
                .WhereConcrete()
                .Where(t => !typeof(IConfigureThisEndpoint).IsAssignableFrom(t))
                .ToList();
        }

        /// <summary>
        /// Configures the user classes that need custom config and those that are marked to run at startup
        /// </summary>
        public void ConfigureCustomInitAndStartup()
        {
            foreach (var t in toInitialize)
            {
                var o = (IWantCustomInitialization) Activator.CreateInstance(t);
                if (o is IWantTheEndpointConfig)
                    (o as IWantTheEndpointConfig).Config = specifier;

                o.Init();
            }
        }

        internal List<Type> toInitialize ;
        IConfigureThisEndpoint specifier;

    }
}