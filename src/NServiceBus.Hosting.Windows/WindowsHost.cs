namespace NServiceBus.Hosting.Windows
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Threading;
    using Config;

    /// <summary>
    /// A windows implementation of the NServiceBus hosting solution
    /// </summary>
    public class WindowsHost : MarshalByRefObject
    {
        readonly GenericHost genericHost;
        readonly bool runOtherInstallers;

        /// <summary>
        /// Accepts the type which will specify the users custom configuration.
        /// This type should implement <see cref="IConfigureThisEndpoint"/>.
        /// </summary>
        /// <param name="scannableAssembliesFullName">Name of scan-able assemblies</param>
        public WindowsHost(Type endpointType, string[] args, string endpointName, bool runOtherInstallers, IEnumerable<string> scannableAssembliesFullName)
        {
            var specifier = (IConfigureThisEndpoint)Activator.CreateInstance(endpointType);

            genericHost = new GenericHost(specifier, args, new List<Type> { typeof(Production) }, endpointName, scannableAssembliesFullName);

            Configure.Instance.DefineCriticalErrorAction(OnCriticalError);

            if (runOtherInstallers || Debugger.IsAttached)
            {
                this.runOtherInstallers = true;
            }
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
            genericHost.Start();
        }

        /// <summary>
        /// Does shutdown work.
        /// </summary>
        public void Stop()
        {
            genericHost.Stop();
        }

        /// <summary>
        /// Performs installations
        /// </summary>
        /// <param name="username">Username passed in to host.</param>
        public void Install(string username)
        {
            if (runOtherInstallers)
            {
                Installer<Installation.Environments.Windows>.RunOtherInstallers = true;
            }

            //HACK: to force username to passed through to the 
            WindowsInstallerRunner.RunInstallers = true;
            WindowsInstallerRunner.RunAs = username;

            genericHost.Install<Installation.Environments.Windows>(username);
        }
    }
}