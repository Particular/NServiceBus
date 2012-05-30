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

        /// <summary>
        /// Implements dispose
        /// </summary>
        /// <param name="disposing"></param>
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
            var registrations = container.Kernel.GetAssignableHandlers(concreteComponent).Select(x=>x.ComponentModel);

            if (registrations.Any())
            {
                 Logger.Info("Component " + concreteComponent.FullName + " was already registered in the container.");
                return;
            }

            var lifestyle = GetLifestyleTypeFrom(dependencyLifecycle);
            var services = GetAllServiceTypesFor(concreteComponent);

            container.Register(Component.For(services).ImplementedBy(concreteComponent).LifeStyle.Is(lifestyle));            
        }

        void IContainer.Configure<T>(Func<T> componentFactory, DependencyLifecycle dependencyLifecycle)
        {
            var componentType = typeof (T);
            var registrations = container.Kernel.GetAssignableHandlers(componentType).Select(x => x.ComponentModel);

            if (registrations.Any())
            {
                Logger.Info("Component " + componentType.FullName + " was already registered in the container.");
                return;
            }

            var lifestyle = GetLifestyleTypeFrom(dependencyLifecycle);
            var services = GetAllServiceTypesFor(componentType);

            container.Register(Component.For(services).UsingFactoryMethod(componentFactory).LifeStyle.Is(lifestyle));
        }

        void IContainer.ConfigureProperty(Type component, string property, object value)
        {
            var registration = container.Kernel.GetAssignableHandlers(component).Select(x => x.ComponentModel).SingleOrDefault();

            if (registration==null)
                throw new InvalidOperationException("Cannot configure property for a type which hadn't been configured yet. Please call 'Configure' first.");

            var propertyInfo = component.GetProperty(property);

            registration.AddProperty(
                new PropertySet(propertyInfo, 
                    new DependencyModel(property, propertyInfo.PropertyType, false, true, value )));
        }

        void IContainer.RegisterSingleton(Type lookupType, object instance)
        {
            var registration = container.Kernel.GetAssignableHandlers(lookupType).Select(x => x.ComponentModel).FirstOrDefault();

            if (registration != null)
            {
                registration.ExtendedProperties["instance"] = instance;
                return;
            }

            container.Register(Component.For(lookupType).Named(lookupType.AssemblyQualifiedName).Instance(instance));
        }

        object IContainer.Build(Type typeToBuild)
        {
            return container.Resolve(typeToBuild);
        }

        IEnumerable<object> IContainer.BuildAll(Type typeToBuild)
        {
            return container.ResolveAll(typeToBuild).Cast<object>();
        }


        bool IContainer.HasComponent(Type componentType)
        {
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

        static IEnumerable<Type> GetAllServiceTypesFor(Type t)
        {

            return t.GetInterfaces()
                .Where(x => x.FullName != null && !x.FullName.StartsWith("System."))
                .Concat(new[] {t});
        }
        

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
