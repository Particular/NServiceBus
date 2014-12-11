using System;
using System.Collections.Generic;
using System.Linq;
using StructureMap;
using StructureMap.Graph;
using StructureMap.Pipeline;
using StructureMap.TypeRules;
using IContainer = StructureMap.IContainer;

namespace NServiceBus.ObjectBuilder.StructureMap
{
    /// <summary>
    /// ObjectBuilder implementation for the StructureMap IoC-Container
    /// </summary>
    public class StructureMapObjectBuilder : Common.IContainer
    {
        private readonly IContainer container;
        private readonly IDictionary<Type, Instance> configuredInstances = new Dictionary<Type, Instance>();
        private bool disposed;

        public StructureMapObjectBuilder()
        {
            container = ObjectFactory.GetInstance<IContainer>();
        }

        public StructureMapObjectBuilder(IContainer container)
        {
            this.container = container;
        }

        /// <summary>
        /// Disposes the container and all resources instantiated by the container.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposed)
            {
                return;
            }

            disposed = true;
            if (disposing)
            {
                container.Dispose();
            }

        }

        ~StructureMapObjectBuilder()
        {
            Dispose(false);
        }

        /// <summary>
        /// Returns a child instance of the container to facilitate deterministic disposal
        /// of all resources built by the child container.
        /// </summary>
        /// <returns></returns>
        public Common.IContainer BuildChildContainer()
        {
            return new StructureMapObjectBuilder(container.GetNestedContainer());
        }

        object Common.IContainer.Build(Type typeToBuild)
        {
            if(container.Model.PluginTypes.Any(t=>t.PluginType == typeToBuild))
               return container.GetInstance(typeToBuild);
    
            throw new ArgumentException(typeToBuild + " is not registered in the container");
        }

        IEnumerable<object> Common.IContainer.BuildAll(Type typeToBuild)
        {
            return container.GetAllInstances(typeToBuild).Cast<object>();
        }

        void Common.IContainer.ConfigureProperty(Type component, string property, object value)
        {
            if (value == null)
            {
                return;
            }

            lock (configuredInstances)
            {
                Instance instance;
                configuredInstances.TryGetValue(component, out instance);

                var configuredInstance = instance as ConfiguredInstance;

                if (configuredInstance == null)
                    throw new InvalidOperationException("Cannot configure property before the component has been configured. Please call 'Configure' first.");

                if (value.GetType().IsSimple())
                    configuredInstance.WithProperty(property).EqualTo(value);
                else
                    configuredInstance.Child(property).Is(value);
            }
        }

        void Common.IContainer.Configure(Type component, DependencyLifecycle dependencyLifecycle)
        {
            lock (configuredInstances)
            {
                if(configuredInstances.ContainsKey(component))
                    return;
            }

            var lifecycle = GetLifecycleFrom(dependencyLifecycle);

            ConfiguredInstance configuredInstance = null;

            container.Configure(x =>
            {
                configuredInstance = x.For(component)
                     .LifecycleIs(lifecycle)
                     .Use(component);

                x.EnableSetterInjectionFor(component);

                foreach (var implementedInterface in GetAllInterfacesImplementedBy(component))
                {
                    x.RegisterAdditionalInterfaceForPluginType(implementedInterface, component,lifecycle);

                    x.EnableSetterInjectionFor(implementedInterface);
                }
            });

            lock (configuredInstances)
                configuredInstances.Add(component, configuredInstance);
        }

        void Common.IContainer.Configure<T>(Func<T> componentFactory, DependencyLifecycle dependencyLifecycle)
        {
            var pluginType = typeof(T);

            lock (configuredInstances)
            {
                if (configuredInstances.ContainsKey(pluginType))
                    return;
            }

            var lifecycle = GetLifecycleFrom(dependencyLifecycle);
            LambdaInstance<T> lambdaInstance = null;

            container.Configure(x =>
            {
                lambdaInstance = x.For<T>()
                    .LifecycleIs(lifecycle)
                    .Use(componentFactory);

                x.EnableSetterInjectionFor(pluginType);

                foreach (var implementedInterface in GetAllInterfacesImplementedBy(pluginType))
                {
                    x.RegisterAdditionalInterfaceForPluginType(implementedInterface, pluginType, lifecycle);
                    x.EnableSetterInjectionFor(implementedInterface);
                }
            }
                );

            lock (configuredInstances)
                configuredInstances.Add(pluginType, lambdaInstance);
        }

        void Common.IContainer.RegisterSingleton(Type lookupType, object instance)
        {
            container.Configure(x => x.For(lookupType)
                .Singleton()
                .Use(instance));
            PluginCache.AddFilledType(lookupType);
        }

        bool Common.IContainer.HasComponent(Type componentType)
        {
            return container.Model.PluginTypes.Any(t => t.PluginType == componentType);
        }

        void Common.IContainer.Release(object instance)
        {

        }

        private static ILifecycle GetLifecycleFrom(DependencyLifecycle dependencyLifecycle)
        {
            switch (dependencyLifecycle)
            {
                case DependencyLifecycle.InstancePerCall:
                    return new UniquePerRequestLifecycle();
                case DependencyLifecycle.SingleInstance: 
                    return new SingletonLifecycle();
                case DependencyLifecycle.InstancePerUnitOfWork:
                    return null;//null means the default lifecycle which is transient
            }

            throw new ArgumentException("Unhandled lifecycle - " + dependencyLifecycle);
        }

        private static IEnumerable<Type> GetAllInterfacesImplementedBy(Type t)
        {
            return t.GetInterfaces().Where(x=>x.FullName != null && !x.FullName.StartsWith("System."));
        }

    }
}
