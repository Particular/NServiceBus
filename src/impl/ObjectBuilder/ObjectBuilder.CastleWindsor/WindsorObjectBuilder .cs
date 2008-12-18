using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NServiceBus.ObjectBuilder.Common;
using Castle.DynamicProxy;
using Castle.Windsor;
using Castle.MicroKernel;
using ObjectBuilder;
using Castle.Core;
using Castle.MicroKernel.Registration;

namespace NServiceBus.ObjectBuilder.CastleWindsor
{
    public class WindsorObjectBuilder : IBuilderInternal
    {
        public IWindsorContainer Container { get; set; }

        public WindsorObjectBuilder()
        {
            this.Container = new WindsorContainer();
        }

        private readonly ProxyGenerator proxyGenerator = new ProxyGenerator();

        public IComponentConfig ConfigureComponent(Type concreteComponent, ComponentCallModelEnum callModel)
        {
            var handler = Container.Kernel.GetAssignableHandlers(typeof(object))
                .Where(h => h.ComponentModel.Implementation == concreteComponent)
                .FirstOrDefault();
            if (handler == null)
            {
                LifestyleType lifestyle = GetLifestyleTypeFrom(callModel);

                Container.Kernel.Register(
                    Component.For(GetAllServiceTypesFor(concreteComponent))
                        .ImplementedBy(concreteComponent)
                    );

                handler = Container.Kernel.GetAssignableHandlers(typeof(object))
                    .Where(h => h.ComponentModel.Implementation == concreteComponent)
                    .FirstOrDefault();
            }

            return new ConfigureComponentAdapter(handler);
        }

        private LifestyleType GetLifestyleTypeFrom(ComponentCallModelEnum callModel)
        {
            switch (callModel)
            {
                case ComponentCallModelEnum.Singlecall: return LifestyleType.Transient;
                case ComponentCallModelEnum.Singleton: return LifestyleType.Singleton;
            }

            return LifestyleType.Undefined;
        }

        private IEnumerable<Type> GetAllServiceTypesFor(Type t)
        {
            if (t == null)
                return new List<Type>();

            List<Type> result = new List<Type>(t.GetInterfaces());
            result.Add(t);

            foreach (Type interfaceType in t.GetInterfaces())
                result.AddRange(GetAllServiceTypesFor(interfaceType));

            return result;
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
