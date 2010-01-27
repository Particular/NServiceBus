using System;
using System.Collections.Generic;
using System.Reflection;
using NServiceBus.ObjectBuilder;
using log4net;

namespace NServiceBus.Host.Internal
{
    internal class ConfigManager
    {
        public ConfigManager(IEnumerable<Assembly> assembliesToScan, IConfigureThisEndpoint specifier)
        {
            this.specifier = specifier;

            foreach(var a in assembliesToScan)
                foreach(var t in a.GetTypes())
                {
                    if (typeof(IWantCustomInitialization).IsAssignableFrom(t) && !t.IsInterface && !typeof(IConfigureThisEndpoint).IsAssignableFrom(t))
                        toInitialize.Add(t);
                    if (typeof(IWantToRunAtStartup).IsAssignableFrom(t) && !t.IsInterface)
                        toRunAtStartup.Add(t);
                }
        }

        public void ConfigureCustomInitAndStartup()
        {
            foreach (var t in toRunAtStartup)
                Configure.Instance.Configurer.ConfigureComponent(t, ComponentCallModelEnum.Singleton);

            foreach (var t in toInitialize)
            {
                var o = (IWantCustomInitialization) Activator.CreateInstance(t);
                if (o is IWantTheEndpointConfig)
                    (o as IWantTheEndpointConfig).Config = specifier;

                o.Init();
            }
        }

        public void Startup()
        {
            thingsToRunAtStartup = Configure.Instance.Builder.BuildAll<IWantToRunAtStartup>();

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

        private readonly IList<Type> toInitialize = new List<Type>();
        private readonly IList<Type> toRunAtStartup = new List<Type>();
        private readonly IConfigureThisEndpoint specifier;

        private IEnumerable<IWantToRunAtStartup> thingsToRunAtStartup;
    }
}
