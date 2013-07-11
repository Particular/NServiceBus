using System;
using System.Collections.Generic;
using System.Linq;
using Castle.Core.Resource;
using Castle.MicroKernel.Lifestyle;
using Castle.MicroKernel.SubSystems.Configuration;
using Castle.Windsor.Configuration.Interpreters;
using NServiceBus.ObjectBuilder.Common;
using Castle.Windsor;
using Castle.MicroKernel;
using Castle.Core;
using Castle.MicroKernel.Registration;

namespace NServiceBus.ObjectBuilder.CastleWindsor
{
    using System.Threading;
    using Castle.MicroKernel.ComponentActivator;
    using Castle.MicroKernel.Context;
    using Logging;

    /// <summary>
    /// Castle Windsor implementation of IContainer.
    /// </summary>
    public class WindsorObjectBuilder : IContainer
    {
        /// <summary>
        /// The container itself.
        /// </summary>
        private IWindsorContainer container { get; set; }

        private bool disposed;

        /// <summary>
        /// Instantiates the class with a new WindsorContainer.
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
            if (!disposed && scope.IsValueCreated && scope.Value != null)
            {
                scope.Value.Dispose();
                scope.Value = null;
                return;
            }

            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~WindsorObjectBuilder()
        {
            Dispose(false);
        }

        /// <summary>
        /// Implements dispose
        /// </summary>
        /// <param name="disposing"></param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposed)
            {
                return;
            }

            if (disposing)
            {
                container.Dispose();
                scope.Dispose();
            }

            disposed = true;
        }

        /// <summary>
        /// Returns a child instance of the container to facilitate deterministic disposal
        /// of all resources built by the child container.
        /// </summary>
        /// <returns></returns>
        public IContainer BuildChildContainer()
        {
            scope.Value = container.Kernel.BeginScope();
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
                    new DependencyModel(property, value.GetType(), false, true, value )));
        }

        void IContainer.RegisterSingleton(Type lookupType, object instance)
        {
            var registration = container.Kernel.GetAssignableHandlers(lookupType).Select(x => x.ComponentModel).FirstOrDefault();

            if (registration != null)
            {
                registration.ExtendedProperties["instance"] = instance;
                return;
            }

            var services = GetAllServiceTypesFor( instance.GetType() ).Union( new[] { lookupType } );

            container.Register(Component.For(services).Activator<ExternalInstanceActivatorWithDecommissionConcern>().Instance(instance).LifestyleSingleton());
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

        void IContainer.Release(object instance)
        {
            container.Release(instance);
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

        readonly ThreadLocal<IDisposable> scope = new ThreadLocal<IDisposable>();
       
        private static readonly ILog Logger = LogManager.GetLogger(typeof(WindsorObjectBuilder));
    }

    class ExternalInstanceActivatorWithDecommissionConcern : AbstractComponentActivator, IDependencyAwareActivator
    {
        public ExternalInstanceActivatorWithDecommissionConcern(ComponentModel model, IKernelInternal kernel, ComponentInstanceDelegate onCreation, ComponentInstanceDelegate onDestruction)
            : base(model, kernel, onCreation, onDestruction)
        {
        }

        public bool CanProvideRequiredDependencies(ComponentModel component)
        {
            //we already have an instance so we don't need to provide any dependencies at all
            return true;
        }

        public bool IsManagedExternally(ComponentModel component)
        {
            return false;
        }

        protected override object InternalCreate(CreationContext context)
        {
            return Model.ExtendedProperties["instance"];
        }

        protected override void InternalDestroy(object instance)
        {
            ApplyDecommissionConcerns(instance);
        }
    }

    internal class NoOpInterpreter : AbstractInterpreter
    {
        public override void ProcessResource(IResource resource, IConfigurationStore store, IKernel kernel)
        {

        }
    }
}
