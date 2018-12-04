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
        public AcceptanceTestingContainer()
        {
            builder = new LightInjectObjectBuilder();
        }

        public void Dispose()
        {
            builder.Dispose();
        }

        public object Build(Type typeToBuild)
        {
            locked = true;
            return builder.Build(typeToBuild);
        }

        public IContainer BuildChildContainer()
        {
            return builder.BuildChildContainer();
        }

        public IEnumerable<object> BuildAll(Type typeToBuild)
        {
            locked = true;
            return builder.BuildAll(typeToBuild);
        }

        public void Configure(Type component, DependencyLifecycle dependencyLifecycle)
        {
            ThrowIfLocked();
            builder.Configure(component, dependencyLifecycle);
        }

        public void Configure<T>(Func<T> component, DependencyLifecycle dependencyLifecycle)
        {
            ThrowIfLocked();
            builder.Configure(component, dependencyLifecycle);
        }

        public void RegisterSingleton(Type lookupType, object instance)
        {
            ThrowIfLocked();
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
    }
}