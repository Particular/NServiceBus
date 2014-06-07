namespace NServiceBus.Hosting.Windows
{
    using System;
    using System.Collections.Generic;
    using System.Threading;

    /// <summary>
    /// A windows implementation of the NServiceBus hosting solution
    /// </summary>
    public class WindowsHost : MarshalByRefObject
    {
        NServiceBus.GenericHost genericHost;

        /// <summary>
        /// Accepts the type which will specify the users custom configuration.
        /// This type should implement <see cref="IConfigureThisEndpoint"/>.
        /// </summary>
        public WindowsHost(Type endpointType, string[] args, string endpointName, IEnumerable<string> scannableAssembliesFullName)
        {
            var specifier = (IConfigureThisEndpoint)Activator.CreateInstance(endpointType);

            genericHost = new NServiceBus.GenericHost(specifier, args, new List<Type> { typeof(Production) }, endpointName, scannableAssembliesFullName);
#pragma warning disable 0618
            Configure.Instance.DefineCriticalErrorAction(OnCriticalError);
#pragma warning restore 0618
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

    }
}