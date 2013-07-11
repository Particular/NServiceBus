using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NServiceBus.Hosting.Helpers;
using NServiceBus.Logging;

namespace NServiceBus.Hosting.Configuration
{
    /// <summary>
    /// Configures the host upon startup
    /// </summary>
    public class ConfigManager
    {
        /// <summary>
        /// Contructs the manager with the given user configuration and the list of assemblies that should be scanned
        /// </summary>
        /// <param name="assembliesToScan"></param>
        /// <param name="specifier"></param>
        public ConfigManager(List<Assembly> assembliesToScan, IConfigureThisEndpoint specifier)
        {
            this.specifier = specifier;

            toInitialize = assembliesToScan
                .AllTypesAssignableTo<IWantCustomInitialization>()
                .WhereConcrete()
                .Where(t => !typeof(IConfigureThisEndpoint).IsAssignableFrom(t))
                .ToList();
            toRunAtStartup = assembliesToScan
                .AllTypesAssignableTo<IWantToRunAtStartup>()
                .WhereConcrete()
                .ToList();
        }

        /// <summary>
        /// Configures the user classes that need custom config and those that are marked to run at startup
        /// </summary>
        public void ConfigureCustomInitAndStartup()
        {
            foreach (var t in toRunAtStartup)
                Configure.Instance.Configurer.ConfigureComponent(t, DependencyLifecycle.InstancePerCall);

            foreach (var t in toInitialize)
            {
                var o = (IWantCustomInitialization) Activator.CreateInstance(t);
                if (o is IWantTheEndpointConfig)
                    (o as IWantTheEndpointConfig).Config = specifier;

                o.Init();
            }
        }

        /// <summary>
        /// Executes the user classes that are marked as "run at startup"
        /// </summary>
        public void Startup()
        {
            thingsToRunAtStartup = Configure.Instance.Builder.BuildAll<IWantToRunAtStartup>().ToList();

            if (thingsToRunAtStartup == null)
                return;

            foreach (var thing in thingsToRunAtStartup)
            {
                var toRun = thing;
                Action onstart = () =>
                                     {
                                         var logger = LogManager.GetLogger(toRun.GetType());
                                         try
                                         {
                                             logger.Debug("Calling " + toRun.GetType().Name);
                                             toRun.Run();
                                         }
                                         catch (Exception ex)
                                         {
                                             logger.Error("Problem occurred when starting the endpoint.", ex);

                                             //don't rethrow so that thread doesn't die before log message is shown.
                                         }

                                     };

                onstart.BeginInvoke(null, null);
            }
        }

        /// <summary>
        /// Shutsdown the user classes started earlier
        /// </summary>
        public void Shutdown()
        {
            if (thingsToRunAtStartup == null)
                return;

            foreach (var thing in thingsToRunAtStartup)
            {
                if (thing != null)
                {
                    var logger = LogManager.GetLogger(thing.GetType());
                    logger.Debug("Stopping " + thing.GetType().Name);
                    try
                    {
                        thing.Stop();
                    }
                    catch (Exception ex)
                    {
                        logger.Error(thing.GetType().Name + " could not be stopped.", ex);

                        // no need to rethrow, closing the process anyway
                    }
                }
            }
        }

        internal List<Type> toInitialize ;
        internal List<Type> toRunAtStartup;
        private readonly IConfigureThisEndpoint specifier;

        private IList<IWantToRunAtStartup> thingsToRunAtStartup;
    }
}