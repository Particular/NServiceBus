namespace NServiceBus
{
    using System;
    using LightInject;
    using NServiceBus.ObjectBuilder;

    class LightInjectResolver : IResolver
    {
        IServiceFactory serviceFactory;

        public LightInjectResolver(IServiceFactory serviceFactory)
        {
            this.serviceFactory = serviceFactory;
        }

        public object Build(Type typeToBuild)
        {
            return serviceFactory.GetInstance(typeToBuild);
        }

        public T Build<T>()
        {
            return (T)Build(typeof(T));
        }
    }
}