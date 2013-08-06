

namespace NServiceBus.ObjectBuilder.Spring
{
    using System;
    using System.Collections.Generic;
    using global::Spring.Context.Support;
    using Common;
    using System.Threading;
    using global::Spring.Objects.Factory.Config;
    using global::Spring.Objects.Factory.Support;

    /// <summary>
    /// Implementation of <see cref="IContainer"/> using the Spring Framework container
    /// </summary>
    public class SpringObjectBuilder : IContainer
    {
        private static GenericApplicationContext context;

        /// <summary>
        /// Instantiates the builder using a new <see cref="GenericApplicationContext"/>.
        /// </summary>
        public SpringObjectBuilder() : this(new GenericApplicationContext())
        {
        }

        /// <summary>
        /// Instantiates the builder using the given container.
        /// </summary>
        public SpringObjectBuilder(GenericApplicationContext container)
        {
            context = container;
        }

        /// <summary>
        /// No resources to dispose.
        /// </summary>
        public void Dispose()
        {
            //This is to figure out if Dispose was called from a child container or not
            if (scope.IsValueCreated)
            {
                return;
            }

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
                // Dispose managed resources.
                context.Dispose();
            }

        }

        ~SpringObjectBuilder()
        {
            Dispose(false);
        }

        IContainer IContainer.BuildChildContainer()
        {
            //return this until we can get the child containers to work properly
            //return new SpringObjectBuilder(new GenericApplicationContext(context));
            
            scope.Value = true;

            return this;
        }

        object IContainer.Build(Type typeToBuild)
        {
            Init();
            var dict = context.GetObjectsOfType(typeToBuild, true, false);

            var de = dict.GetEnumerator();

            if (de.MoveNext())
            {
                return de.Value;
            }
            
            throw new ArgumentException(string.Format("{0} has not been configured. In order to avoid this exception, check the return value of the 'HasComponent' method for this type.", typeToBuild));
        }

        IEnumerable<object> IContainer.BuildAll(Type typeToBuild)
        {
            Init();
            var dict = context.GetObjectsOfType(typeToBuild, true, false);

            var de = dict.GetEnumerator();
            while (de.MoveNext())
            {
                yield return de.Entry.Value;
            }
        }

        void IContainer.Configure(Type concreteComponent, DependencyLifecycle dependencyLifecycle)
        {
            if (initialized)
            {
                throw new InvalidOperationException("You can't alter the registrations after the container components has been resolved from the container");
            }

            typeHandleLookup[concreteComponent] = dependencyLifecycle;

            lock (componentProperties)
            {
                if (!componentProperties.ContainsKey(concreteComponent))
                {
                    componentProperties[concreteComponent] = new ComponentConfig();
                }
            }
        }

        void IContainer.Configure<T>(Func<T> componentFactory, DependencyLifecycle dependencyLifecycle)
        {
            var componentType = typeof(T);

            if (((IContainer)this).HasComponent(componentType))
            {
                return;
            }

            var funcFactory = new ArbitraryFuncDelegatingFactoryObject<T>(componentFactory, dependencyLifecycle == DependencyLifecycle.SingleInstance);
            
            context.ObjectFactory.RegisterSingleton(componentType.FullName, funcFactory);
        }

        void IContainer.ConfigureProperty(Type concreteComponent, string property, object value)
        {
            if (initialized)
            {
                throw new InvalidOperationException("You can't alter the registrations after the container components has been resolved from the container");
            }

            lock (componentProperties)
            {
                ComponentConfig result;
                componentProperties.TryGetValue(concreteComponent, out result);

                if (result == null)
                {
                    throw new InvalidOperationException("Cannot configure property before the component has been configured. Please call 'Configure' first.");
                }

                result.ConfigureProperty(property, value);
            }
        }

        void IContainer.RegisterSingleton(Type lookupType, object instance)
        {
            if (initialized)
            {
                throw new InvalidOperationException("You can't alter the registrations after the container components has been resolved from the container");
            }

            context.ObjectFactory.RegisterSingleton(lookupType.FullName, instance);
        }

        bool IContainer.HasComponent(Type componentType)
        {
            if (componentProperties.ContainsKey(componentType))
            {
                return true;
            }

            if (context.ObjectFactory.ContainsObjectDefinition(componentType.FullName))
            {
                return true;
            }

            if (context.ObjectFactory.ContainsSingleton(componentType.FullName))
            {
                return true;
            }

            foreach (var component in componentProperties.Keys)
            {
                if (componentType.IsAssignableFrom(component))
                {
                    return true;
                }
            }

            return false;
        }

        void IContainer.Release(object instance)
        {
        }

        void Init()
        {
            if (initialized)
            {
                return;
            }

            lock (componentProperties)
            {
                foreach (var t in componentProperties.Keys)
                {
                    var builder = ObjectDefinitionBuilder.RootObjectDefinition(factory, t)
                        .SetAutowireMode(AutoWiringMode.AutoDetect)
                        .SetSingleton(typeHandleLookup[t] == DependencyLifecycle.SingleInstance);                    

                    componentProperties[t].Configure(builder);

                    IObjectDefinition def = builder.ObjectDefinition;
                    context.RegisterObjectDefinition(t.FullName, def);
                }
            }

            initialized = true;
            context.Refresh();
        }

        ThreadLocal<bool> scope = new ThreadLocal<bool>();
        Dictionary<Type, DependencyLifecycle> typeHandleLookup = new Dictionary<Type, DependencyLifecycle>();
        Dictionary<Type, ComponentConfig> componentProperties = new Dictionary<Type, ComponentConfig>();
        bool initialized;
        bool disposed;
        DefaultObjectDefinitionFactory factory = new DefaultObjectDefinitionFactory();
    }
}
