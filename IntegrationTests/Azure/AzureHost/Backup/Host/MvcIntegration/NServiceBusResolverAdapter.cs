using System;
using System.Collections.Generic;
using System.Web.Mvc;
using NServiceBus.ObjectBuilder;

namespace Host
{
    public class NServiceBusResolverAdapter : IDependencyResolver
    {
        private readonly IBuilder builder;

        public NServiceBusResolverAdapter(IBuilder builder)
        {
            this.builder = builder;
        }

        public object GetService(Type serviceType)
        {
            return builder.Build(serviceType);
        }

        public IEnumerable<object> GetServices(Type serviceType)
        {
            return builder.BuildAll(serviceType);
        }
    }
}