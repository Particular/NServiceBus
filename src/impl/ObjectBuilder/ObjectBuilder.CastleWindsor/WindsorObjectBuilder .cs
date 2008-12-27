using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NServiceBus.ObjectBuilder.Common;
using Castle.DynamicProxy;
using Castle.Windsor;
using Castle.MicroKernel;
using NServiceBus.ObjectBuilder;
using Castle.Core;
using Castle.MicroKernel.Registration;

namespace NServiceBus.ObjectBuilder.CastleWindsor
{
    /// <summary>
    /// Castle Windsor implementatino of IBuilderInternal.
    /// </summary>
    public class WindsorObjectBuilder : IBuilderInternal
    {
        /// <summary>
        /// The container itself.
        /// </summary>
        public IWindsorContainer Container { get; set; }

        /// <summary>
        /// Instantites the class with a new WindsorContainer.
        /// </summary>
        public WindsorObjectBuilder() : this(new WindsorContainer())
        {
        }

        /// <summary>
        /// Instantiates the class saving the given container.
        /// </summary>
        /// <param name="container"></param>
        public WindsorObjectBuilder(IWindsorContainer container)
        {
            this.Container = container;
        }

        private readonly ProxyGenerator proxyGenerator = new ProxyGenerator();

        /// <summary>
        /// Checks to see if the given type has been previously configured and, if so,
        /// returns the previously created component config; if not,
        /// registers the given type in the container with the given call model
        /// returning a new component config.
        /// </summary>
        /// <param name="concreteComponent"></param>
        /// <param name="callModel"></param>
        /// <returns></returns>
        public IComponentConfig ConfigureComponent(Type concreteComponent, ComponentCallModelEnum callModel)
        {
            var handler = Container.Kernel.GetAssignableHandlers(typeof(object))
                .Where(h => h.ComponentModel.Implementation == concreteComponent)
                .FirstOrDefault();
            if (handler == null)
            {
                LifestyleType lifestyle = GetLifestyleTypeFrom(callModel);

                ComponentRegistration<object> reg = Component.For(GetAllServiceTypesFor(concreteComponent)).ImplementedBy(concreteComponent);
                reg.LifeStyle.Is(lifestyle);
                        
                Container.Kernel.Register(reg);

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

        /// <summary>
        /// Uses the private proxyGenerator to proxy the given type after
        /// performing a regular ConfigureComponent.
        /// </summary>
        /// <param name="concreteType"></param>
        /// <param name="callModel"></param>
        /// <returns></returns>
        public object Configure(Type concreteType, ComponentCallModelEnum callModel)
        {
            var componentConfig = ConfigureComponent(concreteType, callModel);
            return proxyGenerator.CreateClassProxy(concreteType, new RecordPropertySet(componentConfig));
        }

        /// <summary>
        /// Uses the container to resolve the given type.
        /// </summary>
        /// <param name="typeToBuild"></param>
        /// <returns></returns>
        public object Build(Type typeToBuild)
        {
            return Container.Resolve(typeToBuild);
        }

        /// <summary>
        /// Uses the container to resolve all instances compatible with the given type.
        /// </summary>
        /// <param name="typeToBuild"></param>
        /// <returns></returns>
        public IEnumerable<object> BuildAll(Type typeToBuild)
        {
            foreach (var component in Container.ResolveAll(typeToBuild))
            {
                yield return component;
            }
        }

        /// <summary>
        /// Uses the container to instantiate the given type and then invokes
        /// the given action on that instance.
        /// </summary>
        /// <param name="typeToBuild"></param>
        /// <param name="action"></param>
        public void BuildAndDispatch(Type typeToBuild, Action<object> action)
        {
            var instance = Build(typeToBuild);
            action(instance);
        }
    }
}
