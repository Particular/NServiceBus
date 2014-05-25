namespace NServiceBus.Hosting.Windows
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Arguments;
    using Microsoft.Practices.ServiceLocation;

    /// <summary>
    /// Plugs into the generic service locator to return an instance of <see cref="WindowsHost"/>.
    /// </summary>
    public class HostServiceLocator : ServiceLocatorImplBase
    {
        /// <summary>
        /// Command line arguments.
        /// </summary>
        public static string[] Args;

        /// <summary>
        /// Returns an instance of <see cref="WindowsHost"/>
        /// </summary>
        protected override object DoGetInstance(Type serviceType, string key)
        {
            var endpoint = Type.GetType(key, true);

            var arguments = new HostArguments(Args);

            return new WindowsHost(endpoint, Args, arguments.EndpointName, false, arguments.ScannedAssemblies.ToList());
        }

        /// <summary>
        /// Not implemented.
        /// </summary>
        protected override IEnumerable<object> DoGetAllInstances(Type serviceType)
        {
            throw new NotImplementedException();
        }
    }
}