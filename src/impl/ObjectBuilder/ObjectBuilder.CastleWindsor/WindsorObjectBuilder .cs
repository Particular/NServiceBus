using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NServiceBus.ObjectBuilder.Common;
using Castle.DynamicProxy;
using Castle.Windsor;
using Castle.MicroKernel;
using ObjectBuilder;

namespace NServiceBus.ObjectBuilder.CastleWindsor
{
    public class WindsorObjectBuilder : IBuilderInternal
    {
        public IWindsorContainer Container { get; set; }

        private readonly ProxyGenerator proxyGenerator = new ProxyGenerator();

        public IComponentConfig ConfigureComponent(Type concreteComponent, ComponentCallModelEnum ignored)
        {
            var handler = Container.Kernel.GetAssignableHandlers(typeof(object))
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
            return Container.Resolve(typeToBuild);
        }

        public IEnumerable<object> BuildAll(Type typeToBuild)
        {
            foreach (var component in Container.ResolveAll(typeToBuild))
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
