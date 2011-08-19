using System;

namespace NServiceBus.Hosting.Azure.HostProcess
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
        public WindowsHost(Type endpointType, string[] args)
        {
            var specifier = (IConfigureThisEndpoint)Activator.CreateInstance(endpointType);

            Program.EndpointId = Program.GetEndpointId(specifier);

            genericHost = new GenericHost(specifier, args, new[] { typeof(Development) });
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