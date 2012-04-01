using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Practices.ServiceLocation;
using NServiceBus.Hosting.Windows.Arguments;
using Topshelf.Internal;

namespace NServiceBus.Hosting.Windows
{
    /// <summary>
    /// Plugs into the generic service locator to return an instance of <see cref="GenericHost"/>.
    /// </summary>
    public class HostServiceLocator : ServiceLocatorImplBase
    {
        /// <summary>
        /// Command line arguments.
        /// </summary>
        public static string[] Args;

        /// <summary>
        /// Returns an instance of <see cref="GenericHost"/>
        /// </summary>
        /// <param name="serviceType"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        protected override object DoGetInstance(Type serviceType, string key)
        {
            var endpoint = Type.GetType(key,true);

            Parser.Args commandLineArguments = Parser.ParseArgs(Args);
            var arguments = new HostArguments(commandLineArguments);

            string endpointName = string.Empty;
            if (arguments.EndpointName != null)
                endpointName = arguments.EndpointName.Value;

            string[] scannedAssemblies = null;
            if (arguments.ScannedAssemblies != null)
                scannedAssemblies = arguments.ScannedAssemblies.Value.Split(';').ToArray();

            return new WindowsHost(endpoint, Args, endpointName, false, false, scannedAssemblies);
        }

        /// <summary>
        /// Not implemented.
        /// </summary>
        /// <param name="serviceType"></param>
        /// <returns></returns>
        protected override IEnumerable<object> DoGetAllInstances(Type serviceType)
        {
            throw new NotImplementedException();
        }
    }
}