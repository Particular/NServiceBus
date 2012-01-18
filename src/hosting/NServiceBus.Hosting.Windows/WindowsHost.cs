using System;
using System.Diagnostics;
using System.Threading;
using NServiceBus.Config.Conventions;
using log4net;

namespace NServiceBus.Hosting.Windows
{
    /// <summary>
    /// A windows implementation of the NServiceBus hosting solution
    /// </summary>
    public class WindowsHost : MarshalByRefObject
    {
        private readonly GenericHost genericHost;

        /// <summary>
        /// Accepts the type which will specify the users custom configuration.
        /// This type should implement <see cref="IConfigureThisEndpoint"/>.
        /// </summary>
        /// <param name="endpointType"></param>
        /// <param name="args"></param>
        /// <param name="endpointName"></param>
        public WindowsHost(Type endpointType, string[] args, string endpointName)
        {
            var specifier = (IConfigureThisEndpoint)Activator.CreateInstance(endpointType);

            genericHost = new GenericHost(specifier, args, new[] { typeof(Lite) }, endpointName);

            Configure.Instance.DefineCriticalErrorAction(OnCriticalError);
        }

        private static readonly ILog Logger = LogManager.GetLogger(typeof(WindowsHost));
        private void OnCriticalError(Exception exception)
        {
            Logger.Fatal(exception);
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
            genericHost.Install<Installation.Environments.Windows>();
            
        }

    }
}