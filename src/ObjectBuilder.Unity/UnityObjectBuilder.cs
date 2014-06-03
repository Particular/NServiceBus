namespace NServiceBus.ObjectBuilder.Unity
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Common;
    using Microsoft.Practices.Unity;

    class UnityObjectBuilder : IContainer
    {
        IUnityContainer container;

        /// <summary>
        /// Instantiates the class with a new <see cref="UnityContainer"/>.
        /// </summary>
        public UnityObjectBuilder()
            : this(new UnityContainer())
        {
        }

        /// <summary>
        /// Instantiates the class saving the given container.
        /// </summary>
        public UnityObjectBuilder(IUnityContainer container)
        {
            this.container = container;
            
            var propertyInjectionExtension = this.container.Configure<PropertyInjectionContainerExtension>();
            if (propertyInjectionExtension == null)
            {
                this.container.AddNewExtension<PropertyInjectionContainerExtension>();
            }

        }

        public void Dispose()
        {
            //Injected at compile time
        }

        /// <summary>
        /// Returns a child instance of the container to facilitate deterministic disposal
        /// of all resources built by the child container.
        /// </summary>
        public IContainer BuildChildContainer()
        {
            return new UnityObjectBuilder(container.CreateChildContainer());
        }

        public object Build(Type typeToBuild)
        {
            if (!DefaultInstances.Contains(typeToBuild))
            {
                throw new ArgumentException(typeToBuild + " is not registered in the container");
            }

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
            {
                return;
            }

            var interfaces = GetAllServiceTypesFor(concreteComponent);
      
            foreach (var t in interfaces)
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

        public void Configure<T>(Func<T> componentFactory, DependencyLifecycle dependencyLifecycle)
        {
            var componentType = typeof (T);

            if (HasComponent(componentType))
            {
                return;
            }

           var interfaces = GetAllServiceTypesFor(componentType);

           foreach (var t in interfaces)
           {
               if (DefaultInstances.Contains(t))
               {
                   container.RegisterType(t, Guid.NewGuid().ToString(), GetLifetimeManager(dependencyLifecycle), 
                       new InjectionFactory(unityContainer => componentFactory()));
               }
               else
               {
                   container.RegisterType(t, GetLifetimeManager(dependencyLifecycle), new InjectionFactory(unityContainer => componentFactory()));
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

        public void Release(object instance)
        {
            //Not sure if I need to call this or not!
            container.Teardown(instance);
        }

        static IEnumerable<Type> GetAllServiceTypesFor(Type t)
        {
            if (t == null)
            {
                return new List<Type>();
            }

            return new List<Type>(t.GetInterfaces().Where(x => !x.FullName.StartsWith("System.")))
                   {
                       t
                   };
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
}
