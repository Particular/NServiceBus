using System;
using System.Collections.Generic;
using System.Web.Mvc;
using NServiceBus.ObjectBuilder;

namespace Website
{
    public class NServiceBusDependencyResolverAdapter : IDependencyResolver
    {
        private IBuilder builder;
 
        public NServiceBusDependencyResolverAdapter(IBuilder builder)
        {
            this.builder = builder;
        }
        
        public object GetService(Type serviceType)
        {
            if (NServiceBus.Configure.Instance.Configurer.HasComponent(serviceType))
                return builder.Build(serviceType);
            else
                return null;
        }

        public IEnumerable<object> GetServices(Type serviceType)
        {
            return builder.BuildAll(serviceType); 
        }
    }
}