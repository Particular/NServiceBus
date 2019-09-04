namespace NServiceBus.AcceptanceTests.Core.DependencyInjection
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
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
            return new ResolutionPhaseAdapter(container.BuildChildContainer());
        }

        public void Dispose()
        {
            //Injected at compile time
        }

        public T Build<T>()
        {
            return (T)container.Build(typeof(T));
        }

        public object Build(Type typeToBuild)
        {
            return container.Build(typeToBuild);
        }

        IEnumerable<object> IBuilder.BuildAll(Type typeToBuild)
        {
            return container.BuildAll(typeToBuild);
        }

        void IBuilder.Release(object instance)
        {
            container.Release(instance);
        }

        public IEnumerable<T> BuildAll<T>()
        {
            return container.BuildAll(typeof(T)).Cast<T>();
        }

        public void BuildAndDispatch(Type typeToBuild, Action<object> action)
        {
            var o = container.Build(typeToBuild);
            action(o);
        }

        void DisposeManaged()
        {
            container?.Dispose();
        }

        IContainer container;
    }
}

