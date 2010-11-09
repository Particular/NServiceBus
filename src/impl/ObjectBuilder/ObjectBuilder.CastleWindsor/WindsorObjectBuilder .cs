using System;
using System.Collections.Generic;
using System.Linq;
using Castle.Core.Resource;
using Castle.MicroKernel.Releasers;
using Castle.Windsor.Configuration;
using Castle.Windsor.Configuration.Interpreters;
using NServiceBus.ObjectBuilder.Common;
using Castle.Windsor;
using Castle.MicroKernel;
using Castle.Core;
using Castle.MicroKernel.Registration;

namespace NServiceBus.ObjectBuilder.CastleWindsor
{
    /// <summary>
    /// Castle Windsor implementaton of IContainer.
    /// </summary>
    public class WindsorObjectBuilder : IContainer
    {
        /// <summary>
        /// The container itself.
        /// </summary>
        private IWindsorContainer container { get; set; }

        private bool disposed;

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
            if(container == null)
                throw new ArgumentNullException("container","The object builder must be initialized with a valid windsor container");

            this.container = container;
        }

        /// <summary>
        /// Disposes the container and all resources instantiated by the container.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
        }
        protected virtual void Dispose(bool disposing)
        {
            if (!disposing || disposed)
                return;

            disposed = true;
            container.Dispose();
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Returns a child instance of the container to facilitate deterministic disposal
        /// of all resources built by the child container.
        /// </summary>
        /// <returns></returns>
        public IContainer BuildChildContainer()
        {
            return new WindsorObjectBuilder(new WindsorContainer{Parent = container});
        }

        void IContainer.Configure(Type concreteComponent, ComponentCallModelEnum callModel)
        {
            var handler = GetHandlerForType(concreteComponent);
            if (handler == null)
            {
                var lifestyle = GetLifestyleTypeFrom(callModel);

                var reg = Component.For(GetAllServiceTypesFor(concreteComponent)).ImplementedBy(concreteComponent);
                reg.LifeStyle.Is(lifestyle);
                        
                container.Kernel.Register(reg);
            }
        }

        void IContainer.ConfigureProperty(Type component, string property, object value)
        {
            var handler = GetHandlerForType(component);
            if (handler == null)
                throw new InvalidOperationException("Cannot configure property for a type which hadn't been configured yet. Please call 'Configure' first.");

            handler.AddCustomDependencyValue(property, value);
        }

        void IContainer.RegisterSingleton(Type lookupType, object instance)
        {
            container.Kernel.AddComponentInstance(Guid.NewGuid().ToString(), lookupType, instance);
        }

        object IContainer.Build(Type typeToBuild)
        {       
            return container.Resolve(typeToBuild);  
        }

        IEnumerable<object> IContainer.BuildAll(Type typeToBuild)
        {
            return container.ResolveAll(typeToBuild).Cast<object>();
        }

        bool IContainer.HasComponent(Type componentType)
        {
            return container.Kernel.HasComponent(componentType);
        }

        private static LifestyleType GetLifestyleTypeFrom(ComponentCallModelEnum callModel)
        {
            switch (callModel)
            {
                case ComponentCallModelEnum.Singlecall: return LifestyleType.Transient;
                case ComponentCallModelEnum.Singleton: return LifestyleType.Singleton;
            }

            return LifestyleType.Undefined;
        }

        private static IEnumerable<Type> GetAllServiceTypesFor(Type t)
        {
            if (t == null)
                return new List<Type>();

            var result = new List<Type>(t.GetInterfaces()) {t};

            foreach (var interfaceType in t.GetInterfaces())
                result.AddRange(GetAllServiceTypesFor(interfaceType));

            return result;
        }

        private IHandler GetHandlerForType(Type concreteComponent)
        {
            return container.Kernel.GetAssignableHandlers(typeof(object))
                .Where(h => h.ComponentModel.Implementation == concreteComponent)
                .FirstOrDefault();
        }
    }

    internal class NoOpInterpreter:AbstractInterpreter

    {
        public override void ProcessResource(IResource resource, IConfigurationStore store)
        {
            
        }
    }
}
