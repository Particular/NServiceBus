namespace NServiceBus.Hosting.Windows
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Reflection;
    using System.Threading;
    using Config;
    using Helpers;
    using Hosting.Profiles;
    using Hosting.Roles;
    using Logging;
    using Settings;
    using Utils;
    using Wcf;

    /// <summary>
    /// A windows implementation of the NServiceBus hosting solution
    /// </summary>
    public class WindowsHost : MarshalByRefObject
    {
        bool runOtherInstallers;

        string[] args;
        Type endpointType;
        List<string> scannableAssembliesFullName;
        WcfManager wcfManager;
        IStartableBus bus;
        Configure config;
        string endpointName;

        /// <summary>
        /// Accepts the type which will specify the users custom configuration.
        /// This type should implement <see cref="IConfigureThisEndpoint"/>.
        /// </summary>
        public WindowsHost(Type endpointType, string[] args, string endpointName, bool runOtherInstallers, List<string> scannableAssembliesFullName)
        {

            this.args = args;
            this.endpointType = endpointType;
            this.scannableAssembliesFullName = scannableAssembliesFullName;
            if (String.IsNullOrEmpty(endpointName))
            {
                this.endpointName = endpointType.Namespace ?? endpointType.Assembly.GetName().Name;
            }
            else
            {
                this.endpointName = endpointName;
            }


            this.runOtherInstallers = runOtherInstallers;
        }

        /// <summary>
        /// Windows hosting behavior when critical error occurs is suicide.
        /// </summary>
        private void OnCriticalError(string errorMessage, Exception exception)
        {
            if (Environment.UserInteractive)
            {
                Thread.Sleep(10000); // so that user can see on their screen the problem
            }
            
            Environment.FailFast(String.Format("The following critical error was encountered by NServiceBus:\n{0}\nNServiceBus is shutting down.", errorMessage), exception);
        }

        /// <summary>
        /// Does startup work.
        /// </summary>
        public void Start()
        {
            try
            {
                PerformConfiguration();

                bus = Configure.Instance.CreateBus();
                if (bus != null && !SettingsHolder.Instance.Get<bool>("Endpoint.SendOnly"))
                {
                    bus.Start();
                }

                wcfManager.Startup(Configure.Instance);
            }
            catch (Exception ex)
            {
                LogManager.GetLogger<WindowsHost>().Fatal("Exception when starting endpoint.", ex);
                throw;
            }
        }

        /// <summary>
        /// Does shutdown work.
        /// </summary>
        public void Stop()
        {
            wcfManager.Shutdown();

            if (bus != null)
            {
                bus.Shutdown();
                bus.Dispose();

                bus = null;
            }
        }

        /// <summary>
        /// Performs installations
        /// </summary>
        /// <param name="username">Username passed in to host.</param>
        public void Install(string username)
        {
            if (runOtherInstallers || Debugger.IsAttached)
            {
                Installer<Installation.Environments.Windows>.RunOtherInstallers = true;
            }

            //HACK: to force username to passed through to the 
            WindowsInstallerRunner.RunInstallers = true;
            WindowsInstallerRunner.RunAs = username;

            PerformConfiguration();
            //HACK: to ensure the installer runner performs its installation
            Configure.Instance.Initialize();
        }


        void PerformConfiguration()
        {
            var specifier = (IConfigureThisEndpoint)Activator.CreateInstance(endpointType);


            List<Assembly> assembliesToScan;
            Configure.GetEndpointNameAction = () => endpointName;
            Configure.DefineEndpointVersionRetriever = () => FileVersionRetriever.GetFileVersion(specifier.GetType());

            if (scannableAssembliesFullName == null || !scannableAssembliesFullName.Any())
            {
                var assemblyScanner = new AssemblyScanner();
                assemblyScanner.MustReferenceAtLeastOneAssembly.Add(typeof(IHandleMessages<>).Assembly);
                assembliesToScan = assemblyScanner
                    .GetScannableAssemblies()
                    .Assemblies;
            }
            else
            {
                assembliesToScan = scannableAssembliesFullName
                    .Select(Assembly.Load)
                    .ToList();
            }


            var defaultProfiles = new List<Type>
            {
                typeof(Production)
            };

            var profileManager = new ProfileManager(assembliesToScan, args, defaultProfiles);
            ProfileActivator.ProfileManager = profileManager;

            wcfManager = new WcfManager();
            var roleManager = new RoleManager(assembliesToScan);


            var loggingConfigurers = profileManager.GetLoggingConfigurer();
            foreach (var loggingConfigurer in loggingConfigurers)
            {
                loggingConfigurer.Configure(specifier);
            }

            var initialization = specifier as IWantCustomInitialization;
            if (initialization != null)
            {
                try
                {
                    config = initialization.Init();
                }
                catch (NullReferenceException ex)
                {
                    throw new NullReferenceException(
                        "NServiceBus has detected a null reference in your initialization code." +
                        " This could be due to trying to use NServiceBus.Configure before it was ready." +
                        " One possible solution is to inherit from IWantCustomInitialization in a different class" +
                        " than the one that inherits from IConfigureThisEndpoint, and put your code there.",
                        ex);
                }
            }
            else
            {
                config = Configure.With(assembliesToScan);

            }

            //TODO: dont do this if user has defined own OnCriticalError
            config.DefineCriticalErrorAction(OnCriticalError);

            ValidateThatIWantCustomInitIsOnlyUsedOnTheEndpointConfig();

            roleManager.ConfigureBusForEndpoint(specifier);
        }

        void ValidateThatIWantCustomInitIsOnlyUsedOnTheEndpointConfig()
        {
            var problems = config.TypesToScan.Where(t => typeof(IWantCustomInitialization).IsAssignableFrom(t) && !t.IsInterface && !typeof(IConfigureThisEndpoint).IsAssignableFrom(t)).ToList();

            if (!problems.Any())
            {
                return;
            }

            throw new Exception("IWantCustomInitialization is only valid on the same class as ICOnfigureThisEndpoint. Please use INeedInitialization instead. Found types: " + string.Join(",", problems.Select(t => t.FullName)));
        }
    }
}