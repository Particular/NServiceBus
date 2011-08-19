using System;
using System.Collections.Generic;
using Microsoft.Practices.ServiceLocation;

namespace NServiceBus.Hosting.Azure.HostProcess
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
            return new WindowsHost(endpoint, Args);
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