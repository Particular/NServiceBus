using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NServiceBus.ObjectBuilder.Common;
using Castle.DynamicProxy;
using Castle.Windsor;
using Castle.MicroKernel;

namespace ObjectBuilder.CastleWindsor
{
    public class WindsorObjectBuilder : IBuilderInternal
    {
        private readonly IWindsorContainer container;
        private readonly ProxyGenerator proxyGenerator = new ProxyGenerator();

        public WindsorObjectBuilder(IWindsorContainer container)
        {
            this.container = container;
        }

        public IComponentConfig ConfigureComponent(Type concreteComponent, ComponentCallModelEnum ignored)
        {
            var handler = container.Kernel.GetAssignableHandlers(typeof(object))
                .Where(h => h.ComponentModel.Implementation == concreteComponent)
                .FirstOrDefault();
            if (handler == null)
                throw new InvalidOperationException("Could not find component with the implementation of: " +
                                                    concreteComponent);
            return new ConfigureComponentAdapter(handler);
        }

        public object Configure(Type concreteType, ComponentCallModelEnum callModel)
        {
            var componentConfig = ConfigureComponent(concreteType, callModel);
            return proxyGenerator.CreateClassProxy(concreteType, new RecordPropertySet(componentConfig));
        }

        public object Build(Type typeToBuild)
        {
            return container.Resolve(typeToBuild);
        }

        public T Build<T>()
        {
            return container.Resolve<T>();
        }

        public IEnumerable<T> BuildAll<T>()
        {
            return container.ResolveAll<T>();
        }

        public IEnumerable<object> BuildAll(Type typeToBuild)
        {
            foreach (var component in container.ResolveAll(typeToBuild))
            {
                yield return component;
            }
        }

        public void BuildAndDispatch(Type typeToBuild, Action<object> action)
        {
            var instance = Build(typeToBuild);
            action(instance);
        }
    }
}
