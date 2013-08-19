namespace NServiceBus.ObjectBuilder.Ninject
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using global::Ninject;
    using global::Ninject.Activation;
    using global::Ninject.Infrastructure;
    using global::Ninject.Injection;
    using global::Ninject.Parameters;
    using global::Ninject.Selection;
    using Common;
    using Internal;
    using global::Ninject.Planning.Bindings;

    /// <summary>
    /// Implementation of IBuilderInternal using the Ninject Framework container
    /// </summary>
    public class NinjectObjectBuilder : IContainer
    {
        /// <summary>
        /// The kernel hold by this object builder.
        /// </summary>
        IKernel kernel;

        /// <summary>
        /// The object builders injection propertyHeuristic for properties.
        /// </summary>
        IObjectBuilderPropertyHeuristic propertyHeuristic;

        /// <summary>
        /// Maps the supported <see cref="NServiceBus.DependencyLifecycle"/> to the <see cref="StandardScopeCallbacks"/> of ninject.
        /// </summary>
        IDictionary<DependencyLifecycle, Func<IContext, object>> dependencyLifecycleToScopeMapping =
            new Dictionary<DependencyLifecycle, Func<IContext, object>>
                {
                    { DependencyLifecycle.SingleInstance, StandardScopeCallbacks.Singleton }, 
                    { DependencyLifecycle.InstancePerCall, StandardScopeCallbacks.Transient }, 
                    { DependencyLifecycle.InstancePerUnitOfWork, StandardScopeCallbacks.Transient }, 
                };

        bool disposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="NinjectObjectBuilder"/> class.
        /// </summary>
        public NinjectObjectBuilder()
            : this(new StandardKernel())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="NinjectObjectBuilder"/> class.
        /// </summary>
        /// <remarks>
        /// Uses the default object builder property <see cref="propertyHeuristic"/> 
        /// <see cref="ObjectBuilderPropertyHeuristic"/>.
        /// </remarks>
        /// <param name="kernel">
        /// The kernel.
        /// </param>
        public NinjectObjectBuilder(IKernel kernel)
        {
            this.kernel = kernel;

            RegisterNecessaryBindings();

            propertyHeuristic = this.kernel.Get<IObjectBuilderPropertyHeuristic>();

            AddCustomPropertyInjectionHeuristic();

            this.kernel
                .Bind<NinjectChildContainer>()
                .ToSelf()
                .DefinesNinjectObjectBuilderScope();
        }

        /// <summary>
        /// Builds the specified type.
        /// </summary>
        /// <param name="typeToBuild">
        /// The type to build.
        /// </param>
        /// <returns>
        /// An instance of the given type.
        /// </returns>
        public object Build(Type typeToBuild)
        {
            if (!HasComponent(typeToBuild))
            {
                throw new ArgumentException(typeToBuild + " is not registered in the container");
            }

            return kernel.Get(typeToBuild);
        }

        /// <summary>
        /// Returns a child instance of the container to facilitate deterministic disposal
        /// of all resources built by the child container.
        /// </summary>
        /// <returns>A new child container.</returns>
        public IContainer BuildChildContainer()
        {
            return kernel.Get<NinjectChildContainer>();
        }

        /// <summary>
        /// Returns a list of objects instantiated because their type is compatible with the given type.
        /// </summary>
        /// <param name="typeToBuild">
        /// The type to build.
        /// </param>
        /// <returns>
        /// A list of objects
        /// </returns>
        public IEnumerable<object> BuildAll(Type typeToBuild)
        {
            return kernel.GetAll(typeToBuild);
        }

        /// <summary>
        /// Configures the call model of the given component type.
        /// </summary>
        /// <param name="component">Type to be configured</param>
        /// <param name="dependencyLifecycle">The desired lifecycle for this type</param>
        public void Configure(Type component, DependencyLifecycle dependencyLifecycle)
        {
            if (HasComponent(component))
            {
                return;
            }

            var instanceScope = GetInstanceScopeFrom(dependencyLifecycle);

            var isInstancePerUnitOfWork = dependencyLifecycle == DependencyLifecycle.InstancePerUnitOfWork;
            var bindingConfigurations = BindComponentToItself(component, instanceScope, isInstancePerUnitOfWork);
            AddAliasesOfComponentToBindingConfigurations(component, bindingConfigurations);

            propertyHeuristic.RegisteredTypes.Add(component);
        }

        /// <summary>
        /// Configures the call model of the given component type.
        /// </summary>
        /// <typeparam name="T">Type to be configured</typeparam>
        /// <param name="componentFactory">Factory method that return the type</param>
        /// <param name="dependencyLifecycle">The desired lifecycle for this type</param>
        public void Configure<T>(Func<T> componentFactory, DependencyLifecycle dependencyLifecycle)
        {
            var componentType = typeof (T);

            if (HasComponent(componentType))
            {
                return;
            }

            var instanceScope = GetInstanceScopeFrom(dependencyLifecycle);

            var isInstancePerUnitOfWork = dependencyLifecycle == DependencyLifecycle.InstancePerUnitOfWork;
            var bindingConfigurations = BindComponentToMethod(componentFactory, instanceScope, isInstancePerUnitOfWork);
            AddAliasesOfComponentToBindingConfigurations(componentType, bindingConfigurations);

            propertyHeuristic.RegisteredTypes.Add(componentType);
        }

        /// <summary>
        /// Configures the property.
        /// </summary>
        /// <param name="component">
        /// The component.
        /// </param>
        /// <param name="property">
        /// The property.
        /// </param>
        /// <param name="value">
        /// The value.
        /// </param>
        public void ConfigureProperty(Type component, string property, object value)
        {
            var bindings = kernel.GetBindings(component);

            if (!bindings.Any())
            {
                throw new ArgumentException("Component not registered", "component");
            }

            foreach (var binding in bindings)
            {
                binding.Parameters.Add(new PropertyValue(property, value));
            }
        }

        /// <summary>
        /// Registers the singleton.
        /// </summary>
        /// <param name="lookupType">
        /// Type lookup type.
        /// </param>
        /// <param name="instance">
        /// The instance.
        /// </param>
        public void RegisterSingleton(Type lookupType, object instance)
        {
            if (propertyHeuristic.RegisteredTypes.Contains(lookupType))
            {
                kernel
                    .Rebind(lookupType)
                    .ToConstant(instance);
                return;
            }

            propertyHeuristic
                .RegisteredTypes
                .Add(lookupType);
			
            kernel
                .Bind(lookupType)
                .ToConstant(instance);
        }

        /// <summary>
        /// Determines whether the specified component type has a component.
        /// </summary>
        /// <param name="componentType">
        /// Type of the component.
        /// </param>
        /// <returns>
        /// <c>true</c> if the specified component type has a component; otherwise, <c>false</c>.
        /// </returns>
        public bool HasComponent(Type componentType)
        {
            var request = kernel.CreateRequest(componentType, null, new IParameter[0], false, true);

            return kernel.CanResolve(request);
        }

        public void Release(object instance)
        {
            kernel.Release(instance);
        }

        /// <summary>
        /// Gets all service types of a given component.
        /// </summary>
        /// <param name="component">The component.</param>
        /// <returns>All service types.</returns>
        static IEnumerable<Type> GetAllServiceTypesFor(Type component)
        {
            if (component == null)
            {
                return new List<Type>();
            }

            var result = new List<Type>(component.GetInterfaces()) { component };

            foreach (var interfaceType in component.GetInterfaces())
            {
                result.AddRange(GetAllServiceTypesFor(interfaceType));
            }

            return result.Distinct();
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            if (disposed)
            {
                return;
            }

            disposed = true;
            if (kernel != null)
            {
                if (!kernel.IsDisposed)
                {
                    kernel.Dispose();
                }
            }
        }


        /// <summary>
        /// Gets the instance scope from call model.
        /// </summary>
        /// <param name="dependencyLifecycle">
        /// The call model.
        /// </param>
        /// <returns>
        /// The instance scope
        /// </returns>
        Func<IContext, object> GetInstanceScopeFrom(DependencyLifecycle dependencyLifecycle)
        {
            Func<IContext, object> scope;

            if (!dependencyLifecycleToScopeMapping.TryGetValue(dependencyLifecycle, out scope))
            {
                throw new ArgumentException("The dependency lifecycle is not supported", "dependencyLifecycle");
            }

            return scope;
        }

        /// <summary>
        /// Adds the aliases of component to the binding configurations.
        /// </summary>
        /// <param name="component">The component.</param>
        /// <param name="bindingConfigurations">The binding configurations.</param>
        void AddAliasesOfComponentToBindingConfigurations(Type component, IEnumerable<IBindingConfiguration> bindingConfigurations)
        {
            var services = GetAllServiceTypesFor(component).Where(t => t != component);

            foreach (var service in services)
            {
                foreach (var bindingConfiguration in bindingConfigurations)
                {
                    kernel.AddBinding(new Binding(service, bindingConfiguration));
                }
            }
        }

        /// <summary>
        /// Binds the component to itself with the given <paramref name="instanceScope"/>.
        /// </summary>
        /// <param name="component">The component.</param>
        /// <param name="instanceScope">The instance scope.</param>
        /// <param name="addChildContainerScope">if set to <c>true</c> an additional binding scoped to the child container is added.</param>
        /// <returns>The created binding configurations.</returns>
        IEnumerable<IBindingConfiguration> BindComponentToItself(Type component, Func<IContext, object> instanceScope, bool addChildContainerScope)
        {
            var bindingConfigurations = new List<IBindingConfiguration>();
            if (addChildContainerScope)
            {
                var instanceScopeConfiguration = kernel
                    .Bind(component)
                    .ToSelf()
                    .WhenNotInUnitOfWork()
                    .InScope(instanceScope)
                    .BindingConfiguration;
                bindingConfigurations.Add(instanceScopeConfiguration);

                var unitOfWorkConfiguration = kernel
                    .Bind(component)
                    .ToSelf()
                    .WhenInUnitOfWork()
                    .InUnitOfWorkScope()
                    .BindingConfiguration;
                bindingConfigurations.Add(unitOfWorkConfiguration);
            }
            else
            {
                var instanceScopeConfiguration = kernel
                    .Bind(component)
                    .ToSelf()
                    .InScope(instanceScope)
                    .BindingConfiguration;
                bindingConfigurations.Add(instanceScopeConfiguration);
            }

            return bindingConfigurations;
        }

        IEnumerable<IBindingConfiguration> BindComponentToMethod<T>(Func<T> component, Func<IContext, object> instanceScope, bool addChildContainerScope)
        {
            var bindingConfigurations = new List<IBindingConfiguration>();
            if (addChildContainerScope)
            {
                var instanceScopeConfiguration = kernel
                    .Bind<T>()
                    .ToMethod(context => component.Invoke())
                    .WhenNotInUnitOfWork()
                    .InScope(instanceScope)
                    .BindingConfiguration;
                bindingConfigurations.Add(instanceScopeConfiguration);

                var unitOfWorkConfiguration = kernel
                    .Bind<T>()
                    .ToMethod(context => component.Invoke())
                    .WhenInUnitOfWork()
                    .InUnitOfWorkScope()
                    .BindingConfiguration;
                bindingConfigurations.Add(unitOfWorkConfiguration);
            }
            else
            {
                var instanceScopeConfiguration = kernel
                    .Bind<T>()
                    .ToMethod(context => component.Invoke())
                    .InScope(instanceScope)
                    .BindingConfiguration;
                bindingConfigurations.Add(instanceScopeConfiguration);
            }

            return bindingConfigurations;
        }

        /// <summary>
        /// Adds the custom property injection heuristic.
        /// </summary>
        void AddCustomPropertyInjectionHeuristic()
        {
            var selector = kernel.Components.Get<ISelector>();

            selector.InjectionHeuristics.Add(
                kernel.Get<IObjectBuilderPropertyHeuristic>());
        }

        /// <summary>
        /// Registers the necessary bindings.
        /// </summary>
        void RegisterNecessaryBindings()
        {
            kernel
                .Bind<IContainer>()
                .ToConstant(this)
                .InSingletonScope();

            kernel
                .Bind<IObjectBuilderPropertyHeuristic>()
                .To<ObjectBuilderPropertyHeuristic>()
                .InSingletonScope()
                .WithPropertyValue("Settings", context => context.Kernel.Settings);

            kernel
                .Bind<IInjectorFactory>()
                .ToMethod(context => context.Kernel.Components.Get<IInjectorFactory>());
        }
    }
}
