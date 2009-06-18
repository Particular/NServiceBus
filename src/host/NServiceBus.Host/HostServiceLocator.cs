using System;
using System.Collections.Generic;
using Microsoft.Practices.ServiceLocation;

namespace NServiceBus.Host
{
    public class HostServiceLocator : ServiceLocatorImplBase
    {
        private readonly Type endpointType;

        public HostServiceLocator(Type endpointType)
        {
            this.endpointType = endpointType;
        }

        protected override object DoGetInstance(Type serviceType, string key)
        {
            return new GenericHost(endpointType);
        }
        protected override IEnumerable<object> DoGetAllInstances(Type serviceType)
        {
            return new List<object> { new GenericHost(endpointType) };
        }
    }

}