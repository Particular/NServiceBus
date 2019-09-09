namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using ObjectBuilder.Common;

    /// <summary>
    /// Container that enforces registration immutability once the first instance has been resolved.
    /// </summary>
    public class AcceptanceTestingContainer : IContainer
    {
        public Dictionary<Type, Component> RegisteredComponents { get; } = new Dictionary<Type, Component>();

        public bool WasDisposed { get; private set; }

        public AcceptanceTestingContainer()
        {
            builder = new LightInjectObjectBuilder();
        }

        public void Dispose()
        {
            builder.Dispose();
            WasDisposed = true;
        }

        public object Build(Type typeToBuild)
        {
            if (RegisteredComponents.TryGetValue(typeToBuild, out var component))
            {
                component.WasResolved = true;
            }

            locked = true;
            return builder.Build(typeToBuild);
        }

        public IContainer BuildChildContainer()
        {
            return builder.BuildChildContainer();
        }

        public IEnumerable<object> BuildAll(Type typeToBuild)
        {
            if (RegisteredComponents.TryGetValue(typeToBuild, out var component))
            {
                component.WasResolved = true;
            }

            locked = true;
            return builder.BuildAll(typeToBuild);
        }

        public void Configure(Type component, DependencyLifecycle dependencyLifecycle)
        {
            ThrowIfLocked();

            RegisteredComponents[component] = new Component(component, dependencyLifecycle);

            builder.Configure(component, dependencyLifecycle);
        }

        public void Configure<T>(Func<T> component, DependencyLifecycle dependencyLifecycle)
        {
            ThrowIfLocked();

            RegisteredComponents[typeof(T)] = new Component(typeof(T), dependencyLifecycle);

            builder.Configure(component, dependencyLifecycle);
        }

        public void RegisterSingleton(Type lookupType, object instance)
        {
            ThrowIfLocked();

            RegisteredComponents[lookupType] = new Component(lookupType, DependencyLifecycle.SingleInstance);

            builder.RegisterSingleton(lookupType, instance);
        }

        public bool HasComponent(Type componentType)
        {
            return builder.HasComponent(componentType);
        }

        public void Release(object instance)
        {
            builder.Release(instance);
        }

        void ThrowIfLocked()
        {
            if (locked)
            {
                throw new InvalidOperationException("Can't register components after the container has been used to resolve instances");
            }
        }

        LightInjectObjectBuilder builder;
        bool locked;

        public class Component
        {
            public Type Type { get; }

            public DependencyLifecycle Lifecycle { get; }

            public bool WasResolved { get; set; }

            public Component(Type type, DependencyLifecycle dependencyLifecycle)
            {
                Type = type;
                Lifecycle = dependencyLifecycle;
            }

            public override string ToString()
            {
                return $"{Type.FullName} - {Lifecycle}";
            }
        }
    }
}