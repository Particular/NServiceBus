using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace NServiceBus.Hosting.Windows
{
    /// <summary>
    /// A windows implementation of the NServiceBus hosting solution
    /// </summary>
    public class WindowsHost : MarshalByRefObject
    {
        readonly GenericHost genericHost;
        readonly bool runOtherInstallers;
        readonly bool runInfrastructureInstallers;

        /// <summary>
        /// Accepts the type which will specify the users custom configuration.
        /// This type should implement <see cref="IConfigureThisEndpoint"/>.
        /// </summary>
        /// <param name="endpointType"></param>
        /// <param name="args"></param>
        /// <param name="endpointName"></param>
        /// <param name="runOtherInstallers"></param>
        /// <param name="runInfrastructureInstallers"></param>
        /// <param name="scannableAssembliesFullName">Name of scan-able assemblies</param>
        public WindowsHost(Type endpointType, string[] args, string endpointName, bool runOtherInstallers, bool runInfrastructureInstallers, IEnumerable<string> scannableAssembliesFullName)
        {
            var specifier = (IConfigureThisEndpoint)Activator.CreateInstance(endpointType);

            genericHost = new GenericHost(specifier, args, new[] { typeof(Production) }, endpointName, scannableAssembliesFullName);

            Configure.Instance.DefineCriticalErrorAction(OnCriticalError);

            if (runOtherInstallers || Debugger.IsAttached)
                this.runOtherInstallers = true;

            this.runInfrastructureInstallers = runInfrastructureInstallers;
        }

        /// <summary>
        /// Windows hosting behavior when critical error occurs is suicide.
        /// </summary>
        private void OnCriticalError()
        {
            Thread.Sleep(10000); // so that user can see on their screen the problem
            Process.GetCurrentProcess().Kill();
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
        public void Install()
        {
            if (runOtherInstallers)
                Installer<Installation.Environments.Windows>.RunOtherInstallers = true;
            if (runInfrastructureInstallers)
                Installer<Installation.Environments.Windows>.RunInfrastructureInstallers = true;

            genericHost.Install<Installation.Environments.Windows>();
        }

    }
}