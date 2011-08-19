using System;
using System.Collections.Generic;
using log4net;
using NServiceBus.Hosting.Configuration;
using NServiceBus.Hosting.Helpers;
using NServiceBus.Hosting.Profiles;
using NServiceBus.Hosting.Roles;
using NServiceBus.Hosting.Wcf;

namespace NServiceBus.Hosting
{
    /// <summary>
    /// A generic host that can be used to provide hosting services in different environments
    /// </summary>
    public class GenericHost : IHost
    {
        /// <summary>
        /// Does startup work.
        /// </summary>
        public void Start()
        {
            try
            {
                if (specifier is IWantCustomLogging)
                    (specifier as IWantCustomLogging).Init();
                else
                {
                    var loggingConfigurer = profileManager.GetLoggingConfigurer();
                    loggingConfigurer.Configure(specifier);
                }

                if (specifier is IWantCustomInitialization)
                {
                    try
                    {
                        if (specifier is IWantCustomLogging)
                        {
                            bool called = false;
                            //make sure we don't call the Init method again, unless there's an explicit impl
                            var initMap = specifier.GetType().GetInterfaceMap(typeof (IWantCustomInitialization));
                            foreach (var m in initMap.TargetMethods)
                                if (!m.IsPublic && m.Name == "NServiceBus.IWantCustomInitialization.Init")
                                {
                                    (specifier as IWantCustomInitialization).Init();
                                    called = true;
                                }

                            if (!called)
                            {
                                //call the regular Init method if IWantCustomLogging was an explicitly implemented method
                                var logMap = specifier.GetType().GetInterfaceMap(typeof (IWantCustomLogging));
                                foreach (var tm in logMap.TargetMethods)
                                    if (!tm.IsPublic && tm.Name == "NServiceBus.IWantCustomLogging.Init")
                                        (specifier as IWantCustomInitialization).Init();
                            }
                        }
                        else
                            (specifier as IWantCustomInitialization).Init();
                    }
                    catch(NullReferenceException ex)
                    {
                        throw new NullReferenceException("NServiceBus has detected a null reference in your initalization code." + 
                            " This could be due to trying to use NServiceBus.Configure before it was ready." +
                            " One possible solution is to inherit from IWantCustomInitialization in a different class" +
                            " than the one that inherits from IConfigureThisEndpoint, and put your code there.", ex);
                    }
                }

                if (Configure.Instance == null)
                    Configure.With();

                if (Configure.Instance.Configurer == null || Configure.Instance.Builder == null)
                    Configure.Instance.DefaultBuilder();

                roleManager.ConfigureBusForEndpoint(specifier);

                configManager.ConfigureCustomInitAndStartup();

                profileManager.ActivateProfileHandlers();

                if (Address.Local == null)
                {
                    var arr = specifier.GetType().GetCustomAttributes(typeof(EndpointNameAttribute), false);
                    if (arr.Length == 1)
                    {
                        string q = (arr[0] as EndpointNameAttribute).Name;
                        Address.InitializeLocalAddress(q);
                    }
                    else
                    {
                        string q = specifier.GetType().Namespace;
                        Address.InitializeLocalAddress(q);
                    }
                }

                var bus = Configure.Instance.CreateBus();
                if (bus != null)
                    bus.Start();

                configManager.Startup();
                wcfManager.Startup();
            }
            catch (Exception ex)
            {
                //we log the error here in order to avoid issues with non serializable exceptions
                //going across the appdomain back to topshelf
                LogManager.GetLogger(typeof(GenericHost)).Fatal(ex);

                throw new Exception("Exception when starting endpoint, error has been logged. Reason: " + ex.Message, ex);
            }
        }

        /// <summary>
        /// Does shutdown work.
        /// </summary>
        public void Stop()
        {
            configManager.Shutdown();
            wcfManager.Shutdown();
        }

        /// <summary>
        /// Accepts the type which will specify the users custom configuration.
        /// This type should implement <see cref="IConfigureThisEndpoint"/>.
        /// </summary>
        /// <param name="specifier"></param>
        /// <param name="args"></param>
        /// <param name="defaultProfiles"></param>
        public GenericHost(IConfigureThisEndpoint specifier, string[] args,IEnumerable<Type> defaultProfiles)
        {
            this.specifier = specifier;

            var assembliesToScan = AssemblyScanner.GetScannableAssemblies();

            profileManager = new ProfileManager(assembliesToScan, specifier, args,defaultProfiles);
            configManager = new ConfigManager(assembliesToScan, specifier);
            wcfManager = new WcfManager(assembliesToScan);
            roleManager = new RoleManager(assembliesToScan);
        }

        private readonly IConfigureThisEndpoint specifier;
        private readonly ProfileManager profileManager;
        private readonly ConfigManager configManager;
        private readonly WcfManager wcfManager;
        private readonly RoleManager roleManager;
    }
}