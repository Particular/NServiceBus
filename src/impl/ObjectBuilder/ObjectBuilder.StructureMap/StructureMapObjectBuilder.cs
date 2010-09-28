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
        private readonly IDictionary<Type, ConfiguredInstance> configuredInstances = new Dictionary<Type, ConfiguredInstance>();
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

        void Common.IContainer.ReleaseInstance(object instance)
        {
            //no-op since structuremap doesn't have a release feature 
        }

        void Common.IContainer.ConfigureProperty(Type component, string property, object value)
        {
            if (value == null)
            {
                return;
            }

            lock (configuredInstances)
            {
                ConfiguredInstance configuredInstance;
                configuredInstances.TryGetValue(component, out configuredInstance);

                if (configuredInstance == null)
                    throw new InvalidOperationException("Cannot configure property before the component has been configured. Please call 'Configure' first.");

                if (value.GetType().IsSimple())
                    configuredInstance.WithProperty(property).EqualTo(value);
                else
                    configuredInstance.Child(property).Is(value);
            }
        }

        void Common.IContainer.Configure(Type component, ComponentCallModelEnum callModel)
        {
            lock (configuredInstances)
            {
                if(configuredInstances.ContainsKey(component))
                    return;
            }

            var lifecycle = GetLifecycleFrom(callModel);

            ConfiguredInstance configuredInstance = null;

            container.Configure(x =>
            {
                configuredInstance = x.For(component)
                     .LifecycleIs(lifecycle)
                     .Use(component);

                foreach (var implementedInterface in GetAllInterfacesImplementedBy(component))
                {
                    x.RegisterAdditionalInterfaceForPluginType(implementedInterface, component,lifecycle);

                    x.EnableSetterInjectionFor(implementedInterface);
                }
            });

            lock (configuredInstances)
                configuredInstances.Add(component, configuredInstance);
        }

        void Common.IContainer.RegisterSingleton(Type lookupType, object instance)
        {

            container.Inject(lookupType, instance);
            PluginCache.AddFilledType(lookupType);
        }

        bool Common.IContainer.HasComponent(Type componentType)
        {
            return container.Model.PluginTypes.Any(t => t.PluginType == componentType);
        }

        private static ILifecycle GetLifecycleFrom(ComponentCallModelEnum callModel)
        {
            switch (callModel)
            {
                case ComponentCallModelEnum.Singlecall: return new UniquePerRequestLifecycle();
                case ComponentCallModelEnum.Singleton: return new SingletonLifecycle();
            }

            throw new ArgumentException("Unhandled call model:" + callModel);
        }

        private static IEnumerable<Type> GetAllInterfacesImplementedBy(Type t)
        {
            var result = new List<Type>();

            if (t == null)
                return result;

            var interfaces = t.GetInterfaces().Where(x => (
                !x.IsGenericType
                ));
            result.AddRange(interfaces);

            foreach (Type interfaceType in interfaces)
                result.AddRange(GetAllInterfacesImplementedBy(interfaceType));

            return result;
        }

    }
}
