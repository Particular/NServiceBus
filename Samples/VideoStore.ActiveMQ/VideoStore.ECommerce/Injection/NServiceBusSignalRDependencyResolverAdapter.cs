namespace VideoStore.ECommerce.Injection
{
    using System;
    using System.Collections.Generic;
    using Microsoft.AspNet.SignalR;
    using NServiceBus;
    using NServiceBus.ObjectBuilder;

    public class NServiceBusSignalRDependencyResolverAdapter : DefaultDependencyResolver
    {
        readonly IBuilder builder;

        public NServiceBusSignalRDependencyResolverAdapter(IBuilder builder)
        {
            this.builder = builder;
        }

        public override object GetService(Type serviceType)
        {
            if (Configure.Instance.Configurer.HasComponent(serviceType))
            {
                return builder.Build(serviceType);
            }

            return base.GetService(serviceType);
        }

        public override IEnumerable<object> GetServices(Type serviceType)
        {
            if (Configure.Instance.Configurer.HasComponent(serviceType))
            {
                return builder.BuildAll(serviceType);
            }

            return base.GetServices(serviceType);
        }
    }
}