namespace NServiceBus
{
    using System;
    using Autofac;
    using ObjectBuilder;

    class AutofacResolver : IResolver
    {
        readonly IComponentContext componentContext;

        public AutofacResolver(IComponentContext componentContext)
        {
            // need to resolve context here as it throws an objectdisposedexception when trying to access the passed context outside the callbacks scope.
            this.componentContext = componentContext.Resolve<IComponentContext>();
        }

        public object Build(Type typeToBuild)
        {
            return componentContext.Resolve(typeToBuild);
        }

        public T Build<T>()
        {
            return componentContext.Resolve<T>();
        }
    }
}