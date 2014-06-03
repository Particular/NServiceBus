namespace NServiceBus.Hosting.Windows
{
    using System;
    using System.Collections.Generic;
    using Arguments;
    using Microsoft.Practices.ServiceLocation;

    /// <summary>
    /// Plugs into the generic service locator to return an instance of <see cref="GenericHost"/>.
    /// </summary>
    class HostServiceLocator : ServiceLocatorImplBase
    {
        /// <summary>
        /// Command line arguments.
        /// </summary>
        public static string[] Args;

        /// <summary>
        /// Returns an instance of <see cref="GenericHost"/>
        /// </summary>
        protected override object DoGetInstance(Type serviceType, string key)
        {
            var endpoint = Type.GetType(key, true);

            var arguments = new HostArguments(Args);

            var endpointName = string.Empty;
            if (arguments.EndpointName != null)
                endpointName = arguments.EndpointName;

            return new WindowsHost(endpoint, Args, endpointName, false, arguments.ScannedAssemblies.ToArray());
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