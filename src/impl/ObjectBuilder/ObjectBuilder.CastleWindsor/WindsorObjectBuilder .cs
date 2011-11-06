using System;
using System.Collections.Generic;
using System.Linq;
using Castle.Core.Resource;
using Castle.MicroKernel.Lifestyle;
using Castle.MicroKernel.SubSystems.Configuration;
using Castle.Windsor.Configuration.Interpreters;
using log4net;
using NServiceBus.ObjectBuilder.Common;
using Castle.Windsor;
using Castle.MicroKernel;
using Castle.Core;
using Castle.MicroKernel.Registration;

namespace NServiceBus.ObjectBuilder.CastleWindsor
{
    using Castle.Core.Internal;

    /// <summary>
    /// Castle Windsor implementaton of IContainer.
    /// </summary>
    public class WindsorObjectBuilder : IContainer
    {
        /// <summary>
        /// The container itself.
        /// </summary>
        private IWindsorContainer container { get; set; }

        private bool disposed;

        /// <summary>
        /// Instantites the class with a new WindsorContainer.
        /// </summary>
        public WindsorObjectBuilder()
            : this(new WindsorContainer())
        {
        }

        /// <summary>
        /// Instantiates the class saving the given container.
        /// </summary>
        /// <param name="container"></param>
        public WindsorObjectBuilder(IWindsorContainer container)
        {
            if (container == null)
                throw new ArgumentNullException("container", "The object builder must be initialized with a valid windsor container");

            this.container = container;
        }

        /// <summary>
        /// Disposes the container and all resources instantiated by the container.
        /// </summary>
        public void Dispose()
        {
            if (disposableScope != null)
            {
                disposableScope.Dispose();
                disposableScope = null;
                return;
            }

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
            disposableScope = container.Kernel.BeginScope();
            return this;
        }

        void IContainer.Configure(Type concreteComponent, DependencyLifecycle dependencyLifecycle)
        {
            var reg = GetRegistrationForType(concreteComponent);
            if (reg == null)
            {
                var lifestyle = GetLifestyleTypeFrom(dependencyLifecycle);

                var services = GetAllServiceTypesFor(concreteComponent);
                reg = Component.For(services).ImplementedBy(concreteComponent);
                reg.LifeStyle.Is(lifestyle);

                lock (lookup)
                {
                    lookup[concreteComponent] = reg;
                    services.ForEach(s => types.Add(s));
                }
            }
            else
                Logger.Info("Component " + concreteComponent.FullName + " was already registered in the container.");
        }

        void IContainer.ConfigureProperty(Type component, string property, object value)
        {
            var reg = GetRegistrationForType(component);
            if (reg == null)
                throw new InvalidOperationException("Cannot configure property for a type which hadn't been configured yet. Please call 'Configure' first.");

            reg.DependsOn(Property.ForKey(property).Eq(value));
        }

        void IContainer.RegisterSingleton(Type lookupType, object instance)
        {
            container.Register(Component.For(lookupType).Named(lookupType.Name).Instance(instance));
        }

        object IContainer.Build(Type typeToBuild)
        {
            Initialize();

            return container.Resolve(typeToBuild);
        }

        IEnumerable<object> IContainer.BuildAll(Type typeToBuild)
        {
            Initialize();

            return container.ResolveAll(typeToBuild).Cast<object>();
        }

        private void Initialize()
        {
            if (!initialized)
                lock (lookup)
                {
                    if (!initialized)
                        container.Register(lookup.Values.ToArray());

                    initialized = true;
                }
        }

        bool IContainer.HasComponent(Type componentType)
        {
            if (types.Contains(componentType))
                return true;

            return container.Kernel.HasComponent(componentType);
        }

        private static LifestyleType GetLifestyleTypeFrom(DependencyLifecycle dependencyLifecycle)
        {
            switch (dependencyLifecycle)
            {
                case DependencyLifecycle.InstancePerCall:
                    return LifestyleType.Transient;
                case DependencyLifecycle.SingleInstance:
                    return LifestyleType.Singleton;
                case DependencyLifecycle.InstancePerUnitOfWork:
                    return LifestyleType.Scoped;
            }

            throw new ArgumentException("Unhandled lifecycle - " + dependencyLifecycle);
        }

        private static IEnumerable<Type> GetAllServiceTypesFor(Type t)
        {
            if (t == null)
                return new List<Type>();

            var result = new List<Type>(t.GetInterfaces()) { t };

            foreach (var interfaceType in t.GetInterfaces())
                result.AddRange(GetAllServiceTypesFor(interfaceType));

            return result;
        }

        private ComponentRegistration<object> GetRegistrationForType(Type concreteComponent)
        {
            ComponentRegistration<object> output;

            lock (lookup)
                lookup.TryGetValue(concreteComponent, out output);

            return output;
        }


        private bool initialized;
        private IDictionary<Type, ComponentRegistration<object>> lookup = new Dictionary<Type, ComponentRegistration<object>>();
        private IList<Type> types = new List<Type>();

        [ThreadStatic]
        private static IDisposable disposableScope;

        private static ILog Logger = LogManager.GetLogger("NServiceBus.ObjectBuilder");
    }

    internal class NoOpInterpreter : AbstractInterpreter
    {
        public override void ProcessResource(IResource resource, IConfigurationStore store, IKernel kernel)
        {

        }
    }
}
