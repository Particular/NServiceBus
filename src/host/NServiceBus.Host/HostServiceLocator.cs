using System;
using System.Collections.Generic;
using Microsoft.Practices.ServiceLocation;

namespace NServiceBus.Host
{
    public class HostServiceLocator : ServiceLocatorImplBase
    {
    
        protected override object DoGetInstance(Type serviceType, string key)
        {
            Type endpoint = Type.GetType(key,true);
            return new GenericHost(endpoint);
        }
        protected override IEnumerable<object> DoGetAllInstances(Type serviceType)
        {
            throw new NotImplementedException();
        }
    }

}