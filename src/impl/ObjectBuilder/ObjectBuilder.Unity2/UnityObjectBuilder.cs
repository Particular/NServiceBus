using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Practices.Unity;
using NServiceBus.ObjectBuilder.Common;

namespace NServiceBus.ObjectBuilder.Unity2
{
    public class UnityObjectBuilder : IContainer
    {
        /// <summary>
        /// The container itself.
        /// </summary>
        private readonly IUnityContainer container;

        private readonly HashSet<Type> typesWithDefaultInstances = new HashSet<Type>();

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
            var autowireContainerExtension = this.container.Configure<FullAutowireContainerExtension>();
            if (autowireContainerExtension == null)
            {
                this.container.AddNewExtension<FullAutowireContainerExtension>();
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
            if (!typesWithDefaultInstances.Contains(typeToBuild))
                throw new ArgumentException(typeToBuild + " is not registered in the container");

            return container.Resolve(typeToBuild);
        }

        public IEnumerable<object> BuildAll(Type typeToBuild)
        {
            if (typesWithDefaultInstances.Contains(typeToBuild))
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
            ConfigureComponentAdapter config =
               container.ResolveAll<ConfigureComponentAdapter>().Where(x => x.ConfiguredType == concreteComponent).
                  FirstOrDefault();
            if (config == null)
            {
                IEnumerable<Type> interfaces = GetAllServiceTypesFor(concreteComponent);
                config = new ConfigureComponentAdapter(container, concreteComponent);
                container.RegisterInstance(Guid.NewGuid().ToString(), config);

                foreach (Type t in interfaces)
                {
                    if (typesWithDefaultInstances.Contains(t))
                    {
                        container.RegisterType(t, concreteComponent, Guid.NewGuid().ToString(), GetLifetimeManager(dependencyLifecycle));
                    }
                    else
                    {
                        container.RegisterType(t, concreteComponent, GetLifetimeManager(dependencyLifecycle));
                        typesWithDefaultInstances.Add(t);
                    }
                }
            }
        }

        public void ConfigureProperty(Type concreteComponent, string property, object value)
        {
            ConfigureComponentAdapter config =
               container.ResolveAll<ConfigureComponentAdapter>().Where(x => x.ConfiguredType == concreteComponent).
                  First();
            config.ConfigureProperty(property, value);
        }

        public void RegisterSingleton(Type lookupType, object instance)
        {
            container.RegisterInstance(lookupType, instance);
        }

        public bool HasComponent(Type componentType)
        {
            return container.ResolveAll<ConfigureComponentAdapter>().Any(x => x.ConfiguredType == componentType);
        }

        private static IEnumerable<Type> GetAllServiceTypesFor(Type t)
        {
            if (t == null)
            {
                return new List<Type>();
            }

            var result = new List<Type>(t.GetInterfaces());
            result.Add(t);

            foreach (Type interfaceType in t.GetInterfaces())
            {
                result.AddRange(GetAllServiceTypesFor(interfaceType));
            }

            return result.Distinct();
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
                    return new TransientLifetimeManager();
            }
            throw new ArgumentException("Unhandled lifecycle - " + dependencyLifecycle);
        }
    }
}
