namespace NServiceBus.ObjectBuilder.StructureMap
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using global::StructureMap;
    using global::StructureMap.Graph;
    using global::StructureMap.Pipeline;
    using global::StructureMap.TypeRules;

    /// <summary>
    /// ObjectBuilder implementation for the StructureMap IoC-Container
    /// </summary>
    public class StructureMapObjectBuilder : Common.IContainer
    {
        IContainer container;
        IDictionary<Type, Instance> configuredInstances = new Dictionary<Type, Instance>();

        public StructureMapObjectBuilder()
        {
            container = ObjectFactory.GetInstance<IContainer>();
        }

        public StructureMapObjectBuilder(IContainer container)
        {
            this.container = container;
        }

        public void Dispose()
        {
            //Injected at compile time
        }

        /// <summary>
        /// Returns a child instance of the container to facilitate deterministic disposal
        /// of all resources built by the child container.
        /// </summary>
        public Common.IContainer BuildChildContainer()
        {
            return new StructureMapObjectBuilder(container.GetNestedContainer());
        }

        public object Build(Type typeToBuild)
        {
            if (container.Model.PluginTypes.Any(t => t.PluginType == typeToBuild))
            {
                return container.GetInstance(typeToBuild);
            }
    
            throw new ArgumentException(typeToBuild + " is not registered in the container");
        }

        public IEnumerable<object> BuildAll(Type typeToBuild)
        {
            return container.GetAllInstances(typeToBuild).Cast<object>();
        }

        public void ConfigureProperty(Type component, string property, object value)
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
                {
                    throw new InvalidOperationException("Cannot configure property before the component has been configured. Please call 'Configure' first.");
                }

                if (value.GetType().IsSimple())
                {
                    configuredInstance.WithProperty(property).EqualTo(value);
                }
                else
                {
                    configuredInstance.Child(property).Is(value);
                }
            }
        }

        public void Configure(Type component, DependencyLifecycle dependencyLifecycle)
        {
            lock (configuredInstances)
            {
                if (configuredInstances.ContainsKey(component))
                {
                    return;
                }
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
            {
                configuredInstances.Add(component, configuredInstance);
            }
        }

        public void Configure<T>(Func<T> componentFactory, DependencyLifecycle dependencyLifecycle)
        {
            var pluginType = typeof(T);

            lock (configuredInstances)
            {
                if (configuredInstances.ContainsKey(pluginType))
                {
                    return;
                }
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
            {
                configuredInstances.Add(pluginType, lambdaInstance);
            }
        }

        public void RegisterSingleton(Type lookupType, object instance)
        {
            container.Configure(x => x.For(lookupType)
                .Singleton()
                .Use(instance));
            PluginCache.AddFilledType(lookupType);
        }

        public bool HasComponent(Type componentType)
        {
            return container.Model.PluginTypes.Any(t => t.PluginType == componentType);
        }

        public void Release(object instance)
        {

        }

        static ILifecycle GetLifecycleFrom(DependencyLifecycle dependencyLifecycle)
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

        static IEnumerable<Type> GetAllInterfacesImplementedBy(Type t)
        {
            return t.GetInterfaces().Where(x=>x.FullName != null && !x.FullName.StartsWith("System."));
        }

    }
}
