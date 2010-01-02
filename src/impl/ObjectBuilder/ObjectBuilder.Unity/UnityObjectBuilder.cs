using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Practices.Unity;
using NServiceBus.ObjectBuilder.Common;

namespace NServiceBus.ObjectBuilder.Unity
{
    public class UnityObjectBuilder : IContainer
    {
        /// <summary>
        /// The container itself.
        /// </summary>
        private readonly IUnityContainer container;

        private readonly HashSet<Type> typesWithDefaultInstances = new HashSet<Type>();

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

        public object Build(Type typeToBuild)
        {
            if (typesWithDefaultInstances.Contains(typeToBuild))
            {
                return container.Resolve(typeToBuild);
            }
            return null;
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

        public void Configure(Type concreteComponent, ComponentCallModelEnum callModel)
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
                        container.RegisterType(t, concreteComponent, Guid.NewGuid().ToString(), GetLifetimeManager(callModel));
                    }
                    else
                    {
                        container.RegisterType(t, concreteComponent, GetLifetimeManager(callModel));
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

        private static LifetimeManager GetLifetimeManager(ComponentCallModelEnum callModel)
        {
            switch (callModel)
            {
                case ComponentCallModelEnum.Singlecall:
                    return new TransientLifetimeManager();
                case ComponentCallModelEnum.Singleton:
                    return new ContainerControlledLifetimeManager();
                default:
                    return new TransientLifetimeManager();
            }

        }
    }
}
