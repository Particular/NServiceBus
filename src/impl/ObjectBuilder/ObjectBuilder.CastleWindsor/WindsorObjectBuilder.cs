namespace NServiceBus.ObjectBuilder.CastleWindsor
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Castle.MicroKernel.Lifestyle;
    using Common;
    using Castle.Windsor;
    using Castle.Core;
    using Castle.MicroKernel.Registration;
    using Logging;

    /// <summary>
    /// Castle Windsor implementation of IContainer.
    /// </summary>
    public class WindsorObjectBuilder : IContainer
    {
        IWindsorContainer container;
        IDisposable scope;
        static ILog Logger = LogManager.GetLogger(typeof(WindsorObjectBuilder));

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
            {
                throw new ArgumentNullException("container", "The object builder must be initialized with a valid windsor container");
            }

            this.container = container;
        }

        public void Dispose()
        {
            //Injected at compile time
        }

        void DisposeManaged()
        {
            //if we are in a child scope dispose of that but not the parent container
            if (scope != null)
            {
                scope.Dispose();
                return;
            }
            if (container != null)
            {
                container.Dispose();
            }
        }


        /// <summary>
        /// Returns a child instance of the container to facilitate deterministic disposal
        /// of all resources built by the child container.
        /// </summary>
        public IContainer BuildChildContainer()
        {
            return new WindsorObjectBuilder(container)
                                {
                                    scope = container.Kernel.BeginScope()
                                };
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

            if (registration == null)
            {
                var message = "Cannot configure property for a type which hadn't been configured yet. Please call 'Configure' first.";
                throw new InvalidOperationException(message);
            }

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

        static LifestyleType GetLifestyleTypeFrom(DependencyLifecycle dependencyLifecycle)
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

  
    }
}
