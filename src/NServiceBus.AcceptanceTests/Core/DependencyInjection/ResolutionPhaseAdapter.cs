namespace NServiceBus.AcceptanceTests.Core.DependencyInjection
{
    using System;
    using System.Collections.Generic;
    using ObjectBuilder;
    using ObjectBuilder.Common;

    class ResolutionPhaseAdapter : IBuilder
    {
        public ResolutionPhaseAdapter(IContainer container)
        {
            this.container = container;
        }

        public IBuilder CreateChildBuilder()
        {
            throw new NotSupportedException();
        }

        public void Dispose()
        {
            container.Dispose();
        }

        public T Build<T>()
        {
            throw new NotSupportedException();
        }

        public object Build(Type typeToBuild)
        {
            throw new NotSupportedException();
        }

        IEnumerable<object> IBuilder.BuildAll(Type typeToBuild)
        {
            throw new NotSupportedException();
        }

        void IBuilder.Release(object instance)
        {
            throw new NotSupportedException();
        }

        public IEnumerable<T> BuildAll<T>()
        {
            throw new NotSupportedException();
        }

        public void BuildAndDispatch(Type typeToBuild, Action<object> action)
        {
            throw new NotSupportedException();
        }

        public object GetService(Type serviceType)
        {
            return container.Build(serviceType);
        }

        IContainer container;
    }
}

