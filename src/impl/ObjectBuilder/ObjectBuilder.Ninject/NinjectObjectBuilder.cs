﻿using System;
using System.Collections.Generic;
using System.Linq;
using Ninject;
using Ninject.Activation;
using Ninject.Activation.Strategies;
using Ninject.Infrastructure;
using Ninject.Injection;
using Ninject.Parameters;
using Ninject.Selection;
using NServiceBus.ObjectBuilder.Common;
using Ninject.Extensions.ChildKernel;
using NServiceBus.ObjectBuilder.Ninject.Internal;

namespace NServiceBus.ObjectBuilder.Ninject
{

    /// <summary>
    /// Implementation of IBuilderInternal using the Ninject Framework container
    /// </summary>
    public class NinjectObjectBuilder : IContainer
    {
        /// <summary>
        /// The kernel hold by this object builder.
        /// </summary>
        private readonly IKernel kernel;

        /// <summary>
        /// The object builders injection propertyHeuristic for properties.
        /// </summary>
        private readonly IObjectBuilderPropertyHeuristic propertyHeuristic;

        /// <summary>
        /// Maps the supported <see cref="DependencyLifecycle"/> to the <see cref="StandardScopeCallbacks"/> of ninject.
        /// </summary>
        private readonly IDictionary<DependencyLifecycle, Func<IContext, object>> dependencyLifecycleToScopeMapping =
            new Dictionary<DependencyLifecycle, Func<IContext, object>>
                {
                    { DependencyLifecycle.SingleInstance, StandardScopeCallbacks.Singleton }, 
                    { DependencyLifecycle.InstancePerCall, StandardScopeCallbacks.Transient }, 
                    { DependencyLifecycle.InstancePerUnitOfWork, StandardScopeCallbacks.Transient }, 
                };

        private bool disposed;

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

            this.RegisterNecessaryBindings();

            this.propertyHeuristic = this.kernel.Get<IObjectBuilderPropertyHeuristic>();

            this.AddCustomPropertyInjectionHeuristic();

            this.ReplacePropertyInjectionStrategyWithCustomPropertyInjectionStrategy();
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
            if (!this.HasComponent(typeToBuild))
            {
                throw new ArgumentException(typeToBuild + " is not registered in the container");
            }

            var output = this.kernel.Get(typeToBuild);

            return output;
        }

        /// <summary>
        /// Returns a child instance of the container to facilitate deterministic disposal
        /// of all resources built by the child container.
        /// </summary>
        /// <returns>A new child container.</returns>
        public IContainer BuildChildContainer()
        {
            return new NinjectObjectBuilder(new ChildKernel(this.kernel));
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
            var output = this.kernel.GetAll(typeToBuild);

            return output;
        }

        /// <summary>
        /// Configures the call model of the given component type.
        /// </summary>
        /// <param name="component">Type to be configured</param>
        /// <param name="dependencyLifecycle">The desired lifecycle for this type</param>
        public void Configure(Type component, DependencyLifecycle dependencyLifecycle)
        {
            if (this.HasComponent(component))
            {
                return;
            }

            var instanceScope = this.GetInstanceScopeFrom(dependencyLifecycle);

            this.BindComponentToItself(component, instanceScope);

            this.BindAliasesOfComponentToComponent(component, instanceScope);

            this.propertyHeuristic.RegisteredTypes.Add(component);
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
            var bindings = this.kernel.GetBindings(component);

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
            this.kernel.Bind(lookupType).ToConstant(instance);
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
            var bindings = this.kernel.GetBindings(componentType);
            return bindings.Any();
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            this.Dispose(true);

            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Gets all service types of a given component.
        /// </summary>
        /// <param name="component">The component.</param>
        /// <returns>All service types.</returns>
        private static IEnumerable<Type> GetAllServiceTypesFor(Type component)
        {
            if (component == null)
            {
                return new List<Type>();
            }

            var result = new List<Type>(component.GetInterfaces()) { component };

            foreach (Type interfaceType in component.GetInterfaces())
            {
                result.AddRange(GetAllServiceTypesFor(interfaceType));
            }

            return result.Distinct();
        }

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources
        /// </summary>
        /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        private void Dispose(bool disposing)
        {
            if (!this.disposed)
            {
                if (disposing)
                {
                    if (!this.kernel.IsDisposed)
                    {
                        this.kernel.Dispose();
                    }
                }

                this.disposed = true;
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
        private Func<IContext, object> GetInstanceScopeFrom(DependencyLifecycle dependencyLifecycle)
        {
            Func<IContext, object> scope;

            if (!this.dependencyLifecycleToScopeMapping.TryGetValue(dependencyLifecycle, out scope))
            {
                throw new ArgumentException("The dependency lifecycle is not supported", "dependencyLifecycle");
            }

            return scope;
        }

        /// <summary>
        /// Binds the aliases of component to component with the given <paramref name="instanceScope"/>.
        /// </summary>
        /// <param name="component">
        /// The component.
        /// </param>
        /// <param name="instanceScope">
        /// The instance scope.
        /// </param>
        private void BindAliasesOfComponentToComponent(Type component, Func<IContext, object> instanceScope)
        {
            var services = GetAllServiceTypesFor(component).Where(t => t != component);

            foreach (var service in services)
            {
                this.kernel.Bind(service).ToMethod(ctx => ctx.Kernel.Get(component))
                    .InScope(instanceScope);
            }
        }

        /// <summary>
        /// Binds the component to itself with the given <paramref name="instanceScope"/>.
        /// </summary>
        /// <param name="component">
        /// The component.
        /// </param>
        /// <param name="instanceScope">
        /// The instance scope.
        /// </param>
        private void BindComponentToItself(Type component, Func<IContext, object> instanceScope)
        {
            this.kernel.Bind(component).ToSelf()
                .InScope(instanceScope);
        }

        /// <summary>
        /// Adds the custom property injection heuristic.
        /// </summary>
        private void AddCustomPropertyInjectionHeuristic()
        {
            ISelector selector = this.kernel.Components.Get<ISelector>();

            selector.InjectionHeuristics.Add(
                this.kernel.Get<IObjectBuilderPropertyHeuristic>());
        }

        /// <summary>
        /// Registers the necessary bindings.
        /// </summary>
        private void RegisterNecessaryBindings()
        {
            this.kernel.Bind<IContainer>().ToConstant(this).InSingletonScope();

            this.kernel.Bind<NewActivationPropertyInjectStrategy>().ToSelf()
                .InSingletonScope()
                .WithPropertyValue("Settings", ctx => ctx.Kernel.Settings);

            this.kernel.Bind<IObjectBuilderPropertyHeuristic>().To<ObjectBuilderPropertyHeuristic>()
                .InSingletonScope()
                .WithPropertyValue("Settings", ctx => ctx.Kernel.Settings);

            this.kernel.Bind<IInjectorFactory>().ToMethod(ctx => ctx.Kernel.Components.Get<IInjectorFactory>());
        }

        /// <summary>
        /// Replaces the default property injection strategy with custom property injection strategy.
        /// </summary>
        private void ReplacePropertyInjectionStrategyWithCustomPropertyInjectionStrategy()
        {
            IList<IActivationStrategy> activationStrategies = this.kernel.Components.Get<IPipeline>().Strategies;

            IList<IActivationStrategy> copiedStrategies = new List<IActivationStrategy>(
                activationStrategies.Where(strategy => !strategy.GetType().Equals(typeof(PropertyInjectionStrategy)))
                    .Union(new List<IActivationStrategy> { this.kernel.Get<NewActivationPropertyInjectStrategy>() }));

            activationStrategies.Clear();
            copiedStrategies.ToList().ForEach(activationStrategies.Add);
        }
    }
}