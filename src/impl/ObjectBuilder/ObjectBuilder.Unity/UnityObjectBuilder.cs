﻿using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Practices.Unity;

namespace NServiceBus.ObjectBuilder.Unity
{
    using IContainer = Common.IContainer;

    public class UnityObjectBuilder : IContainer
    {
        /// <summary>
        /// The container itself.
        /// </summary>
        private readonly IUnityContainer container;


        private bool disposed;

        /// <summary>
        /// Instantites the class with a new UnityContainer.
        /// </summary>
        public UnityObjectBuilder()
            : this(new UnityContainer())
        {
        }

        /// <summary>
        /// Instantiates the class saving the given container.
        /// </summary>
        /// <param name="container"></param>
        public UnityObjectBuilder(IUnityContainer container)
        {
            this.container = container;
            
            var propertyInjectionExtension = this.container.Configure<PropertyInjectionContainerExtension>();
            if (propertyInjectionExtension == null)
            {
                this.container.AddNewExtension<PropertyInjectionContainerExtension>();
            }

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
            return new UnityObjectBuilder(container.CreateChildContainer());
        }

        public object Build(Type typeToBuild)
        {
            if (!DefaultInstances.Contains(typeToBuild))
                throw new ArgumentException(typeToBuild + " is not registered in the container");

            return container.Resolve(typeToBuild);
        }

        public IEnumerable<object> BuildAll(Type typeToBuild)
        {
            if (DefaultInstances.Contains(typeToBuild))
            {
                yield return container.Resolve(typeToBuild);
                foreach (var component in container.ResolveAll(typeToBuild))
                {
                    yield return component;
                }
            }
        }

        public void Configure(Type concreteComponent, DependencyLifecycle dependencyLifecycle)
        {
            if (HasComponent(concreteComponent))
                return;

            IEnumerable<Type> interfaces = GetAllServiceTypesFor(concreteComponent);

      
            foreach (Type t in interfaces)
            {
                if (DefaultInstances.Contains(t))
                {
                    container.RegisterType(t, concreteComponent, Guid.NewGuid().ToString(), GetLifetimeManager(dependencyLifecycle));
                }
                else
                {
                    container.RegisterType(t, concreteComponent, GetLifetimeManager(dependencyLifecycle));
                    DefaultInstances.Add(t);
                }
            }

        }

        public void ConfigureProperty(Type concreteComponent, string property, object value)
        {
            PropertyInjectionBuilderStrategy.SetPropertyValue(concreteComponent, property, value);
        }

        public void RegisterSingleton(Type lookupType, object instance)
        {
            DefaultInstances.Add(lookupType);
            container.RegisterInstance(lookupType, instance);
        }

        public bool HasComponent(Type componentType)
        {
            return container.IsRegistered(componentType);
        }

        private static IEnumerable<Type> GetAllServiceTypesFor(Type t)
        {
            if (t == null)
            {
                return new List<Type>();
            }

            var result = new List<Type>(t.GetInterfaces().Where(x => x.FullName != null && !x.FullName.StartsWith("System.")));
            result.Add(t);
            return result;
        }

        private static LifetimeManager GetLifetimeManager(DependencyLifecycle dependencyLifecycle)
        {
            switch (dependencyLifecycle)
            {
                case DependencyLifecycle.InstancePerCall:
                    return new TransientLifetimeManager();
                case DependencyLifecycle.SingleInstance:
                    return new ContainerControlledLifetimeManager();
                case DependencyLifecycle.InstancePerUnitOfWork:
                    return new HierarchicalLifetimeManager();
            }
            throw new ArgumentException("Unhandled lifecycle - " + dependencyLifecycle);
        }
    }

    public static class DefaultInstances
    {
        static readonly HashSet<Type> typesWithDefaultInstances = new HashSet<Type>();

        public static bool Contains(Type type)
        {
            return typesWithDefaultInstances.Contains(type);
        }

        public static void Add(Type type)
        {
            typesWithDefaultInstances.Add(type);
        }

        public static void Clear()
        {
            typesWithDefaultInstances.Clear();
        }
    }
}
