/*********************************************************************************
    The MIT License (MIT)

    Copyright (c) 2020 bernhard.richter@gmail.com

    Permission is hereby granted, free of charge, to any person obtaining a copy
    of this software and associated documentation files (the "Software"), to deal
    in the Software without restriction, including without limitation the rights
    to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
    copies of the Software, and to permit persons to whom the Software is
    furnished to do so, subject to the following conditions:

    The above copyright notice and this permission notice shall be included in all
    copies or substantial portions of the Software.

    THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
    IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
    FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
    AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
    LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
    OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
    SOFTWARE.
******************************************************************************
    LightInject version 6.3.5
    http://www.lightinject.net/
    http://twitter.com/bernhardrichter
******************************************************************************/

//NOTE: This is LightInject 6.4.0: https://github.com/seesharper/LightInject/blob/v6.4.0/src/LightInject/LightInject.cs with all public types made internal, altered conditional compilation (see further notes).

[module: System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.ReadabilityRules", "SA1126:PrefixCallsCorrectly", Justification = "Reviewed")]
[module: System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.ReadabilityRules", "SA1101:PrefixLocalCallsWithThis", Justification = "No inheritance")]
[module: System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1402:FileMayOnlyContainASingleClass", Justification = "Single source file deployment.")]
[module: System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1633:FileMustHaveHeader", Justification = "Custom header.")]
[module: System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1600:ElementsMustBeDocumented", Justification = "All public members are documented.")]
[module: System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1401:FieldsMustBePrivate", Justification = "Performance")]
[module: System.Diagnostics.CodeAnalysis.SuppressMessage("MaintainabilityRules", "SA1403", Justification = "One source file")]
[module: System.Diagnostics.CodeAnalysis.SuppressMessage("DocumentationRules", "SA1649", Justification = "One source file")]

namespace LightInject
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.IO;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Reflection;
    using System.Reflection.Emit;
    using System.Runtime.CompilerServices;
#if NET452
    using System.Runtime.Remoting.Messaging;
#endif
    using System.Text;
    using System.Text.RegularExpressions;
    using System.Threading;

    /// <summary>
    /// A delegate that represents the dynamic method compiled to resolved service instances.
    /// </summary>
    /// <param name="args">The arguments used by the dynamic method that this delegate represents.</param>
    /// <param name="scope">The <see cref="Scope"/> to be used when resolving the instance.
    /// If this is set to null, the ambient (current) scope is used.</param>
    /// <returns>A service instance.</returns>
    internal delegate object GetInstanceDelegate(object[] args, Scope scope);

    /// <summary>
    /// Describes the logging level/severity.
    /// </summary>
    internal enum LogLevel
    {
        /// <summary>
        /// Indicates the <see cref="LogEntry"/> represents an information message.
        /// </summary>
        Info,

        /// <summary>
        /// Indicates the <see cref="LogEntry"/> represents a warning message.
        /// </summary>
        Warning,
    }

    /// <summary>
    /// Defines a set of methods used to register services into the service container.
    /// </summary>
    internal interface IServiceRegistry
    {
        /// <summary>
        /// Gets a list of <see cref="ServiceRegistration"/> instances that represents the
        /// registered services.
        /// </summary>
        IEnumerable<ServiceRegistration> AvailableServices { get; }

        /// <summary>
        /// Registers the <paramref name="serviceType"/> with the <paramref name="implementingType"/>.
        /// </summary>
        /// <param name="serviceType">The service type to register.</param>
        /// <param name="implementingType">The implementing type.</param>
        /// <returns>The <see cref="IServiceRegistry"/>, for chaining calls.</returns>
        IServiceRegistry Register(Type serviceType, Type implementingType);

        /// <summary>
        /// Registers the <paramref name="serviceType"/> with the <paramref name="implementingType"/>.
        /// </summary>
        /// <param name="serviceType">The service type to register.</param>
        /// <param name="implementingType">The implementing type.</param>
        /// <param name="lifetime">The <see cref="ILifetime"/> instance that controls the lifetime of the registered service.</param>
        /// <returns>The <see cref="IServiceRegistry"/>, for chaining calls.</returns>
        IServiceRegistry Register(Type serviceType, Type implementingType, ILifetime lifetime);

        /// <summary>
        /// Registers the <paramref name="serviceType"/> with the <paramref name="implementingType"/>.
        /// </summary>
        /// <param name="serviceType">The service type to register.</param>
        /// <param name="implementingType">The implementing type.</param>
        /// <param name="serviceName">The name of the service.</param>
        /// <returns>The <see cref="IServiceRegistry"/>, for chaining calls.</returns>
        IServiceRegistry Register(Type serviceType, Type implementingType, string serviceName);

        /// <summary>
        /// Registers the <paramref name="serviceType"/> with the <paramref name="implementingType"/>.
        /// </summary>
        /// <param name="serviceType">The service type to register.</param>
        /// <param name="implementingType">The implementing type.</param>
        /// <param name="serviceName">The name of the service.</param>
        /// <param name="lifetime">The <see cref="ILifetime"/> instance that controls the lifetime of the registered service.</param>
        /// <returns>The <see cref="IServiceRegistry"/>, for chaining calls.</returns>
        IServiceRegistry Register(Type serviceType, Type implementingType, string serviceName, ILifetime lifetime);

        /// <summary>
        /// Registers the <typeparamref name="TService"/> with the <typeparamref name="TImplementation"/>.
        /// </summary>
        /// <typeparam name="TService">The service type to register.</typeparam>
        /// <typeparam name="TImplementation">The implementing type.</typeparam>
        /// <returns>The <see cref="IServiceRegistry"/>, for chaining calls.</returns>
        IServiceRegistry Register<TService, TImplementation>()
            where TImplementation : TService;

        /// <summary>
        /// Registers the <typeparamref name="TService"/> with the <typeparamref name="TImplementation"/>.
        /// </summary>
        /// <typeparam name="TService">The service type to register.</typeparam>
        /// <typeparam name="TImplementation">The implementing type.</typeparam>
        /// <param name="lifetime">The <see cref="ILifetime"/> instance that controls the lifetime of the registered service.</param>
        /// <returns>The <see cref="IServiceRegistry"/>, for chaining calls.</returns>
        IServiceRegistry Register<TService, TImplementation>(ILifetime lifetime)
            where TImplementation : TService;

        /// <summary>
        /// Registers the <typeparamref name="TService"/> with the <typeparamref name="TImplementation"/>.
        /// </summary>
        /// <typeparam name="TService">The service type to register.</typeparam>
        /// <typeparam name="TImplementation">The implementing type.</typeparam>
        /// <param name="serviceName">The name of the service.</param>
        /// <returns>The <see cref="IServiceRegistry"/>, for chaining calls.</returns>
        IServiceRegistry Register<TService, TImplementation>(string serviceName)
            where TImplementation : TService;

        /// <summary>
        /// Registers the <typeparamref name="TService"/> with the <typeparamref name="TImplementation"/>.
        /// </summary>
        /// <typeparam name="TService">The service type to register.</typeparam>
        /// <typeparam name="TImplementation">The implementing type.</typeparam>
        /// <param name="serviceName">The name of the service.</param>
        /// <param name="lifetime">The <see cref="ILifetime"/> instance that controls the lifetime of the registered service.</param>
        /// <returns>The <see cref="IServiceRegistry"/>, for chaining calls.</returns>
        IServiceRegistry Register<TService, TImplementation>(string serviceName, ILifetime lifetime)
            where TImplementation : TService;

        /// <summary>
        /// Registers the <typeparamref name="TService"/> with the given <paramref name="instance"/>.
        /// </summary>
        /// <typeparam name="TService">The service type to register.</typeparam>
        /// <param name="instance">The instance returned when this service is requested.</param>
        /// <returns>The <see cref="IServiceRegistry"/>, for chaining calls.</returns>
        IServiceRegistry RegisterInstance<TService>(TService instance);

        /// <summary>
        /// Registers the <typeparamref name="TService"/> with the given <paramref name="instance"/>.
        /// </summary>
        /// <typeparam name="TService">The service type to register.</typeparam>
        /// <param name="instance">The instance returned when this service is requested.</param>
        /// <param name="serviceName">The name of the service.</param>
        /// <returns>The <see cref="IServiceRegistry"/>, for chaining calls.</returns>
        IServiceRegistry RegisterInstance<TService>(TService instance, string serviceName);

        /// <summary>
        /// Registers the <paramref name="serviceType"/> with the given <paramref name="instance"/>.
        /// </summary>
        /// <param name="serviceType">The service type to register.</param>
        /// <param name="instance">The instance returned when this service is requested.</param>
        /// <returns>The <see cref="IServiceRegistry"/>, for chaining calls.</returns>
        IServiceRegistry RegisterInstance(Type serviceType, object instance);

        /// <summary>
        /// Registers the <paramref name="serviceType"/> with the given <paramref name="instance"/>.
        /// </summary>
        /// <param name="serviceType">The service type to register.</param>
        /// <param name="instance">The instance returned when this service is requested.</param>
        /// <param name="serviceName">The name of the service.</param>
        /// <returns>The <see cref="IServiceRegistry"/>, for chaining calls.</returns>
        IServiceRegistry RegisterInstance(Type serviceType, object instance, string serviceName);

        /// <summary>
        /// Registers a concrete type as a service.
        /// </summary>
        /// <typeparam name="TService">The service type to register.</typeparam>
        /// <returns>The <see cref="IServiceRegistry"/>, for chaining calls.</returns>
        IServiceRegistry Register<TService>();

        /// <summary>
        /// Registers a concrete type as a service.
        /// </summary>
        /// <typeparam name="TService">The service type to register.</typeparam>
        /// <param name="lifetime">The <see cref="ILifetime"/> instance that controls the lifetime of the registered service.</param>
        /// <returns>The <see cref="IServiceRegistry"/>, for chaining calls.</returns>
        IServiceRegistry Register<TService>(ILifetime lifetime);

        /// <summary>
        /// Registers a concrete type as a service.
        /// </summary>
        /// <param name="serviceType">The concrete type to register.</param>
        /// <returns>The <see cref="IServiceRegistry"/>, for chaining calls.</returns>
        IServiceRegistry Register(Type serviceType);

        /// <summary>
        /// Registers a concrete type as a service.
        /// </summary>
        /// <param name="serviceType">The concrete type to register.</param>
        /// <param name="lifetime">The <see cref="ILifetime"/> instance that controls the lifetime of the registered service.</param>
        /// <returns>The <see cref="IServiceRegistry"/>, for chaining calls.</returns>
        IServiceRegistry Register(Type serviceType, ILifetime lifetime);

        /// <summary>
        /// Registers the <typeparamref name="TService"/> with the <paramref name="factory"/> that
        /// describes the dependencies of the service.
        /// </summary>
        /// <typeparam name="TService">The service type to register.</typeparam>
        /// <param name="factory">A factory delegate used to create the <typeparamref name="TService"/> instance.</param>
        /// <returns>The <see cref="IServiceRegistry"/>, for chaining calls.</returns>
        IServiceRegistry Register<TService>(Func<IServiceFactory, TService> factory);

        /// <summary>
        /// Registers the <typeparamref name="TService"/> with the <paramref name="factory"/> that
        /// describes the dependencies of the service.
        /// </summary>
        /// <typeparam name="T">The parameter type.</typeparam>
        /// <typeparam name="TService">The service type to register.</typeparam>
        /// <param name="factory">A factory delegate used to create the <typeparamref name="TService"/> instance.</param>
        /// <returns>The <see cref="IServiceRegistry"/>, for chaining calls.</returns>
        IServiceRegistry Register<T, TService>(Func<IServiceFactory, T, TService> factory);

        /// <summary>
        /// Registers the <typeparamref name="TService"/> with the <paramref name="factory"/> that
        /// describes the dependencies of the service.
        /// </summary>
        /// <typeparam name="T">The parameter type.</typeparam>
        /// <typeparam name="TService">The service type to register.</typeparam>
        /// <param name="factory">A factory delegate used to create the <typeparamref name="TService"/> instance.</param>
        /// <param name="serviceName">The name of the service.</param>
        /// <returns>The <see cref="IServiceRegistry"/>, for chaining calls.</returns>
        IServiceRegistry Register<T, TService>(Func<IServiceFactory, T, TService> factory, string serviceName);

        /// <summary>
        /// Registers the <typeparamref name="TService"/> with the <paramref name="factory"/> that
        /// describes the dependencies of the service.
        /// </summary>
        /// <typeparam name="T1">The type of the first parameter.</typeparam>
        /// <typeparam name="T2">The type of the second parameter.</typeparam>
        /// <typeparam name="TService">The service type to register.</typeparam>
        /// <param name="factory">A factory delegate used to create the <typeparamref name="TService"/> instance.</param>
        /// <returns>The <see cref="IServiceRegistry"/>, for chaining calls.</returns>
        IServiceRegistry Register<T1, T2, TService>(Func<IServiceFactory, T1, T2, TService> factory);

        /// <summary>
        /// Registers the <typeparamref name="TService"/> with the <paramref name="factory"/> that
        /// describes the dependencies of the service.
        /// </summary>
        /// <typeparam name="T1">The type of the first parameter.</typeparam>
        /// <typeparam name="T2">The type of the second parameter.</typeparam>
        /// <typeparam name="TService">The service type to register.</typeparam>
        /// <param name="factory">A factory delegate used to create the <typeparamref name="TService"/> instance.</param>
        /// <param name="serviceName">The name of the service.</param>
        /// <returns>The <see cref="IServiceRegistry"/>, for chaining calls.</returns>
        IServiceRegistry Register<T1, T2, TService>(Func<IServiceFactory, T1, T2, TService> factory, string serviceName);

        /// <summary>
        /// Registers the <typeparamref name="TService"/> with the <paramref name="factory"/> that
        /// describes the dependencies of the service.
        /// </summary>
        /// <typeparam name="T1">The type of the first parameter.</typeparam>
        /// <typeparam name="T2">The type of the second parameter.</typeparam>
        /// <typeparam name="T3">The type of the third parameter.</typeparam>
        /// <typeparam name="TService">The service type to register.</typeparam>
        /// <param name="factory">A factory delegate used to create the <typeparamref name="TService"/> instance.</param>
        /// <returns>The <see cref="IServiceRegistry"/>, for chaining calls.</returns>
        IServiceRegistry Register<T1, T2, T3, TService>(Func<IServiceFactory, T1, T2, T3, TService> factory);

        /// <summary>
        /// Registers the <typeparamref name="TService"/> with the <paramref name="factory"/> that
        /// describes the dependencies of the service.
        /// </summary>
        /// <typeparam name="T1">The type of the first parameter.</typeparam>
        /// <typeparam name="T2">The type of the second parameter.</typeparam>
        /// <typeparam name="T3">The type of the third parameter.</typeparam>
        /// <typeparam name="TService">The service type to register.</typeparam>
        /// <param name="factory">A factory delegate used to create the <typeparamref name="TService"/> instance.</param>
        /// <param name="serviceName">The name of the service.</param>
        /// <returns>The <see cref="IServiceRegistry"/>, for chaining calls.</returns>
        IServiceRegistry Register<T1, T2, T3, TService>(Func<IServiceFactory, T1, T2, T3, TService> factory, string serviceName);

        /// <summary>
        /// Registers the <typeparamref name="TService"/> with the <paramref name="factory"/> that
        /// describes the dependencies of the service.
        /// </summary>
        /// <typeparam name="T1">The type of the first parameter.</typeparam>
        /// <typeparam name="T2">The type of the second parameter.</typeparam>
        /// <typeparam name="T3">The type of the third parameter.</typeparam>
        /// <typeparam name="T4">The type of the fourth parameter.</typeparam>
        /// <typeparam name="TService">The service type to register.</typeparam>
        /// <param name="factory">A factory delegate used to create the <typeparamref name="TService"/> instance.</param>
        /// <returns>The <see cref="IServiceRegistry"/>, for chaining calls.</returns>
        IServiceRegistry Register<T1, T2, T3, T4, TService>(Func<IServiceFactory, T1, T2, T3, T4, TService> factory);

        /// <summary>
        /// Registers the <typeparamref name="TService"/> with the <paramref name="factory"/> that
        /// describes the dependencies of the service.
        /// </summary>
        /// <typeparam name="T1">The type of the first parameter.</typeparam>
        /// <typeparam name="T2">The type of the second parameter.</typeparam>
        /// <typeparam name="T3">The type of the third parameter.</typeparam>
        /// <typeparam name="T4">The type of the fourth parameter.</typeparam>
        /// <typeparam name="TService">The service type to register.</typeparam>
        /// <param name="factory">A factory delegate used to create the <typeparamref name="TService"/> instance.</param>
        /// <param name="serviceName">The name of the service.</param>
        /// <returns>The <see cref="IServiceRegistry"/>, for chaining calls.</returns>
        IServiceRegistry Register<T1, T2, T3, T4, TService>(Func<IServiceFactory, T1, T2, T3, T4, TService> factory, string serviceName);

        /// <summary>
        /// Registers the <typeparamref name="TService"/> with the <paramref name="factory"/> that
        /// describes the dependencies of the service.
        /// </summary>
        /// <typeparam name="TService">The service type to register.</typeparam>
        /// <param name="factory">The lambdaExpression that describes the dependencies of the service.</param>
        /// <param name="lifetime">The <see cref="ILifetime"/> instance that controls the lifetime of the registered service.</param>
        /// <returns>The <see cref="IServiceRegistry"/>, for chaining calls.</returns>
        IServiceRegistry Register<TService>(Func<IServiceFactory, TService> factory, ILifetime lifetime);

        /// <summary>
        /// Registers the <typeparamref name="TService"/> with the <paramref name="factory"/> that
        /// describes the dependencies of the service.
        /// </summary>
        /// <typeparam name="TService">The service type to register.</typeparam>
        /// <param name="factory">The lambdaExpression that describes the dependencies of the service.</param>
        /// <param name="serviceName">The name of the service.</param>
        /// <returns>The <see cref="IServiceRegistry"/>, for chaining calls.</returns>
        IServiceRegistry Register<TService>(Func<IServiceFactory, TService> factory, string serviceName);

        /// <summary>
        /// Registers the <typeparamref name="TService"/> with the <paramref name="factory"/> that
        /// describes the dependencies of the service.
        /// </summary>
        /// <typeparam name="TService">The service type to register.</typeparam>
        /// <param name="factory">The lambdaExpression that describes the dependencies of the service.</param>
        /// <param name="serviceName">The name of the service.</param>
        /// <param name="lifetime">The <see cref="ILifetime"/> instance that controls the lifetime of the registered service.</param>
        /// <returns>The <see cref="IServiceRegistry"/>, for chaining calls.</returns>
        IServiceRegistry Register<TService>(Func<IServiceFactory, TService> factory, string serviceName, ILifetime lifetime);

        /// <summary>
        /// Registers the <paramref name="serviceType"/> with a set of <paramref name="implementingTypes"/> and
        /// ensures that service instance ordering matches the ordering of the <paramref name="implementingTypes"/>.
        /// </summary>
        /// <param name="serviceType">The service type to register.</param>
        /// <param name="implementingTypes">The implementing types.</param>
        /// <param name="lifetimeFactory">The <see cref="ILifetime"/> factory that controls the lifetime of each entry in <paramref name="implementingTypes"/>.</param>
        /// <returns>The <see cref="IServiceRegistry"/>, for chaining calls.</returns>
        IServiceRegistry RegisterOrdered(Type serviceType, Type[] implementingTypes, Func<Type, ILifetime> lifetimeFactory);

        /// <summary>
        /// Registers the <paramref name="serviceType"/> with a set of <paramref name="implementingTypes"/> and
        /// ensures that service instance ordering matches the ordering of the <paramref name="implementingTypes"/>.
        /// </summary>
        /// <param name="serviceType">The service type to register.</param>
        /// <param name="implementingTypes">The implementing types.</param>
        /// <param name="lifeTimeFactory">The <see cref="ILifetime"/> factory that controls the lifetime of each entry in <paramref name="implementingTypes"/>.</param>
        /// <param name="serviceNameFormatter">The function used to format the service name based on current registration index.</param>
        /// <returns>The <see cref="IServiceRegistry"/>, for chaining calls.</returns>
        IServiceRegistry RegisterOrdered(
            Type serviceType,
            Type[] implementingTypes,
            Func<Type, ILifetime> lifeTimeFactory,
            Func<int, string> serviceNameFormatter);

        /// <summary>
        /// Registers a custom factory delegate used to create services that is otherwise unknown to the service container.
        /// </summary>
        /// <param name="predicate">Determines if the service can be created by the <paramref name="factory"/> delegate.</param>
        /// <param name="factory">Creates a service instance according to the <paramref name="predicate"/> predicate.</param>
        /// <returns>The <see cref="IServiceRegistry"/>, for chaining calls.</returns>
        IServiceRegistry RegisterFallback(Func<Type, string, bool> predicate, Func<ServiceRequest, object> factory);

        /// <summary>
        /// Registers a custom factory delegate used to create services that is otherwise unknown to the service container.
        /// </summary>
        /// <param name="predicate">Determines if the service can be created by the <paramref name="factory"/> delegate.</param>
        /// <param name="factory">Creates a service instance according to the <paramref name="predicate"/> predicate.</param>
        /// <param name="lifetime">The <see cref="ILifetime"/> instance that controls the lifetime of the registered service.</param>
        /// <returns>The <see cref="IServiceRegistry"/>, for chaining calls.</returns>
        IServiceRegistry RegisterFallback(Func<Type, string, bool> predicate, Func<ServiceRequest, object> factory, ILifetime lifetime);

        /// <summary>
        /// Registers a service based on a <see cref="ServiceRegistration"/> instance.
        /// </summary>
        /// <param name="serviceRegistration">The <see cref="ServiceRegistration"/> instance that contains service metadata.</param>
        /// <returns>The <see cref="IServiceRegistry"/>, for chaining calls.</returns>
        IServiceRegistry Register(ServiceRegistration serviceRegistration);

        /// <summary>
        /// Registers composition roots from the given <paramref name="assembly"/>.
        /// </summary>
        /// <param name="assembly">The assembly to be scanned for services.</param>
        /// <remarks>
        /// If the target <paramref name="assembly"/> contains an implementation of the <see cref="ICompositionRoot"/> interface, this
        /// will be used to configure the container.
        /// </remarks>
        /// <returns>The <see cref="IServiceRegistry"/>, for chaining calls.</returns>
        IServiceRegistry RegisterAssembly(Assembly assembly);

        /// <summary>
        /// Registers services from the given <paramref name="assembly"/>.
        /// </summary>
        /// <param name="assembly">The assembly to be scanned for services.</param>
        /// <param name="shouldRegister">A function delegate that determines if a service implementation should be registered.</param>
        /// <remarks>
        /// If the target <paramref name="assembly"/> contains an implementation of the <see cref="ICompositionRoot"/> interface, this
        /// will be used to configure the container.
        /// </remarks>
        /// <returns>The <see cref="IServiceRegistry"/>, for chaining calls.</returns>
        IServiceRegistry RegisterAssembly(Assembly assembly, Func<Type, Type, bool> shouldRegister);

        /// <summary>
        /// Registers services from the given <paramref name="assembly"/>.
        /// </summary>
        /// <param name="assembly">The assembly to be scanned for services.</param>
        /// <param name="lifetimeFactory">The <see cref="ILifetime"/> factory that controls the lifetime of the registered service.</param>
        /// <remarks>
        /// If the target <paramref name="assembly"/> contains an implementation of the <see cref="ICompositionRoot"/> interface, this
        /// will be used to configure the container.
        /// </remarks>
        /// <returns>The <see cref="IServiceRegistry"/>, for chaining calls.</returns>
        IServiceRegistry RegisterAssembly(Assembly assembly, Func<ILifetime> lifetimeFactory);

        /// <summary>
        /// Registers services from the given <paramref name="assembly"/>.
        /// </summary>
        /// <param name="assembly">The assembly to be scanned for services.</param>
        /// <param name="lifetimeFactory">The <see cref="ILifetime"/> factory that controls the lifetime of the registered service.</param>
        /// <param name="shouldRegister">A function delegate that determines if a service implementation should be registered.</param>
        /// <remarks>
        /// If the target <paramref name="assembly"/> contains an implementation of the <see cref="ICompositionRoot"/> interface, this
        /// will be used to configure the container.
        /// </remarks>
        /// <returns>The <see cref="IServiceRegistry"/>, for chaining calls.</returns>
        IServiceRegistry RegisterAssembly(Assembly assembly, Func<ILifetime> lifetimeFactory, Func<Type, Type, bool> shouldRegister);

        /// <summary>
        /// Registers services from the given <paramref name="assembly"/>.
        /// </summary>
        /// <param name="assembly">The assembly to be scanned for services.</param>
        /// <param name="lifetimeFactory">The <see cref="ILifetime"/> factory that controls the lifetime of the registered service.</param>
        /// <param name="shouldRegister">A function delegate that determines if a service implementation should be registered.</param>
        /// <param name="serviceNameProvider">A function delegate used to provide the service name for a service during assembly scanning.</param>
        /// <remarks>
        /// If the target <paramref name="assembly"/> contains an implementation of the <see cref="ICompositionRoot"/> interface, this
        /// will be used to configure the container.
        /// </remarks>
        /// <returns>The <see cref="IServiceRegistry"/>, for chaining calls.</returns>
        IServiceRegistry RegisterAssembly(
            Assembly assembly,
            Func<ILifetime> lifetimeFactory,
            Func<Type, Type, bool> shouldRegister,
            Func<Type, Type, string> serviceNameProvider);

        /// <summary>
        /// Registers services from the given <typeparamref name="TCompositionRoot"/> type.
        /// </summary>
        /// <typeparam name="TCompositionRoot">The type of <see cref="ICompositionRoot"/> to register from.</typeparam>
        /// <returns>The <see cref="IServiceRegistry"/>, for chaining calls.</returns>
        IServiceRegistry RegisterFrom<TCompositionRoot>()
            where TCompositionRoot : ICompositionRoot, new();

        /// <summary>
        /// Registers services from the given <paramref name="compositionRoot"/>.
        /// </summary>
        /// <param name="compositionRoot">The <see cref="ICompositionRoot"/> from which to register services.</param>
        /// <typeparam name="TCompositionRoot">The type of <see cref="ICompositionRoot"/> to register from.</typeparam>
        /// <returns>The <see cref="IServiceRegistry"/>, for chaining calls.</returns>
        IServiceRegistry RegisterFrom<TCompositionRoot>(TCompositionRoot compositionRoot)
            where TCompositionRoot : ICompositionRoot;

        /// <summary>
        /// Registers a factory delegate to be used when resolving a constructor dependency for
        /// an implicitly registered service.
        /// </summary>
        /// <typeparam name="TDependency">The dependency type.</typeparam>
        /// <param name="factory">The factory delegate used to create an instance of the dependency.</param>
        /// <returns>The <see cref="IServiceRegistry"/>, for chaining calls.</returns>
        IServiceRegistry RegisterConstructorDependency<TDependency>(
            Func<IServiceFactory, ParameterInfo, TDependency> factory);

        /// <summary>
        /// Registers a factory delegate to be used when resolving a constructor dependency for
        /// an implicitly registered service.
        /// </summary>
        /// <typeparam name="TDependency">The dependency type.</typeparam>
        /// <param name="factory">The factory delegate used to create an instance of the dependency.</param>
        /// <returns>The <see cref="IServiceRegistry"/>, for chaining calls.</returns>
        IServiceRegistry RegisterConstructorDependency<TDependency>(
            Func<IServiceFactory, ParameterInfo, object[], TDependency> factory);

        /// <summary>
        /// Registers a factory delegate to be used when resolving a constructor dependency for
        /// an implicitly registered service.
        /// </summary>
        /// <typeparam name="TDependency">The dependency type.</typeparam>
        /// <param name="factory">The factory delegate used to create an instance of the dependency.</param>
        /// <returns>The <see cref="IServiceRegistry"/>, for chaining calls.</returns>
        IServiceRegistry RegisterPropertyDependency<TDependency>(
            Func<IServiceFactory, PropertyInfo, TDependency> factory);

#if NET452 || NET46 || NETSTANDARD1_6 || NETSTANDARD2_0 || NETCOREAPP2_0
        /// <summary>
        /// Registers composition roots from assemblies in the base directory that match the <paramref name="searchPattern"/>.
        /// </summary>
        /// <param name="searchPattern">The search pattern used to filter the assembly files.</param>
        /// <returns>The <see cref="IServiceRegistry"/>, for chaining calls.</returns>
        IServiceRegistry RegisterAssembly(string searchPattern);
#endif

        /// <summary>
        /// Decorates the <paramref name="serviceType"/> with the given <paramref name="decoratorType"/>.
        /// </summary>
        /// <param name="serviceType">The target service type.</param>
        /// <param name="decoratorType">The decorator type used to decorate the <paramref name="serviceType"/>.</param>
        /// <param name="predicate">A function delegate that determines if the <paramref name="decoratorType"/>
        /// should be applied to the target <paramref name="serviceType"/>.</param>
        /// <returns>The <see cref="IServiceRegistry"/>, for chaining calls.</returns>
        IServiceRegistry Decorate(Type serviceType, Type decoratorType, Func<ServiceRegistration, bool> predicate);

        /// <summary>
        /// Decorates the <paramref name="serviceType"/> with the given <paramref name="decoratorType"/>.
        /// </summary>
        /// <param name="serviceType">The target service type.</param>
        /// <param name="decoratorType">The decorator type used to decorate the <paramref name="serviceType"/>.</param>
        /// <returns>The <see cref="IServiceRegistry"/>, for chaining calls.</returns>
        IServiceRegistry Decorate(Type serviceType, Type decoratorType);

        /// <summary>
        /// Decorates the <typeparamref name="TService"/> with the given <typeparamref name="TDecorator"/>.
        /// </summary>
        /// <typeparam name="TService">The target service type.</typeparam>
        /// <typeparam name="TDecorator">The decorator type used to decorate the <typeparamref name="TService"/>.</typeparam>
        /// <returns>The <see cref="IServiceRegistry"/>, for chaining calls.</returns>
        IServiceRegistry Decorate<TService, TDecorator>()
            where TDecorator : TService;

        /// <summary>
        /// Decorates the <typeparamref name="TService"/> using the given decorator <paramref name="factory"/>.
        /// </summary>
        /// <typeparam name="TService">The target service type.</typeparam>
        /// <param name="factory">A factory delegate used to create a decorator instance.</param>
        /// <returns>The <see cref="IServiceRegistry"/>, for chaining calls.</returns>
        IServiceRegistry Decorate<TService>(Func<IServiceFactory, TService, TService> factory);

        /// <summary>
        /// Registers a decorator based on a <see cref="DecoratorRegistration"/> instance.
        /// </summary>
        /// <param name="decoratorRegistration">The <see cref="DecoratorRegistration"/> instance that contains the decorator metadata.</param>
        /// <returns>The <see cref="IServiceRegistry"/>, for chaining calls.</returns>
        IServiceRegistry Decorate(DecoratorRegistration decoratorRegistration);

        /// <summary>
        /// Allows a registered service to be overridden by another <see cref="ServiceRegistration"/>.
        /// </summary>
        /// <param name="serviceSelector">A function delegate that is used to determine the service that should be
        /// overridden using the <see cref="ServiceRegistration"/> returned from the <paramref name="serviceRegistrationFactory"/>.</param>
        /// <param name="serviceRegistrationFactory">The factory delegate used to create a <see cref="ServiceRegistration"/> that overrides
        /// the incoming <see cref="ServiceRegistration"/>.</param>
        /// <returns>The <see cref="IServiceRegistry"/>, for chaining calls.</returns>
        IServiceRegistry Override(
            Func<ServiceRegistration, bool> serviceSelector,
            Func<IServiceFactory, ServiceRegistration, ServiceRegistration> serviceRegistrationFactory);

        /// <summary>
        /// Allows post-processing of a service instance.
        /// </summary>
        /// <param name="predicate">A function delegate that determines if the given service can be post-processed.</param>
        /// <param name="processor">An action delegate that exposes the created service instance.</param>
        /// <returns>The <see cref="IServiceRegistry"/>, for chaining calls.</returns>
        IServiceRegistry Initialize(Func<ServiceRegistration, bool> predicate, Action<IServiceFactory, object> processor);

        /// <summary>
        /// Sets the default lifetime for types registered without an explicit lifetime. Will only affect new registrations (after this call).
        /// </summary>
        /// <typeparam name="T">The default lifetime type.</typeparam>
        /// <returns>The <see cref="IServiceRegistry"/>, for chaining calls.</returns>
        IServiceRegistry SetDefaultLifetime<T>()
            where T : ILifetime, new();
    }

    /// <summary>
    /// Defines a set of methods used to retrieve service instances.
    /// </summary>
    internal interface IServiceFactory
    {
        /// <summary>
        /// Starts a new <see cref="Scope"/>.
        /// </summary>
        /// <returns><see cref="Scope"/>.</returns>
        Scope BeginScope();

        /// <summary>
        /// Gets an instance of the given <paramref name="serviceType"/>.
        /// </summary>
        /// <param name="serviceType">The type of the requested service.</param>
        /// <returns>The requested service instance.</returns>
        object GetInstance(Type serviceType);

        /// <summary>
        /// Gets an instance of the given <paramref name="serviceType"/>.
        /// </summary>
        /// <param name="serviceType">The type of the requested service.</param>
        /// <param name="arguments">The arguments to be passed to the target instance.</param>
        /// <returns>The requested service instance.</returns>
        object GetInstance(Type serviceType, object[] arguments);

        /// <summary>
        /// Gets an instance of the given <paramref name="serviceType"/>.
        /// </summary>
        /// <param name="serviceType">The type of the requested service.</param>
        /// <param name="serviceName">The name of the requested service.</param>
        /// <param name="arguments">The arguments to be passed to the target instance.</param>
        /// <returns>The requested service instance.</returns>
        object GetInstance(Type serviceType, string serviceName, object[] arguments);

        /// <summary>
        /// Gets a named instance of the given <paramref name="serviceType"/>.
        /// </summary>
        /// <param name="serviceType">The type of the requested service.</param>
        /// <param name="serviceName">The name of the requested service.</param>
        /// <returns>The requested service instance.</returns>
        object GetInstance(Type serviceType, string serviceName);

        /// <summary>
        /// Gets an instance of the given <paramref name="serviceType"/>.
        /// </summary>
        /// <param name="serviceType">The type of the requested service.</param>
        /// <returns>The requested service instance if available, otherwise null.</returns>
        object TryGetInstance(Type serviceType);

        /// <summary>
        /// Gets a named instance of the given <paramref name="serviceType"/>.
        /// </summary>
        /// <param name="serviceType">The type of the requested service.</param>
        /// <param name="serviceName">The name of the requested service.</param>
        /// <returns>The requested service instance if available, otherwise null.</returns>
        object TryGetInstance(Type serviceType, string serviceName);

        /// <summary>
        /// Gets all instances of the given <paramref name="serviceType"/>.
        /// </summary>
        /// <param name="serviceType">The type of services to resolve.</param>
        /// <returns>A list that contains all implementations of the <paramref name="serviceType"/>.</returns>
        IEnumerable<object> GetAllInstances(Type serviceType);

        /// <summary>
        /// Creates an instance of a concrete class.
        /// </summary>
        /// <param name="serviceType">The type of class for which to create an instance.</param>
        /// <returns>An instance of the <paramref name="serviceType"/>.</returns>
        object Create(Type serviceType);
    }

    /// <summary>
    /// Represents an inversion of control container.
    /// </summary>
    internal interface IServiceContainer : IServiceRegistry, IServiceFactory, IDisposable
    {
        /// <summary>
        /// Gets or sets the <see cref="IScopeManagerProvider"/> that is responsible
        /// for providing the <see cref="IScopeManager"/> used to manage scopes.
        /// </summary>
        IScopeManagerProvider ScopeManagerProvider { get; set; }

        /// <summary>
        /// Returns <b>true</b> if the container can create the requested service, otherwise <b>false</b>.
        /// </summary>
        /// <param name="serviceType">The <see cref="Type"/> of the service.</param>
        /// <param name="serviceName">The name of the service.</param>
        /// <returns><b>true</b> if the container can create the requested service, otherwise <b>false</b>.</returns>
        bool CanGetInstance(Type serviceType, string serviceName);

        /// <summary>
        /// Injects the property dependencies for a given <paramref name="instance"/>.
        /// </summary>
        /// <param name="instance">The target instance for which to inject its property dependencies.</param>
        /// <returns>The <paramref name="instance"/> with its property dependencies injected.</returns>
        object InjectProperties(object instance);

        /// <summary>
        /// Compiles all registered services.
        /// </summary>
        void Compile();

        /// <summary>
        /// Compiles services that matches the given <paramref name="predicate"/>.
        /// </summary>
        /// <param name="predicate">The predicate that determines if a service should be compiled.</param>
        void Compile(Func<ServiceRegistration, bool> predicate);

        /// <summary>
        /// Compiles the service identified by <typeparamref name="TService"/>
        /// and optionally the <paramref name="serviceName"/>.
        /// </summary>
        /// <typeparam name="TService">The service type to be compiled.</typeparam>
        /// <param name="serviceName">The name of the service to be compiled.</param>
        void Compile<TService>(string serviceName = null);
    }

    /// <summary>
    /// Represents a class that manages the lifetime of a service instance.
    /// </summary>
    internal interface ILifetime
    {
        /// <summary>
        /// Returns a service instance according to the specific lifetime characteristics.
        /// </summary>
        /// <param name="createInstance">The function delegate used to create a new service instance.</param>
        /// <param name="scope">The <see cref="Scope"/> of the current service request.</param>
        /// <returns>The requested services instance.</returns>
        object GetInstance(Func<object> createInstance, Scope scope);
    }

    /// <summary>
    /// Optionally implemented by <see cref="ILifetime"/> implementations
    /// to provide a way to clone the lifetime.
    /// </summary>
    internal interface ICloneableLifeTime
    {
        /// <summary>
        /// Returns a clone of this <see cref="ILifetime"/>.
        /// </summary>
        /// <returns><see cref="ILifetime"/>.</returns>
        ILifetime Clone();
    }

    /// <summary>
    /// Represents a class that acts as a composition root for an <see cref="IServiceRegistry"/> instance.
    /// </summary>
    internal interface ICompositionRoot
    {
        /// <summary>
        /// Composes services by adding services to the <paramref name="serviceRegistry"/>.
        /// </summary>
        /// <param name="serviceRegistry">The target <see cref="IServiceRegistry"/>.</param>
        void Compose(IServiceRegistry serviceRegistry);
    }

    /// <summary>
    /// Represents a class that extracts a set of types from an <see cref="Assembly"/>.
    /// </summary>
    internal interface ITypeExtractor
    {
        /// <summary>
        /// Extracts types found in the given <paramref name="assembly"/>.
        /// </summary>
        /// <param name="assembly">The <see cref="Assembly"/> for which to extract types.</param>
        /// <returns>A set of types found in the given <paramref name="assembly"/>.</returns>
        Type[] Execute(Assembly assembly);
    }

    /// <summary>
    /// Represents a class that is capable of extracting
    /// attributes of type <see cref="CompositionRootTypeAttribute"/> from an <see cref="Assembly"/>.
    /// </summary>
    internal interface ICompositionRootAttributeExtractor
    {
        /// <summary>
        /// Gets a list of attributes of type <see cref="CompositionRootTypeAttribute"/> from
        /// the given <paramref name="assembly"/>.
        /// </summary>
        /// <param name="assembly">The assembly from which to extract
        /// <see cref="CompositionRootTypeAttribute"/> attributes.</param>
        /// <returns>A list of attributes of type <see cref="CompositionRootTypeAttribute"/>.</returns>
        CompositionRootTypeAttribute[] GetAttributes(Assembly assembly);
    }

    /// <summary>
    /// Represents a class that is responsible for selecting injectable properties.
    /// </summary>
    internal interface IPropertySelector
    {
        /// <summary>
        /// Selects properties that represents a dependency from the given <paramref name="type"/>.
        /// </summary>
        /// <param name="type">The <see cref="Type"/> for which to select the properties.</param>
        /// <returns>A list of injectable properties.</returns>
        IEnumerable<PropertyInfo> Execute(Type type);
    }

    /// <summary>
    /// Represents a class that is responsible for selecting the property dependencies for a given <see cref="Type"/>.
    /// </summary>
    internal interface IPropertyDependencySelector
    {
        /// <summary>
        /// Selects the property dependencies for the given <paramref name="type"/>.
        /// </summary>
        /// <param name="type">The <see cref="Type"/> for which to select the property dependencies.</param>
        /// <returns>A list of <see cref="PropertyDependency"/> instances that represents the property
        /// dependencies for the given <paramref name="type"/>.</returns>
        IEnumerable<PropertyDependency> Execute(Type type);
    }

    /// <summary>
    /// Represents a class that is responsible for selecting the constructor dependencies for a given <see cref="ConstructorInfo"/>.
    /// </summary>
    internal interface IConstructorDependencySelector
    {
        /// <summary>
        /// Selects the constructor dependencies for the given <paramref name="constructor"/>.
        /// </summary>
        /// <param name="constructor">The <see cref="ConstructionInfo"/> for which to select the constructor dependencies.</param>
        /// <returns>A list of <see cref="ConstructorDependency"/> instances that represents the constructor
        /// dependencies for the given <paramref name="constructor"/>.</returns>
        IEnumerable<ConstructorDependency> Execute(ConstructorInfo constructor);
    }

    /// <summary>
    /// Represents a class that is capable of building a <see cref="ConstructorInfo"/> instance
    /// based on a <see cref="Registration"/>.
    /// </summary>
    internal interface IConstructionInfoBuilder
    {
        /// <summary>
        /// Returns a <see cref="ConstructionInfo"/> instance based on the given <see cref="Registration"/>.
        /// </summary>
        /// <param name="registration">The <see cref="Registration"/> for which to return a <see cref="ConstructionInfo"/> instance.</param>
        /// <returns>A <see cref="ConstructionInfo"/> instance that describes how to create a service instance.</returns>
        ConstructionInfo Execute(Registration registration);
    }

    /// <summary>
    /// Represents a class that keeps track of a <see cref="ConstructionInfo"/> instance for each <see cref="Registration"/>.
    /// </summary>
    internal interface IConstructionInfoProvider
    {
        /// <summary>
        /// Gets a <see cref="ConstructionInfo"/> instance for the given <paramref name="registration"/>.
        /// </summary>
        /// <param name="registration">The <see cref="Registration"/> for which to get a <see cref="ConstructionInfo"/> instance.</param>
        /// <returns>The <see cref="ConstructionInfo"/> instance that describes how to create an instance of the given <paramref name="registration"/>.</returns>
        ConstructionInfo GetConstructionInfo(Registration registration);
    }

    /// <summary>
    /// Represents a class that builds a <see cref="ConstructionInfo"/> instance based on the implementing <see cref="Type"/>.
    /// </summary>
    internal interface ITypeConstructionInfoBuilder
    {
        /// <summary>
        /// Analyzes the <paramref name="registration"/> and returns a <see cref="ConstructionInfo"/> instance.
        /// </summary>
        /// <param name="registration">The <see cref="Registration"/> that represents the implementing type to analyze.</param>
        /// <returns>A <see cref="ConstructionInfo"/> instance.</returns>
        ConstructionInfo Execute(Registration registration);
    }

    /// <summary>
    /// Represents a class that maps the generic arguments/parameters from a generic servicetype
    /// to a open generic implementing type.
    /// </summary>
    internal interface IGenericArgumentMapper
    {
        /// <summary>
        /// Maps the generic arguments/parameters from the <paramref name="genericServiceType"/>
        /// to the generic arguments/parameters in the <paramref name="openGenericImplementingType"/>.
        /// </summary>
        /// <param name="genericServiceType">The generic type containing the arguments/parameters to be mapped to the generic arguments/parameters of the <paramref name="openGenericImplementingType"/>.</param>
        /// <param name="openGenericImplementingType">The open generic implementing type.</param>
        /// <returns>A <see cref="GenericMappingResult"/>.</returns>
        GenericMappingResult Map(Type genericServiceType, Type openGenericImplementingType);

        /// <summary>
        /// Attempts to create a generic type based on the <paramref name="genericServiceType"/> and the <paramref name="openGenericImplementingType"/>.
        /// </summary>
        /// <param name="genericServiceType">The generic type containing the arguments/parameters to be mapped to the generic arguments/parameters of the <paramref name="openGenericImplementingType"/>.</param>
        /// <param name="openGenericImplementingType">The open generic implementing type.</param>
        /// <returns>The closed generic type if successful, otherwise null.</returns>
        Type TryMakeGenericType(Type genericServiceType, Type openGenericImplementingType);
    }

    /// <summary>
    /// Represents a class that selects the constructor to be used for creating a new service instance.
    /// </summary>
    internal interface IConstructorSelector
    {
        /// <summary>
        /// Selects the constructor to be used when creating a new instance of the <paramref name="implementingType"/>.
        /// </summary>
        /// <param name="implementingType">The <see cref="Type"/> for which to return a <see cref="ConstructionInfo"/>.</param>
        /// <returns>A <see cref="ConstructionInfo"/> instance that represents the constructor to be used
        /// when creating a new instance of the <paramref name="implementingType"/>.</returns>
        ConstructorInfo Execute(Type implementingType);
    }

    /// <summary>
    /// Represents a class that manages <see cref="Scope"/> instances.
    /// </summary>
    internal interface IScopeManager
    {
        /// <summary>
        /// Gets or sets the current <see cref="Scope"/>.
        /// </summary>
        Scope CurrentScope { get; set; }

        /// <summary>
        /// Gets the <see cref="IServiceFactory"/> that is associated with this <see cref="IScopeManager"/>.
        /// </summary>
        IServiceFactory ServiceFactory { get; }

        /// <summary>
        /// Starts a new <see cref="Scope"/>.
        /// </summary>
        /// <returns>A new <see cref="Scope"/>.</returns>
        Scope BeginScope();

        /// <summary>
        /// Ends the given <paramref name="scope"/>.
        /// </summary>
        /// <param name="scope">The scope to be ended.</param>
        void EndScope(Scope scope);
    }

#if NET452 || NET46 || NETSTANDARD1_6 || NETSTANDARD2_0 || NETCOREAPP2_0

    /// <summary>
    /// Represents a class that is responsible loading a set of assemblies based on the given search pattern.
    /// </summary>
    internal interface IAssemblyLoader
    {
        /// <summary>
        /// Loads a set of assemblies based on the given <paramref name="searchPattern"/>.
        /// </summary>
        /// <param name="searchPattern">The search pattern to use.</param>
        /// <returns>A list of assemblies based on the given <paramref name="searchPattern"/>.</returns>
        IEnumerable<Assembly> Load(string searchPattern);
    }
#endif

    /// <summary>
    /// Represents a class that is capable of scanning an assembly and register services into an <see cref="IServiceContainer"/> instance.
    /// </summary>
    internal interface IAssemblyScanner
    {
        /// <summary>
        /// Scans the target <paramref name="assembly"/> and registers services found within the assembly.
        /// </summary>
        /// <param name="assembly">The <see cref="Assembly"/> to scan.</param>
        /// <param name="serviceRegistry">The target <see cref="IServiceRegistry"/> instance.</param>
        /// <param name="lifetime">The <see cref="ILifetime"/> instance that controls the lifetime of the registered service.</param>
        /// <param name="shouldRegister">A function delegate that determines if a service implementation should be registered.</param>
        /// <param name="serviceNameProvider">A function delegate used to provide the service name for a service during assembly scanning.</param>
        void Scan(Assembly assembly, IServiceRegistry serviceRegistry, Func<ILifetime> lifetime, Func<Type, Type, bool> shouldRegister, Func<Type, Type, string> serviceNameProvider);

        /// <summary>
        /// Scans the target <paramref name="assembly"/> and executes composition roots found within the <see cref="Assembly"/>.
        /// </summary>
        /// <param name="assembly">The <see cref="Assembly"/> to scan.</param>
        /// <param name="serviceRegistry">The target <see cref="IServiceRegistry"/> instance.</param>
        void Scan(Assembly assembly, IServiceRegistry serviceRegistry);
    }

    /// <summary>
    /// Represents a class that is capable of providing a service name
    /// to be used when a service is registered during assembly scanning.
    /// </summary>
    internal interface IServiceNameProvider
    {
        /// <summary>
        /// Gets the service name for which the given <paramref name="serviceType"/> will be registered.
        /// </summary>
        /// <param name="serviceType">The service type for which to provide a service name.</param>
        /// <param name="implementingType">The implementing type for which to provide a service name.</param>
        /// <returns>The service name for which the <paramref name="serviceType"/> and <paramref name="implementingType"/> will be registered.</returns>
        string GetServiceName(Type serviceType, Type implementingType);
    }

    /// <summary>
    /// Represents a class that is responsible for executing an <see cref="ICompositionRoot"/> and making
    /// sure that we don't execute the same composition root twice.
    /// </summary>
    internal interface ICompositionRootExecutor
    {
        /// <summary>
        /// Creates an instance of the <paramref name="compositionRootType"/> and executes the <see cref="ICompositionRoot.Compose"/> method.
        /// </summary>
        /// <param name="compositionRootType">The concrete <see cref="ICompositionRoot"/> type to be instantiated and executed.</param>
        void Execute(Type compositionRootType);

        /// <summary>
        /// Executes the <see cref="ICompositionRoot.Compose"/> method.
        /// </summary>
        /// <typeparam name="TCompositionRoot">The type of <see cref="ICompositionRoot"/> to register from.</typeparam>
        /// <param name="compositionRoot">The <see cref="ICompositionRoot"/> to be executed.</param>
        void Execute<TCompositionRoot>(TCompositionRoot compositionRoot)
            where TCompositionRoot : ICompositionRoot;
    }

    /// <summary>
    /// Represents an abstraction of the <see cref="ILGenerator"/> class that provides information
    /// about the <see cref="Type"/> currently on the stack.
    /// </summary>
    internal interface IEmitter
    {
        /// <summary>
        /// Gets the <see cref="Type"/> currently on the stack.
        /// </summary>
        Type StackType { get; }

        /// <summary>
        /// Gets a list containing each <see cref="Instruction"/> to be emitted into the dynamic method.
        /// </summary>
        List<Instruction> Instructions { get; }

        /// <summary>
        /// Puts the specified instruction onto the stream of instructions.
        /// </summary>
        /// <param name="code">The Microsoft Intermediate Language (MSIL) instruction to be put onto the stream.</param>
        void Emit(OpCode code);

        /// <summary>
        /// Puts the specified instruction onto the Microsoft intermediate language (MSIL) stream followed by the metadata token for the given string.
        /// </summary>
        /// <param name="code">The MSIL instruction to be emitted onto the stream.</param>
        /// <param name="arg">The String to be emitted.</param>
        void Emit(OpCode code, string arg);

#if NETSTANDARD1_1 || NETSTANDARD1_3 || NETSTANDARD1_6 || NETSTANDARD2_0

        /// <summary>
        /// Pushes the argument as a constant expression.
        /// </summary>
        /// <param name="arg">The argument value to be pushed.</param>
        /// <param name="type">The type of the argument to be pushed.</param>
        void PushConstantValue(object arg, Type type);
#endif

        /// <summary>
        /// Puts the specified instruction and numerical argument onto the Microsoft intermediate language (MSIL) stream of instructions.
        /// </summary>
        /// <param name="code">The MSIL instruction to be put onto the stream.</param>
        /// <param name="arg">The numerical argument pushed onto the stream immediately after the instruction.</param>
        void Emit(OpCode code, int arg);

        /// <summary>
        /// Puts the specified instruction and numerical argument onto the Microsoft intermediate language (MSIL) stream of instructions.
        /// </summary>
        /// <param name="code">The MSIL instruction to be put onto the stream.</param>
        /// <param name="arg">The numerical argument pushed onto the stream immediately after the instruction.</param>
        void Emit(OpCode code, long arg);

        /// <summary>
        /// Puts the specified instruction and numerical argument onto the Microsoft intermediate language (MSIL) stream of instructions.
        /// </summary>
        /// <param name="code">The MSIL instruction to be put onto the stream.</param>
        /// <param name="arg">The numerical argument pushed onto the stream immediately after the instruction.</param>
        void Emit(OpCode code, sbyte arg);

        /// <summary>
        /// Puts the specified instruction and numerical argument onto the Microsoft intermediate language (MSIL) stream of instructions.
        /// </summary>
        /// <param name="code">The MSIL instruction to be put onto the stream.</param>
        /// <param name="arg">The numerical argument pushed onto the stream immediately after the instruction.</param>
        void Emit(OpCode code, byte arg);

        /// <summary>
        /// Puts the specified instruction onto the Microsoft intermediate language (MSIL) stream followed by the metadata token for the given type.
        /// </summary>
        /// <param name="code">The MSIL instruction to be put onto the stream.</param>
        /// <param name="type">A <see cref="Type"/> representing the type metadata token.</param>
        void Emit(OpCode code, Type type);

        /// <summary>
        /// Puts the specified instruction and metadata token for the specified constructor onto the Microsoft intermediate language (MSIL) stream of instructions.
        /// </summary>
        /// <param name="code">The MSIL instruction to be emitted onto the stream.</param>
        /// <param name="constructor">A <see cref="ConstructorInfo"/> representing a constructor.</param>
        void Emit(OpCode code, ConstructorInfo constructor);

        /// <summary>
        /// Puts the specified instruction onto the Microsoft intermediate language (MSIL) stream followed by the index of the given local variable.
        /// </summary>
        /// <param name="code">The MSIL instruction to be emitted onto the stream.</param>
        /// <param name="localBuilder">A local variable.</param>
        void Emit(OpCode code, LocalBuilder localBuilder);

        /// <summary>
        /// Puts the specified instruction onto the Microsoft intermediate language (MSIL) stream followed by the metadata token for the given method.
        /// </summary>
        /// <param name="code">The MSIL instruction to be emitted onto the stream.</param>
        /// <param name="methodInfo">A <see cref="MethodInfo"/> representing a method.</param>
        void Emit(OpCode code, MethodInfo methodInfo);

        /// <summary>
        /// Declares a local variable of the specified type.
        /// </summary>
        /// <param name="type">A <see cref="Type"/> object that represents the type of the local variable.</param>
        /// <returns>The declared local variable.</returns>
        LocalBuilder DeclareLocal(Type type);
    }

    /// <summary>
    /// Represents a dynamic method skeleton for emitting the code needed to resolve a service instance.
    /// </summary>
    internal interface IMethodSkeleton
    {
        /// <summary>
        /// Gets the <see cref="IEmitter"/> for the this dynamic method.
        /// </summary>
        /// <returns>The <see cref="IEmitter"/> for this dynamic method.</returns>
        IEmitter GetEmitter();

        /// <summary>
        /// Completes the dynamic method and creates a delegate that can be used to execute it.
        /// </summary>
        /// <param name="delegateType">A delegate type whose signature matches that of the dynamic method.</param>
        /// <returns>A delegate of the specified type, which can be used to execute the dynamic method.</returns>
        Delegate CreateDelegate(Type delegateType);
    }

    /// <summary>
    /// Represents a class that is capable of providing a <see cref="IScopeManager"/>.
    /// </summary>
    internal interface IScopeManagerProvider
    {
        /// <summary>
        /// Returns the <see cref="IScopeManager"/> that is responsible for managing scopes.
        /// </summary>
        /// <param name="serviceFactory">The <see cref="IServiceFactory"/> to be associated with this <see cref="ScopeManager"/>.</param>
        /// <returns>The <see cref="IScopeManager"/> that is responsible for managing scopes.</returns>
        IScopeManager GetScopeManager(IServiceFactory serviceFactory);
    }

    /// <summary>
    /// This class is not for public use and is used internally
    /// to load runtime arguments onto the evaluation stack.
    /// </summary>
    internal static class RuntimeArgumentsLoader
    {
        /// <summary>
        /// Loads the runtime arguments onto the evaluation stack.
        /// </summary>
        /// <param name="constants">A object array representing the dynamic method context.</param>
        /// <returns>An array containing the runtime arguments supplied when resolving the service.</returns>
        public static object[] Load(object[] constants)
        {
            if (!(constants[constants.Length - 1] is object[] arguments))
            {
                return new object[] { };
            }

            return arguments;
        }
    }

    /// <summary>
    /// Contains a set of helper method related to validating
    /// user input.
    /// </summary>
    internal static class Ensure
    {
        /// <summary>
        /// Ensures that the given <paramref name="value"/> is not null.
        /// </summary>
        /// <typeparam name="T">The type of value to be validated.</typeparam>
        /// <param name="value">The value to be validated.</param>
        /// <param name="paramName">The name of the parameter from which the <paramref name="value"/> comes from.</param>
        public static void IsNotNull<T>(T value, string paramName)
        {
            if (value == null)
            {
                throw new ArgumentNullException(paramName);
            }
        }
    }

    /// <summary>
    /// Extends the <see cref="IServiceRegistry"/> interface with a
    /// set of convenience methods for registering services.
    /// </summary>
    internal static class ServiceRegistryExtensions
    {
        /// <summary>
        /// Registers the <paramref name="serviceType"/> using the non-generic <paramref name="factory"/> to resolve the instance.
        /// </summary>
        /// <param name="serviceRegistry">The target <see cref="IServiceRegistry"/>.</param>
        /// <param name="serviceType">The type of service to register.</param>
        /// <param name="factory">The factory used to resolve the instance.</param>
        /// <returns>The <see cref="IServiceRegistry"/>, for chaining calls.</returns>
        public static IServiceRegistry Register(this IServiceRegistry serviceRegistry, Type serviceType, Func<IServiceFactory, object> factory)
            => Register(serviceRegistry, serviceType, factory, string.Empty, null);

        /// <summary>
        /// Registers the <paramref name="serviceType"/> with a given <paramref name="lifetime"/> using the non-generic <paramref name="factory"/> to resolve the instance.
        /// </summary>
        /// <param name="serviceRegistry">The target <see cref="IServiceRegistry"/>.</param>
        /// <param name="serviceType">The type of service to register.</param>
        /// <param name="factory">The factory used to resolve the instance.</param>
        /// <param name="lifetime">The <see cref="ILifetime"/> used to control the lifetime of the service.</param>
        /// <returns>The <see cref="IServiceRegistry"/>, for chaining calls.</returns>
        public static IServiceRegistry Register(this IServiceRegistry serviceRegistry, Type serviceType, Func<IServiceFactory, object> factory, ILifetime lifetime)
            => Register(serviceRegistry, serviceType, factory, string.Empty, lifetime);

        /// <summary>
        /// Registers the <paramref name="serviceType"/> using the non-generic <paramref name="factory"/> to resolve the instance.
        /// </summary>
        /// <param name="serviceRegistry">The target <see cref="IServiceRegistry"/>.</param>
        /// <param name="serviceType">The type of service to register.</param>
        /// <param name="factory">The factory used to resolve the instance.</param>
        /// <param name="serviceName">The name the service to register.</param>
        /// <returns>The <see cref="IServiceRegistry"/>, for chaining calls.</returns>
        public static IServiceRegistry Register(this IServiceRegistry serviceRegistry, Type serviceType, Func<IServiceFactory, object> factory, string serviceName)
            => Register(serviceRegistry, serviceType, factory, serviceName, null);

        /// <summary>
        /// Registers the <paramref name="serviceType"/> with a given <paramref name="lifetime"/> using the non-generic <paramref name="factory"/> to resolve the instance.
        /// </summary>
        /// <param name="serviceRegistry">The target <see cref="IServiceRegistry"/>.</param>
        /// <param name="serviceType">The type of service to register.</param>
        /// <param name="factory">The factory used to resolve the instance.</param>
        /// <param name="serviceName">The name the service to register.</param>
        /// <param name="lifetime">The <see cref="ILifetime"/> used to control the lifetime of the service.</param>
        /// <returns>The <see cref="IServiceRegistry"/>, for chaining calls.</returns>
        public static IServiceRegistry Register(this IServiceRegistry serviceRegistry, Type serviceType, Func<IServiceFactory, object> factory, string serviceName, ILifetime lifetime)
        {
            var serviceRegistration = new ServiceRegistration
            {
                FactoryExpression = factory,
                ServiceType = serviceType,
                ServiceName = serviceName,
                Lifetime = lifetime,
            };
            return serviceRegistry.Register(serviceRegistration);
        }

        /// <summary>
        /// Registers a singleton <paramref name="serviceType"/> using the non-generic <paramref name="factory"/> to resolve the instance.
        /// </summary>
        /// <param name="serviceRegistry">The target <see cref="IServiceRegistry"/>.</param>
        /// <param name="serviceType">The type of service to register.</param>
        /// <param name="factory">The factory used to resolve the instance.</param>
        /// <returns>The <see cref="IServiceRegistry"/>, for chaining calls.</returns>
        public static IServiceRegistry RegisterSingleton(this IServiceRegistry serviceRegistry, Type serviceType, Func<IServiceFactory, object> factory) =>
            serviceRegistry.RegisterSingleton(serviceType, factory, string.Empty);

        /// <summary>
        /// Registers a singleton <paramref name="serviceType"/> using the non-generic <paramref name="factory"/> to resolve the instance.
        /// </summary>
        /// <param name="serviceRegistry">The target <see cref="IServiceRegistry"/>.</param>
        /// <param name="serviceType">The type of service to register.</param>
        /// <param name="factory">The factory used to resolve the instance.</param>
        /// <param name="serviceName">The name the service to register.</param>
        /// <returns>The <see cref="IServiceRegistry"/>, for chaining calls.</returns>
        public static IServiceRegistry RegisterSingleton(this IServiceRegistry serviceRegistry, Type serviceType, Func<IServiceFactory, object> factory, string serviceName) =>
            serviceRegistry.Register(serviceType, factory, serviceName, new PerContainerLifetime());

        /// <summary>
        /// Registers a scoped <paramref name="serviceType"/> using the non-generic <paramref name="factory"/> to resolve the instance.
        /// </summary>
        /// <param name="serviceRegistry">The target <see cref="IServiceRegistry"/>.</param>
        /// <param name="serviceType">The type of service to register.</param>
        /// <param name="factory">The factory used to resolve the instance.</param>
        /// <returns>The <see cref="IServiceRegistry"/>, for chaining calls.</returns>
        public static IServiceRegistry RegisterScoped(this IServiceRegistry serviceRegistry, Type serviceType, Func<IServiceFactory, object> factory) =>
            serviceRegistry.RegisterScoped(serviceType, factory, string.Empty);

        /// <summary>
        /// Registers a scoped <paramref name="serviceType"/> using the non-generic <paramref name="factory"/> to resolve the instance.
        /// </summary>
        /// <param name="serviceRegistry">The target <see cref="IServiceRegistry"/>.</param>
        /// <param name="serviceType">The type of service to register.</param>
        /// <param name="factory">The factory used to resolve the instance.</param>
        /// <param name="serviceName">The name the service to register.</param>
        /// <returns>The <see cref="IServiceRegistry"/>, for chaining calls.</returns>
        public static IServiceRegistry RegisterScoped(this IServiceRegistry serviceRegistry, Type serviceType, Func<IServiceFactory, object> factory, string serviceName) =>
            serviceRegistry.Register(serviceType, factory, serviceName, new PerScopeLifetime());

        /// <summary>
        /// Registers a transient <paramref name="serviceType"/> using the non-generic <paramref name="factory"/> to resolve the instance.
        /// </summary>
        /// <param name="serviceRegistry">The target <see cref="IServiceRegistry"/>.</param>
        /// <param name="serviceType">The type of service to register.</param>
        /// <param name="factory">The factory used to resolve the instance.</param>
        /// <returns>The <see cref="IServiceRegistry"/>, for chaining calls.</returns>
        public static IServiceRegistry RegisterTransient(this IServiceRegistry serviceRegistry, Type serviceType, Func<IServiceFactory, object> factory) =>
            serviceRegistry.RegisterTransient(serviceType, factory, string.Empty);

        /// <summary>
        /// Registers a transient <paramref name="serviceType"/> using the non-generic <paramref name="factory"/> to resolve the instance.
        /// </summary>
        /// <param name="serviceRegistry">The target <see cref="IServiceRegistry"/>.</param>
        /// <param name="serviceType">The type of service to register.</param>
        /// <param name="factory">The factory used to resolve the instance.</param>
        /// <param name="serviceName">The name the service to register.</param>
        /// <returns>The <see cref="IServiceRegistry"/>, for chaining calls.</returns>
        public static IServiceRegistry RegisterTransient(this IServiceRegistry serviceRegistry, Type serviceType, Func<IServiceFactory, object> factory, string serviceName) =>
            serviceRegistry.Register(serviceType, factory, serviceName);

        /// <summary>
        /// Registers a singleton service of type <typeparamref name="TService"/> with an implementing type of <typeparamref name="TImplementation"/>.
        /// </summary>
        /// <param name="serviceRegistry">The target <see cref="IServiceRegistry"/>.</param>
        /// <typeparam name="TService">The type of service to register.</typeparam>
        /// <typeparam name="TImplementation">The type implementing the service type.</typeparam>
        /// <returns>The <see cref="IServiceRegistry"/>, for chaining calls.</returns>
        public static IServiceRegistry RegisterSingleton<TService, TImplementation>(this IServiceRegistry serviceRegistry)
            where TImplementation : TService
            => serviceRegistry.Register<TService, TImplementation>(new PerContainerLifetime());

        /// <summary>
        /// Registers a singleton service of type <typeparamref name="TService"/> as a concrete service.
        /// </summary>
        /// <param name="serviceRegistry">The target <see cref="IServiceRegistry"/>.</param>
        /// <typeparam name="TService">The type of service to register.</typeparam>
        /// <returns>The <see cref="IServiceRegistry"/>, for chaining calls.</returns>
        public static IServiceRegistry RegisterSingleton<TService>(this IServiceRegistry serviceRegistry)
            => serviceRegistry.Register<TService>(new PerContainerLifetime());

        /// <summary>
        /// Registers a singleton service of type <typeparamref name="TService"/> with an implementing type of <typeparamref name="TImplementation"/>.
        /// </summary>
        /// <param name="serviceRegistry">The target <see cref="IServiceRegistry"/>.</param>
        /// <param name="serviceName">The name of the service to register.</param>
        /// <typeparam name="TService">The type of service to register.</typeparam>
        /// <typeparam name="TImplementation">The type implementing the service type.</typeparam>
        /// <returns>The <see cref="IServiceRegistry"/>, for chaining calls.</returns>
        public static IServiceRegistry RegisterSingleton<TService, TImplementation>(this IServiceRegistry serviceRegistry, string serviceName)
            where TImplementation : TService
            => serviceRegistry.Register<TService, TImplementation>(serviceName, new PerContainerLifetime());

        /// <summary>
        /// Registers a singleton service of type <paramref name="serviceType"/> with an implementing type of <paramref name="implementingType"/>.
        /// </summary>
        /// <param name="serviceRegistry">The target <see cref="IServiceRegistry"/>.</param>
        /// <param name="serviceType">The type of service to register.</param>
        /// <param name="implementingType">The type implementing the service type.</param>
        /// <returns>The <see cref="IServiceRegistry"/>, for chaining calls.</returns>
        public static IServiceRegistry RegisterSingleton(this IServiceRegistry serviceRegistry, Type serviceType, Type implementingType)
            => serviceRegistry.Register(serviceType, implementingType, new PerContainerLifetime());

        /// <summary>
        /// Registers a singleton service of type <paramref name="serviceType"/> as a concrete service type.
        /// </summary>
        /// <param name="serviceRegistry">The target <see cref="IServiceRegistry"/>.</param>
        /// <param name="serviceType">The type of service to register.</param>
        /// <returns>The <see cref="IServiceRegistry"/>, for chaining calls.</returns>
        public static IServiceRegistry RegisterSingleton(this IServiceRegistry serviceRegistry, Type serviceType)
            => serviceRegistry.Register(serviceType, new PerContainerLifetime());

        /// <summary>
        /// Registers a singleton service of type <paramref name="serviceType"/> with an implementing type of <paramref name="implementingType"/>.
        /// </summary>
        /// <param name="serviceRegistry">The target <see cref="IServiceRegistry"/>.</param>
        /// <param name="serviceType">The type of service to register.</param>
        /// <param name="implementingType">The type implementing the service type.</param>
        /// <param name="serviceName">The name of the service to register.</param>
        /// <returns>The <see cref="IServiceRegistry"/>, for chaining calls.</returns>
        public static IServiceRegistry RegisterSingleton(this IServiceRegistry serviceRegistry, Type serviceType, Type implementingType, string serviceName)
            => serviceRegistry.Register(serviceType, implementingType, serviceName, new PerContainerLifetime());

        /// <summary>
        /// Registers a singleton service of type <typeparamref name="TService"/> using a factory function.
        /// </summary>
        /// <param name="serviceRegistry">The target <see cref="IServiceRegistry"/>.</param>
        /// <param name="factory">The factory function used to create the service instance.</param>
        /// <typeparam name="TService">The type of service to register.</typeparam>
        /// <returns>The <see cref="IServiceRegistry"/>, for chaining calls.</returns>
        public static IServiceRegistry RegisterSingleton<TService>(this IServiceRegistry serviceRegistry, Func<IServiceFactory, TService> factory)
            => serviceRegistry.Register<TService>(factory, new PerContainerLifetime());

        /// <summary>
        /// Registers a singleton service of type <typeparamref name="TService"/> using a factory function.
        /// </summary>
        /// <param name="serviceRegistry">The target <see cref="IServiceRegistry"/>.</param>
        /// <param name="factory">The factory function used to create the service instance.</param>
        /// <param name="serviceName">The name of the service to register.</param>
        /// <typeparam name="TService">The type of service to register.</typeparam>
        /// <returns>The <see cref="IServiceRegistry"/>, for chaining calls.</returns>
        public static IServiceRegistry RegisterSingleton<TService>(this IServiceRegistry serviceRegistry, Func<IServiceFactory, TService> factory, string serviceName)
            => serviceRegistry.Register<TService>(factory, serviceName, new PerContainerLifetime());

        /// <summary>
        /// Registers a scoped service of type <typeparamref name="TService"/> with an implementing type of <typeparamref name="TImplementation"/>.
        /// </summary>
        /// <param name="serviceRegistry">The target <see cref="IServiceRegistry"/>.</param>
        /// <typeparam name="TService">The type of service to register.</typeparam>
        /// <typeparam name="TImplementation">The type implementing the service type.</typeparam>
        /// <returns>The <see cref="IServiceRegistry"/>, for chaining calls.</returns>
        public static IServiceRegistry RegisterScoped<TService, TImplementation>(this IServiceRegistry serviceRegistry)
            where TImplementation : TService
            => serviceRegistry.Register<TService, TImplementation>(new PerScopeLifetime());

        /// <summary>
        /// Registers a scoped service of type <typeparamref name="TService"/> as a concrete service.
        /// </summary>
        /// <param name="serviceRegistry">The target <see cref="IServiceRegistry"/>.</param>
        /// <typeparam name="TService">The type of service to register.</typeparam>
        /// <returns>The <see cref="IServiceRegistry"/>, for chaining calls.</returns>
        public static IServiceRegistry RegisterScoped<TService>(this IServiceRegistry serviceRegistry)
            => serviceRegistry.Register<TService>(new PerScopeLifetime());

        /// <summary>
        /// Registers a scoped service of type <typeparamref name="TService"/> with an implementing type of <typeparamref name="TImplementation"/>.
        /// </summary>
        /// <param name="serviceRegistry">The target <see cref="IServiceRegistry"/>.</param>
        /// <param name="serviceName">The name of the service to register.</param>
        /// <typeparam name="TService">The type of service to register.</typeparam>
        /// <typeparam name="TImplementation">The type implementing the service type.</typeparam>
        /// <returns>The <see cref="IServiceRegistry"/>, for chaining calls.</returns>
        public static IServiceRegistry RegisterScoped<TService, TImplementation>(this IServiceRegistry serviceRegistry, string serviceName)
            where TImplementation : TService
            => serviceRegistry.Register<TService, TImplementation>(serviceName, new PerScopeLifetime());

        /// <summary>
        /// Registers a scoped service of type <paramref name="serviceType"/> with an implementing type of <paramref name="implementingType"/>.
        /// </summary>
        /// <param name="serviceRegistry">The target <see cref="IServiceRegistry"/>.</param>
        /// <param name="serviceType">The type of service to register.</param>
        /// <param name="implementingType">The type implementing the service type.</param>
        /// <returns>The <see cref="IServiceRegistry"/>, for chaining calls.</returns>
        public static IServiceRegistry RegisterScoped(this IServiceRegistry serviceRegistry, Type serviceType, Type implementingType)
            => serviceRegistry.Register(serviceType, implementingType, new PerScopeLifetime());

        /// <summary>
        /// Registers a scoped service of type <paramref name="serviceType"/> as a concrete service type.
        /// </summary>
        /// <param name="serviceRegistry">The target <see cref="IServiceRegistry"/>.</param>
        /// <param name="serviceType">The type of service to register.</param>
        /// <returns>The <see cref="IServiceRegistry"/>, for chaining calls.</returns>
        public static IServiceRegistry RegisterScoped(this IServiceRegistry serviceRegistry, Type serviceType)
            => serviceRegistry.Register(serviceType, new PerScopeLifetime());

        /// <summary>
        /// Registers a scoped service of type <paramref name="serviceType"/> with an implementing type of <paramref name="implementingType"/>.
        /// </summary>
        /// <param name="serviceRegistry">The target <see cref="IServiceRegistry"/>.</param>
        /// <param name="serviceType">The type of service to register.</param>
        /// <param name="implementingType">The type implementing the service type.</param>
        /// <param name="serviceName">The name of the service to register.</param>
        /// <returns>The <see cref="IServiceRegistry"/>, for chaining calls.</returns>
        public static IServiceRegistry RegisterScoped(this IServiceRegistry serviceRegistry, Type serviceType, Type implementingType, string serviceName)
            => serviceRegistry.Register(serviceType, implementingType, serviceName, new PerScopeLifetime());

        /// <summary>
        /// Registers a scoped service of type <typeparamref name="TService"/> using a factory function.
        /// </summary>
        /// <param name="serviceRegistry">The target <see cref="IServiceRegistry"/>.</param>
        /// <param name="factory">The factory function used to create the service instance.</param>
        /// <typeparam name="TService">The type of service to register.</typeparam>
        /// <returns>The <see cref="IServiceRegistry"/>, for chaining calls.</returns>
        public static IServiceRegistry RegisterScoped<TService>(this IServiceRegistry serviceRegistry, Func<IServiceFactory, TService> factory)
            => serviceRegistry.Register<TService>(factory, new PerScopeLifetime());

        /// <summary>
        /// Registers a scoped service of type <typeparamref name="TService"/> using a factory function.
        /// </summary>
        /// <param name="serviceRegistry">The target <see cref="IServiceRegistry"/>.</param>
        /// <param name="factory">The factory function used to create the service instance.</param>
        /// <param name="serviceName">The name of the service to register.</param>
        /// <typeparam name="TService">The type of service to register.</typeparam>
        /// <returns>The <see cref="IServiceRegistry"/>, for chaining calls.</returns>
        public static IServiceRegistry RegisterScoped<TService>(this IServiceRegistry serviceRegistry, Func<IServiceFactory, TService> factory, string serviceName)
            => serviceRegistry.Register<TService>(factory, serviceName, new PerScopeLifetime());

        /// <summary>
        /// Registers a transient service of type <typeparamref name="TService"/> with an implementing type of <typeparamref name="TImplementation"/>.
        /// </summary>
        /// <param name="serviceRegistry">The target <see cref="IServiceRegistry"/>.</param>
        /// <typeparam name="TService">The type of service to register.</typeparam>
        /// <typeparam name="TImplementation">The type implementing the service type.</typeparam>
        /// <returns>The <see cref="IServiceRegistry"/>, for chaining calls.</returns>
        public static IServiceRegistry RegisterTransient<TService, TImplementation>(this IServiceRegistry serviceRegistry)
            where TImplementation : TService
            => serviceRegistry.Register<TService, TImplementation>();

        /// <summary>
        /// Registers a transient service of type <typeparamref name="TService"/> as a concrete service.
        /// </summary>
        /// <param name="serviceRegistry">The target <see cref="IServiceRegistry"/>.</param>
        /// <typeparam name="TService">The type of service to register.</typeparam>
        /// <returns>The <see cref="IServiceRegistry"/>, for chaining calls.</returns>
        public static IServiceRegistry RegisterTransient<TService>(this IServiceRegistry serviceRegistry)
            => serviceRegistry.Register<TService>();

        /// <summary>
        /// Registers a transient service of type <typeparamref name="TService"/> with an implementing type of <typeparamref name="TImplementation"/>.
        /// </summary>
        /// <param name="serviceRegistry">The target <see cref="IServiceRegistry"/>.</param>
        /// <param name="serviceName">The name of the service to register.</param>
        /// <typeparam name="TService">The type of service to register.</typeparam>
        /// <typeparam name="TImplementation">The type implementing the service type.</typeparam>
        /// <returns>The <see cref="IServiceRegistry"/>, for chaining calls.</returns>
        public static IServiceRegistry RegisterTransient<TService, TImplementation>(this IServiceRegistry serviceRegistry, string serviceName)
            where TImplementation : TService
            => serviceRegistry.Register<TService, TImplementation>(serviceName);

        /// <summary>
        /// Registers a transient service of type <paramref name="serviceType"/> with an implementing type of <paramref name="implementingType"/>.
        /// </summary>
        /// <param name="serviceRegistry">The target <see cref="IServiceRegistry"/>.</param>
        /// <param name="serviceType">The type of service to register.</param>
        /// <param name="implementingType">The type implementing the service type.</param>
        /// <returns>The <see cref="IServiceRegistry"/>, for chaining calls.</returns>
        public static IServiceRegistry RegisterTransient(this IServiceRegistry serviceRegistry, Type serviceType, Type implementingType)
            => serviceRegistry.Register(serviceType, implementingType);

        /// <summary>
        /// Registers a singleton service of type <paramref name="serviceType"/> as a concrete service type.
        /// </summary>
        /// <param name="serviceRegistry">The target <see cref="IServiceRegistry"/>.</param>
        /// <param name="serviceType">The type of service to register.</param>
        /// <returns>The <see cref="IServiceRegistry"/>, for chaining calls.</returns>
        public static IServiceRegistry RegisterTransient(this IServiceRegistry serviceRegistry, Type serviceType)
            => serviceRegistry.Register(serviceType);

        /// <summary>
        /// Registers a transient service of type <paramref name="serviceType"/> with an implementing type of <paramref name="implementingType"/>.
        /// </summary>
        /// <param name="serviceRegistry">The target <see cref="IServiceRegistry"/>.</param>
        /// <param name="serviceType">The type of service to register.</param>
        /// <param name="implementingType">The type implementing the service type.</param>
        /// <param name="serviceName">The name of the service to register.</param>
        /// <returns>The <see cref="IServiceRegistry"/>, for chaining calls.</returns>
        public static IServiceRegistry RegisterTransient(this IServiceRegistry serviceRegistry, Type serviceType, Type implementingType, string serviceName)
            => serviceRegistry.Register(serviceType, implementingType, serviceName);

        /// <summary>
        /// Registers a transient service of type <typeparamref name="TService"/> using a factory function.
        /// </summary>
        /// <param name="serviceRegistry">The target <see cref="IServiceRegistry"/>.</param>
        /// <param name="factory">The factory function used to create the service instance.</param>
        /// <typeparam name="TService">The type of service to register.</typeparam>
        /// <returns>The <see cref="IServiceRegistry"/>, for chaining calls.</returns>
        public static IServiceRegistry RegisterTransient<TService>(this IServiceRegistry serviceRegistry, Func<IServiceFactory, TService> factory)
            => serviceRegistry.Register<TService>(factory);

        /// <summary>
        /// Registers a transient service of type <typeparamref name="TService"/> using a factory function.
        /// </summary>
        /// <param name="serviceRegistry">The target <see cref="IServiceRegistry"/>.</param>
        /// <param name="factory">The factory function used to create the service instance.</param>
        /// <param name="serviceName">The name of the service to register.</param>
        /// <typeparam name="TService">The type of service to register.</typeparam>
        /// <returns>The <see cref="IServiceRegistry"/>, for chaining calls.</returns>
        public static IServiceRegistry RegisterTransient<TService>(this IServiceRegistry serviceRegistry, Func<IServiceFactory, TService> factory, string serviceName)
            => serviceRegistry.Register<TService>(factory, serviceName);

        /// <summary>
        /// Allows a registered service to be overridden by another <see cref="ServiceRegistration"/>.
        /// Allows the registered <typeparamref name="TService"/> to be overridden by <typeparamref name="TImplementation"/>.
        /// </summary>
        /// <param name="serviceRegistry">The target <see cref="IServiceRegistry"/>.</param>
        /// <typeparam name="TService">The type of service to override.</typeparam>
        /// <typeparam name="TImplementation">The implementing type used to override the current implementing type.</typeparam>
        /// <returns>The <see cref="IServiceRegistry"/>, for chaining calls.</returns>
        public static IServiceRegistry Override<TService, TImplementation>(this IServiceRegistry serviceRegistry)
            where TImplementation : TService
        {
            return serviceRegistry.Override(sr => sr.ServiceType == typeof(TService), (serviceFactory, registration) =>
            {
                registration.FactoryExpression = null;
                registration.ImplementingType = typeof(TImplementation);
                return registration;
            });
        }

        /// <summary>
        /// Allows a registered service to be overridden by another <see cref="ServiceRegistration"/>.
        /// Allows the registered <typeparamref name="TService"/> to be overridden by <typeparamref name="TImplementation"/>.
        /// </summary>
        /// <param name="serviceRegistry">The target <see cref="IServiceRegistry"/>.</param>
        /// <param name="lifetime">The <see cref="ILifetime"/> to be used when overriding the service.</param>
        /// <typeparam name="TService">The type of service to override.</typeparam>
        /// <typeparam name="TImplementation">The implementing type used to override the current implementing type.</typeparam>
        /// <returns>The <see cref="IServiceRegistry"/>, for chaining calls.</returns>
        public static IServiceRegistry Override<TService, TImplementation>(this IServiceRegistry serviceRegistry, ILifetime lifetime)
            where TImplementation : TService
        {
            return serviceRegistry.Override(sr => sr.ServiceType == typeof(TService), (serviceFactory, registration) =>
            {
                registration.FactoryExpression = null;
                registration.ImplementingType = typeof(TImplementation);
                registration.Lifetime = lifetime;
                return registration;
            });
        }

        /// <summary>
        /// Allows post-processing of a service instance.
        /// </summary>
        /// <param name="serviceRegistry">The target <see cref="IServiceRegistry"/>.</param>
        /// <param name="processor">An action delegate that exposes the created service instance.</param>
        /// <typeparam name="TService">The type of service to initialize.</typeparam>
        /// <returns>The <see cref="IServiceRegistry"/>, for chaining calls.</returns>
        public static IServiceRegistry Initialize<TService>(this IServiceRegistry serviceRegistry, Action<IServiceFactory, TService> processor)
            => serviceRegistry.Initialize(sr => sr.ServiceType == typeof(TService), (factory, instance) => processor(factory, (TService)instance));
    }

    /// <summary>
    /// Extends the <see cref="IServiceFactory"/> interface.
    /// </summary>
    internal static class ServiceFactoryExtensions
    {
        /// <summary>
        /// Gets an instance of the given <typeparamref name="TService"/> type.
        /// </summary>
        /// <typeparam name="TService">The type of the requested service.</typeparam>
        /// <param name="factory">The target <see cref="IServiceFactory"/>.</param>
        /// <returns>The requested service instance.</returns>
        public static TService GetInstance<TService>(this IServiceFactory factory)
            => (TService)factory.GetInstance(typeof(TService));

        /// <summary>
        /// Gets a named instance of the given <typeparamref name="TService"/>.
        /// </summary>
        /// <typeparam name="TService">The type of the requested service.</typeparam>
        /// <param name="factory">The target <see cref="IServiceFactory"/>.</param>
        /// <param name="serviceName">The name of the requested service.</param>
        /// <returns>The requested service instance.</returns>
        public static TService GetInstance<TService>(this IServiceFactory factory, string serviceName)
            => (TService)factory.GetInstance(typeof(TService), serviceName);

        /// <summary>
        /// Gets an instance of the given <typeparamref name="TService"/>.
        /// </summary>
        /// <typeparam name="T">The type of the argument.</typeparam>
        /// <typeparam name="TService">The type of the requested service.</typeparam>
        /// <param name="factory">The target <see cref="IServiceFactory"/>.</param>
        /// <param name="value">The argument value.</param>
        /// <returns>The requested service instance.</returns>
        public static TService GetInstance<T, TService>(this IServiceFactory factory, T value)
            => (TService)factory.GetInstance(typeof(TService), new object[] { value });

        /// <summary>
        /// Gets an instance of the given <typeparamref name="TService"/>.
        /// </summary>
        /// <typeparam name="T">The type of the parameter.</typeparam>
        /// <typeparam name="TService">The type of the requested service.</typeparam>
        /// <param name="factory">The target <see cref="IServiceFactory"/>.</param>
        /// <param name="value">The argument value.</param>
        /// <param name="serviceName">The name of the requested service.</param>
        /// <returns>The requested service instance.</returns>
        public static TService GetInstance<T, TService>(this IServiceFactory factory, T value, string serviceName)
            => (TService)factory.GetInstance(typeof(TService), serviceName, new object[] { value });

        /// <summary>
        /// Gets an instance of the given <typeparamref name="TService"/>.
        /// </summary>
        /// <typeparam name="T1">The type of the first parameter.</typeparam>
        /// <typeparam name="T2">The type of the second parameter.</typeparam>
        /// <typeparam name="TService">The type of the requested service.</typeparam>
        /// <param name="factory">The target <see cref="IServiceFactory"/>.</param>
        /// <param name="arg1">The first argument value.</param>
        /// <param name="arg2">The second argument value.</param>
        /// <returns>The requested service instance.</returns>
        public static TService GetInstance<T1, T2, TService>(this IServiceFactory factory, T1 arg1, T2 arg2)
            => (TService)factory.GetInstance(typeof(TService), new object[] { arg1, arg2 });

        /// <summary>
        /// Gets an instance of the given <typeparamref name="TService"/>.
        /// </summary>
        /// <typeparam name="T1">The type of the first parameter.</typeparam>
        /// <typeparam name="T2">The type of the second parameter.</typeparam>
        /// <typeparam name="TService">The type of the requested service.</typeparam>
        /// <param name="factory">The target <see cref="IServiceFactory"/>.</param>
        /// <param name="arg1">The first argument value.</param>
        /// <param name="arg2">The second argument value.</param>
        /// <param name="serviceName">The name of the requested service.</param>
        /// <returns>The requested service instance.</returns>
        public static TService GetInstance<T1, T2, TService>(this IServiceFactory factory, T1 arg1, T2 arg2, string serviceName)
            => (TService)factory.GetInstance(typeof(TService), serviceName, new object[] { arg1, arg2 });

        /// <summary>
        /// Gets an instance of the given <typeparamref name="TService"/>.
        /// </summary>
        /// <typeparam name="T1">The type of the first parameter.</typeparam>
        /// <typeparam name="T2">The type of the second parameter.</typeparam>
        /// <typeparam name="T3">The type of the third parameter.</typeparam>
        /// <typeparam name="TService">The type of the requested service.</typeparam>
        /// <param name="factory">The target <see cref="IServiceFactory"/>.</param>
        /// <param name="arg1">The first argument value.</param>
        /// <param name="arg2">The second argument value.</param>
        /// <param name="arg3">The third argument value.</param>
        /// <returns>The requested service instance.</returns>
        public static TService GetInstance<T1, T2, T3, TService>(this IServiceFactory factory, T1 arg1, T2 arg2, T3 arg3)
            => (TService)factory.GetInstance(typeof(TService), new object[] { arg1, arg2, arg3 });

        /// <summary>
        /// Gets an instance of the given <typeparamref name="TService"/>.
        /// </summary>
        /// <typeparam name="T1">The type of the first parameter.</typeparam>
        /// <typeparam name="T2">The type of the second parameter.</typeparam>
        /// <typeparam name="T3">The type of the third parameter.</typeparam>
        /// <typeparam name="TService">The type of the requested service.</typeparam>
        /// <param name="factory">The target <see cref="IServiceFactory"/>.</param>
        /// <param name="arg1">The first argument value.</param>
        /// <param name="arg2">The second argument value.</param>
        /// <param name="arg3">The third argument value.</param>
        /// <param name="serviceName">The name of the requested service.</param>
        /// <returns>The requested service instance.</returns>
        public static TService GetInstance<T1, T2, T3, TService>(this IServiceFactory factory, T1 arg1, T2 arg2, T3 arg3, string serviceName)
            => (TService)factory.GetInstance(typeof(TService), serviceName, new object[] { arg1, arg2, arg3 });

        /// <summary>
        /// Gets an instance of the given <typeparamref name="TService"/>.
        /// </summary>
        /// <typeparam name="T1">The type of the first parameter.</typeparam>
        /// <typeparam name="T2">The type of the second parameter.</typeparam>
        /// <typeparam name="T3">The type of the third parameter.</typeparam>
        /// <typeparam name="T4">The type of the fourth parameter.</typeparam>
        /// <typeparam name="TService">The type of the requested service.</typeparam>
        /// <param name="factory">The target <see cref="IServiceFactory"/>.</param>
        /// <param name="arg1">The first argument value.</param>
        /// <param name="arg2">The second argument value.</param>
        /// <param name="arg3">The third argument value.</param>
        /// <param name="arg4">The fourth argument value.</param>
        /// <returns>The requested service instance.</returns>
        public static TService GetInstance<T1, T2, T3, T4, TService>(this IServiceFactory factory, T1 arg1, T2 arg2, T3 arg3, T4 arg4)
            => (TService)factory.GetInstance(typeof(TService), new object[] { arg1, arg2, arg3, arg4 });

        /// <summary>
        /// Gets an instance of the given <typeparamref name="TService"/>.
        /// </summary>
        /// <typeparam name="T1">The type of the first parameter.</typeparam>
        /// <typeparam name="T2">The type of the second parameter.</typeparam>
        /// <typeparam name="T3">The type of the third parameter.</typeparam>
        /// <typeparam name="T4">The type of the fourth parameter.</typeparam>
        /// <typeparam name="TService">The type of the requested service.</typeparam>
        /// <param name="factory">The target <see cref="IServiceFactory"/>.</param>
        /// <param name="arg1">The first argument value.</param>
        /// <param name="arg2">The second argument value.</param>
        /// <param name="arg3">The third argument value.</param>
        /// <param name="arg4">The fourth argument value.</param>
        /// <param name="serviceName">The name of the requested service.</param>
        /// <returns>The requested service instance.</returns>
        public static TService GetInstance<T1, T2, T3, T4, TService>(this IServiceFactory factory, T1 arg1, T2 arg2, T3 arg3, T4 arg4, string serviceName)
            => (TService)factory.GetInstance(typeof(TService), serviceName, new object[] { arg1, arg2, arg3, arg4 });

        /// <summary>
        /// Tries to get an instance of the given <typeparamref name="TService"/> type.
        /// </summary>
        /// <typeparam name="TService">The type of the requested service.</typeparam>
        /// <param name="factory">The target <see cref="IServiceFactory"/>.</param>
        /// <returns>The requested service instance if available, otherwise default(T).</returns>
        public static TService TryGetInstance<TService>(this IServiceFactory factory)
            => (TService)factory.TryGetInstance(typeof(TService));

        /// <summary>
        /// Tries to get an instance of the given <typeparamref name="TService"/> type.
        /// </summary>
        /// <typeparam name="TService">The type of the requested service.</typeparam>
        /// <param name="factory">The target <see cref="IServiceFactory"/>.</param>
        /// <param name="serviceName">The name of the requested service.</param>
        /// <returns>The requested service instance if available, otherwise default(T).</returns>
        public static TService TryGetInstance<TService>(this IServiceFactory factory, string serviceName)
            => (TService)factory.TryGetInstance(typeof(TService), serviceName);

        /// <summary>
        /// Gets all instances of type <typeparamref name="TService"/>.
        /// </summary>
        /// <typeparam name="TService">The type of services to resolve.</typeparam>
        /// <param name="factory">The target <see cref="IServiceFactory"/>.</param>
        /// <returns>A list that contains all implementations of the <typeparamref name="TService"/> type.</returns>
        public static IEnumerable<TService> GetAllInstances<TService>(this IServiceFactory factory)
            => factory.GetInstance<IEnumerable<TService>>();

        /// <summary>
        /// Creates an instance of a concrete class.
        /// </summary>
        /// <typeparam name="TService">The type of class for which to create an instance.</typeparam>
        /// <param name="factory">The target <see cref="IServiceFactory"/>.</param>
        /// <returns>An instance of <typeparamref name="TService"/>.</returns>
        /// <remarks>The concrete type will be registered if not already registered with the container.</remarks>
        public static TService Create<TService>(this IServiceFactory factory)
            where TService : class
            => (TService)factory.Create(typeof(TService));
    }

    /// <summary>
    /// Extends the log delegate to simplify creating log entries.
    /// </summary>
    internal static class LogExtensions
    {
        /// <summary>
        /// Logs a new entry with the <see cref="LogLevel.Info"/> level.
        /// </summary>
        /// <param name="logAction">The log delegate.</param>
        /// <param name="message">The message to be logged.</param>
        public static void Info(this Action<LogEntry> logAction, string message) => logAction(new LogEntry(LogLevel.Info, message));

        /// <summary>
        /// Logs a new entry with the <see cref="LogLevel.Warning"/> level.
        /// </summary>
        /// <param name="logAction">The log delegate.</param>
        /// <param name="message">The message to be logged.</param>
        public static void Warning(this Action<LogEntry> logAction, string message) => logAction(new LogEntry(LogLevel.Warning, message));
    }

    /// <summary>
    /// Extends the <see cref="ImmutableHashTable{TKey,TValue}"/> class.
    /// </summary>
    internal static class ImmutableHashTableExtensions
    {
        /// <summary>
        /// Searches for a value using the given <paramref name="key"/>.
        /// </summary>
        /// <typeparam name="TKey">The type of the key.</typeparam>
        /// <typeparam name="TValue">The type of the value.</typeparam>
        /// <param name="hashTable">The target <see cref="ImmutableHashTable{TKey,TValue}"/> instance.</param>
        /// <param name="key">The key for which to search for a value.</param>
        /// <returns>If found, the <typeparamref name="TValue"/> with the given <paramref name="key"/>, otherwise the default <typeparamref name="TValue"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]

        // Excluded since this is a duplicate of the ImmutableHashTreeExtensions.Search method.
        [ExcludeFromCodeCoverage]
        public static TValue Search<TKey, TValue>(this ImmutableHashTable<TKey, TValue> hashTable, TKey key)
        {
            var hashCode = key.GetHashCode();
            var bucketIndex = hashCode & (hashTable.Divisor - 1);
            ImmutableHashTree<TKey, TValue> tree = hashTable.Buckets[bucketIndex];

            while (tree.Height != 0 && tree.HashCode != hashCode)
            {
                tree = hashCode < tree.HashCode ? tree.Left : tree.Right;
            }

            if (tree.Height != 0 && (ReferenceEquals(tree.Key, key) || Equals(tree.Key, key)))
            {
                return tree.Value;
            }

            if (tree.Duplicates.Items.Length > 0)
            {
                foreach (var keyValue in tree.Duplicates.Items)
                {
                    if (ReferenceEquals(keyValue.Key, key) || Equals(keyValue.Key, key))
                    {
                        return keyValue.Value;
                    }
                }
            }

            return default;
        }

        /// <summary>
        /// Adds a new element to the <see cref="ImmutableHashTree{TKey,TValue}"/>.
        /// </summary>
        /// <typeparam name="TKey">The type of the key.</typeparam>
        /// <typeparam name="TValue">The type of the value.</typeparam>
        /// <param name="hashTable">The target <see cref="ImmutableHashTable{TKey,TValue}"/>.</param>
        /// <param name="key">The key to be associated with the value.</param>
        /// <param name="value">The value to be added to the tree.</param>
        /// <returns>A new <see cref="ImmutableHashTree{TKey,TValue}"/> that contains the new key/value pair.</returns>
        public static ImmutableHashTable<TKey, TValue> Add<TKey, TValue>(this ImmutableHashTable<TKey, TValue> hashTable, TKey key, TValue value)
            => new ImmutableHashTable<TKey, TValue>(hashTable, key, value);

        // Excluded from coverage since it is equal to the generic version.
        [ExcludeFromCodeCoverage]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static GetInstanceDelegate Search(this ImmutableHashTable<Type, GetInstanceDelegate> hashTable, Type key)
        {
            var hashCode = key.GetHashCode();
            var bucketIndex = hashCode & (hashTable.Divisor - 1);

            ImmutableHashTree<Type, GetInstanceDelegate> tree = hashTable.Buckets[bucketIndex];

            while (tree.Height != 0 && tree.HashCode != hashCode)
            {
                tree = hashCode < tree.HashCode ? tree.Left : tree.Right;
            }

            if (tree.Height != 0 && ReferenceEquals(tree.Key, key))
            {
                return tree.Value;
            }

            if (tree.Duplicates.Items.Length > 0)
            {
                foreach (var keyValue in tree.Duplicates.Items)
                {
                    if (ReferenceEquals(keyValue.Key, key))
                    {
                        return keyValue.Value;
                    }
                }
            }

            return default;
        }
    }

    /// <summary>
    /// Extends the <see cref="ImmutableMapTree{TValue}"/> class.
    /// </summary>
    internal static class ImmutableMapTreeExtensions
    {
        /// <summary>
        /// Searches for a <typeparamref name="TValue"/> using the given <paramref name="key"/>.
        /// </summary>
        /// <typeparam name="TValue">The type of the value.</typeparam>
        /// <param name="tree">The target <see cref="ImmutableMapTree{TValue}"/>.</param>
        /// <param name="key">The key of the <see cref="ImmutableMapTree{TValue}"/> to get.</param>
        /// <returns>If found, the <typeparamref name="TValue"/> with the given <paramref name="key"/>, otherwise the default <typeparamref name="TValue"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TValue Search<TValue>(this ImmutableMapTree<TValue> tree, int key)
        {
            while (tree.Height != 0 && tree.Key != key)
            {
                tree = key < tree.Key ? tree.Left : tree.Right;
            }

            if (!tree.IsEmpty)
            {
                return tree.Value;
            }

            return default;
        }

        /// <summary>
        /// Adds a new element to the <see cref="ImmutableMapTree{TValue}"/>.
        /// </summary>
        /// <typeparam name="TValue">The type of the value.</typeparam>
        /// <param name="tree">The target <see cref="ImmutableMapTree{TValue}"/>.</param>
        /// <param name="key">The key to be associated with the value.</param>
        /// <param name="value">The value to be added to the tree.</param>
        /// <returns>A new <see cref="ImmutableMapTree{TValue}"/> that contains the new key/value pair.</returns>
        public static ImmutableMapTree<TValue> Add<TValue>(this ImmutableMapTree<TValue> tree, int key, TValue value)
        {
            if (tree.IsEmpty)
            {
                return new ImmutableMapTree<TValue>(key, value, tree, tree);
            }

            if (key > tree.Key)
            {
                return AddToRightBranch(tree, key, value);
            }

            if (key < tree.Key)
            {
                return AddToLeftBranch(tree, key, value);
            }

            return new ImmutableMapTree<TValue>(key, value, tree);
        }

        private static ImmutableMapTree<TValue> AddToLeftBranch<TValue>(ImmutableMapTree<TValue> tree, int key, TValue value)
        => new ImmutableMapTree<TValue>(tree.Key, tree.Value, tree.Left.Add(key, value), tree.Right);

        private static ImmutableMapTree<TValue> AddToRightBranch<TValue>(ImmutableMapTree<TValue> tree, int key, TValue value)
            => new ImmutableMapTree<TValue>(tree.Key, tree.Value, tree.Left, tree.Right.Add(key, value));
    }

    /// <summary>
    /// Extends the <see cref="ImmutableHashTree{TKey,TValue}"/> class.
    /// </summary>
    internal static class ImmutableHashTreeExtensions
    {
        /// <summary>
        /// Searches for a <typeparamref name="TValue"/> using the given <paramref name="key"/>.
        /// </summary>
        /// <typeparam name="TKey">The type of the key.</typeparam>
        /// <typeparam name="TValue">The type of the value.</typeparam>
        /// <param name="tree">The target <see cref="ImmutableHashTree{TKey,TValue}"/>.</param>
        /// <param name="key">The key of the <see cref="ImmutableHashTree{TKey,TValue}"/> to get.</param>
        /// <returns>If found, the <typeparamref name="TValue"/> with the given <paramref name="key"/>, otherwise the default <typeparamref name="TValue"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TValue Search<TKey, TValue>(this ImmutableHashTree<TKey, TValue> tree, TKey key)
        {
            int hashCode = key.GetHashCode();

            while (tree.Height != 0 && tree.HashCode != hashCode)
            {
                tree = hashCode < tree.HashCode ? tree.Left : tree.Right;
            }

            if (!tree.IsEmpty && (ReferenceEquals(tree.Key, key) || Equals(tree.Key, key)))
            {
                return tree.Value;
            }

            if (tree.Duplicates.Items.Length > 0)
            {
                foreach (var keyValue in tree.Duplicates.Items)
                {
                    if (ReferenceEquals(keyValue.Key, key) || Equals(keyValue.Key, key))
                    {
                        return keyValue.Value;
                    }
                }
            }

            return default;
        }

        /// <summary>
        /// Adds a new element to the <see cref="ImmutableHashTree{TKey,TValue}"/>.
        /// </summary>
        /// <typeparam name="TKey">The type of the key.</typeparam>
        /// <typeparam name="TValue">The type of the value.</typeparam>
        /// <param name="tree">The target <see cref="ImmutableHashTree{TKey,TValue}"/>.</param>
        /// <param name="key">The key to be associated with the value.</param>
        /// <param name="value">The value to be added to the tree.</param>
        /// <returns>A new <see cref="ImmutableHashTree{TKey,TValue}"/> that contains the new key/value pair.</returns>
        public static ImmutableHashTree<TKey, TValue> Add<TKey, TValue>(this ImmutableHashTree<TKey, TValue> tree, TKey key, TValue value)
        {
            if (tree.IsEmpty)
            {
                return new ImmutableHashTree<TKey, TValue>(key, value, tree, tree);
            }

            int hashCode = key.GetHashCode();

            if (hashCode > tree.HashCode)
            {
                return AddToRightBranch(tree, key, value);
            }

            if (hashCode < tree.HashCode)
            {
                return AddToLeftBranch(tree, key, value);
            }

            return new ImmutableHashTree<TKey, TValue>(key, value, tree);
        }

        /// <summary>
        /// Returns the nodes in the tree using in order traversal.
        /// </summary>
        /// <typeparam name="TKey">The type of the key.</typeparam>
        /// <typeparam name="TValue">The type of the value.</typeparam>
        /// <param name="hashTree">The target <see cref="ImmutableHashTree{TKey,TValue}"/>.</param>
        /// <returns>The nodes using in order traversal.</returns>
        public static IEnumerable<KeyValue<TKey, TValue>> InOrder<TKey, TValue>(
            this ImmutableHashTree<TKey, TValue> hashTree)
        {
            if (!hashTree.IsEmpty)
            {
                foreach (var left in InOrder(hashTree.Left))
                {
                    yield return new KeyValue<TKey, TValue>(left.Key, left.Value);
                }

                yield return new KeyValue<TKey, TValue>(hashTree.Key, hashTree.Value);

                for (int i = 0; i < hashTree.Duplicates.Items.Length; i++)
                {
                    yield return hashTree.Duplicates.Items[i];
                }

                foreach (var right in InOrder(hashTree.Right))
                {
                    yield return new KeyValue<TKey, TValue>(right.Key, right.Value);
                }
            }
        }

        private static ImmutableHashTree<TKey, TValue> AddToLeftBranch<TKey, TValue>(ImmutableHashTree<TKey, TValue> tree, TKey key, TValue value)
            => new ImmutableHashTree<TKey, TValue>(tree.Key, tree.Value, tree.Left.Add(key, value), tree.Right);

        private static ImmutableHashTree<TKey, TValue> AddToRightBranch<TKey, TValue>(ImmutableHashTree<TKey, TValue> tree, TKey key, TValue value)
            => new ImmutableHashTree<TKey, TValue>(tree.Key, tree.Value, tree.Left, tree.Right.Add(key, value));
    }

    /// <summary>
    /// Extends the <see cref="IEmitter"/> interface with a set of methods
    /// that optimizes and simplifies emitting MSIL instructions.
    /// </summary>
    internal static class EmitterExtensions
    {
        /// <summary>
        /// Performs a cast or unbox operation if the current <see cref="IEmitter.StackType"/> is
        /// different from the given <paramref name="type"/>.
        /// </summary>
        /// <param name="emitter">The target <see cref="IEmitter"/>.</param>
        /// <param name="type">The requested stack type.</param>
        public static void UnboxOrCast(this IEmitter emitter, Type type)
        {
            if (emitter.StackType == null)
            {
                return;
            }

            if (type == typeof(bool) && emitter.StackType == typeof(int))
            {
                return;
            }

            if (type == typeof(byte) && emitter.StackType == typeof(int))
            {
                return;
            }

            if (type == typeof(sbyte) && emitter.StackType == typeof(int))
            {
                return;
            }

            if (type == typeof(short) && emitter.StackType == typeof(int))
            {
                return;
            }

            if (type == typeof(ushort) && emitter.StackType == typeof(int))
            {
                return;
            }

            if (type == typeof(uint) && emitter.StackType == typeof(int))
            {
                return;
            }

            if (type == typeof(ulong) && emitter.StackType == typeof(long))
            {
                return;
            }

            if (type.GetTypeInfo().IsEnum)
            {
                return;
            }

            if (!type.GetTypeInfo().IsAssignableFrom(emitter.StackType.GetTypeInfo()))
            {
                emitter.Emit(type.GetTypeInfo().IsValueType ? OpCodes.Unbox_Any : OpCodes.Castclass, type);
            }
        }

        /// <summary>
        /// Pushes a constant value onto the evaluation stack.
        /// </summary>
        /// <param name="emitter">The target <see cref="IEmitter"/>.</param>
        /// <param name="index">The index of the constant value to be pushed onto the stack.</param>
        /// <param name="type">The requested stack type.</param>
        public static void PushConstant(this IEmitter emitter, int index, Type type)
        {
            emitter.PushConstant(index);
            emitter.UnboxOrCast(type);
        }

        /// <summary>
        /// Pushes a constant value onto the evaluation stack as a object reference.
        /// </summary>
        /// <param name="emitter">The target <see cref="IEmitter"/>.</param>
        /// <param name="index">The index of the constant value to be pushed onto the stack.</param>
        public static void PushConstant(this IEmitter emitter, int index)
        {
            emitter.PushArgument(0);
            emitter.Push(index);
            emitter.PushArrayElement();
        }

        /// <summary>
        /// Pushes the element containing an object reference at a specified index onto the stack.
        /// </summary>
        /// <param name="emitter">The target <see cref="IEmitter"/>.</param>
        public static void PushArrayElement(this IEmitter emitter) => emitter.Emit(OpCodes.Ldelem_Ref);

        /// <summary>
        /// Pushes the arguments associated with a service request onto the stack.
        /// The arguments are found as an array in the last element of the constants array
        /// that is passed into the dynamic method.
        /// </summary>
        /// <param name="emitter">The target <see cref="IEmitter"/>.</param>
        /// <param name="parameters">A list of <see cref="ParameterInfo"/> instances that
        /// represent the arguments to be pushed onto the stack.</param>
        public static void PushArguments(this IEmitter emitter, ParameterInfo[] parameters)
        {
            var argumentArray = emitter.DeclareLocal(typeof(object[]));
            emitter.Emit(OpCodes.Ldarg_0);
            emitter.Emit(OpCodes.Ldarg_0);
            emitter.Emit(OpCodes.Ldlen);
            emitter.Emit(OpCodes.Conv_I4);
            emitter.Emit(OpCodes.Ldc_I4_1);
            emitter.Emit(OpCodes.Sub);
            emitter.Emit(OpCodes.Ldelem_Ref);
            emitter.Emit(OpCodes.Castclass, typeof(object[]));
            emitter.Emit(OpCodes.Stloc, argumentArray);

            for (int i = 0; i < parameters.Length; i++)
            {
                emitter.Emit(OpCodes.Ldloc, argumentArray);
                emitter.Emit(OpCodes.Ldc_I4, i);
                emitter.Emit(OpCodes.Ldelem_Ref);
                emitter.Emit(
                    parameters[i].ParameterType.GetTypeInfo().IsValueType ? OpCodes.Unbox_Any : OpCodes.Castclass,
                    parameters[i].ParameterType);
            }
        }

        /// <summary>
        /// Calls a late-bound method on an object, pushing the return value onto the stack.
        /// </summary>
        /// <param name="emitter">The target <see cref="IEmitter"/>.</param>
        /// <param name="methodInfo">The <see cref="MethodInfo"/> that represents the method to be called.</param>
        public static void Call(this IEmitter emitter, MethodInfo methodInfo) => emitter.Emit(OpCodes.Callvirt, methodInfo);

        /// <summary>
        /// Pushes a new instance onto the stack.
        /// </summary>
        /// <param name="emitter">The target <see cref="IEmitter"/>.</param>
        /// <param name="constructorInfo">The <see cref="ConstructionInfo"/> that represent the object to be created.</param>
        public static void New(this IEmitter emitter, ConstructorInfo constructorInfo) => emitter.Emit(OpCodes.Newobj, constructorInfo);

        /// <summary>
        /// Pushes the given <paramref name="localBuilder"/> onto the stack.
        /// </summary>
        /// <param name="emitter">The target <see cref="IEmitter"/>.</param>
        /// <param name="localBuilder">The <see cref="LocalBuilder"/> to be pushed onto the stack.</param>
        public static void Push(this IEmitter emitter, LocalBuilder localBuilder)
        {
            int index = localBuilder.LocalIndex;
            switch (index)
            {
                case 0:
                    emitter.Emit(OpCodes.Ldloc_0);
                    return;
                case 1:
                    emitter.Emit(OpCodes.Ldloc_1);
                    return;
                case 2:
                    emitter.Emit(OpCodes.Ldloc_2);
                    return;
                case 3:
                    emitter.Emit(OpCodes.Ldloc_3);
                    return;
            }

            if (index <= 255)
            {
                emitter.Emit(OpCodes.Ldloc_S, (byte)index);
            }
            else
            {
                emitter.Emit(OpCodes.Ldloc, index);
            }
        }

        /// <summary>
        /// Pushes an argument with the given <paramref name="index"/> onto the stack.
        /// </summary>
        /// <param name="emitter">The target <see cref="IEmitter"/>.</param>
        /// <param name="index">The index of the argument to be pushed onto the stack.</param>
        public static void PushArgument(this IEmitter emitter, int index)
        {
            switch (index)
            {
                case 0:
                    emitter.Emit(OpCodes.Ldarg_0);
                    return;
                case 1:
                    emitter.Emit(OpCodes.Ldarg_1);
                    return;
                case 2:
                    emitter.Emit(OpCodes.Ldarg_2);
                    return;
                case 3:
                    emitter.Emit(OpCodes.Ldarg_3);
                    return;
            }

            if (index <= 255)
            {
                emitter.Emit(OpCodes.Ldarg_S, (byte)index);
            }
            else
            {
                emitter.Emit(OpCodes.Ldarg, index);
            }
        }

        /// <summary>
        /// Stores the value currently on top of the stack in the given <paramref name="localBuilder"/>.
        /// </summary>
        /// <param name="emitter">The target <see cref="IEmitter"/>.</param>
        /// <param name="localBuilder">The <see cref="LocalBuilder"/> for which the value is to be stored.</param>
        public static void Store(this IEmitter emitter, LocalBuilder localBuilder)
        {
            int index = localBuilder.LocalIndex;
            switch (index)
            {
                case 0:
                    emitter.Emit(OpCodes.Stloc_0);
                    return;
                case 1:
                    emitter.Emit(OpCodes.Stloc_1);
                    return;
                case 2:
                    emitter.Emit(OpCodes.Stloc_2);
                    return;
                case 3:
                    emitter.Emit(OpCodes.Stloc_3);
                    return;
            }

            if (index <= 255)
            {
                emitter.Emit(OpCodes.Stloc_S, (byte)index);
            }
            else
            {
                emitter.Emit(OpCodes.Stloc, index);
            }
        }

        /// <summary>
        /// Pushes a new array of the given <paramref name="elementType"/> onto the stack.
        /// </summary>
        /// <param name="emitter">The target <see cref="IEmitter"/>.</param>
        /// <param name="elementType">The element <see cref="Type"/> of the new array.</param>
        public static void PushNewArray(this IEmitter emitter, Type elementType) => emitter.Emit(OpCodes.Newarr, elementType);

        /// <summary>
        /// Pushes an <see cref="int"/> value onto the stack.
        /// </summary>
        /// <param name="emitter">The target <see cref="IEmitter"/>.</param>
        /// <param name="value">The <see cref="int"/> value to be pushed onto the stack.</param>
        public static void Push(this IEmitter emitter, int value)
        {
            switch (value)
            {
                case 0:
                    emitter.Emit(OpCodes.Ldc_I4_0);
                    return;
                case 1:
                    emitter.Emit(OpCodes.Ldc_I4_1);
                    return;
                case 2:
                    emitter.Emit(OpCodes.Ldc_I4_2);
                    return;
                case 3:
                    emitter.Emit(OpCodes.Ldc_I4_3);
                    return;
                case 4:
                    emitter.Emit(OpCodes.Ldc_I4_4);
                    return;
                case 5:
                    emitter.Emit(OpCodes.Ldc_I4_5);
                    return;
                case 6:
                    emitter.Emit(OpCodes.Ldc_I4_6);
                    return;
                case 7:
                    emitter.Emit(OpCodes.Ldc_I4_7);
                    return;
                case 8:
                    emitter.Emit(OpCodes.Ldc_I4_8);
                    return;
            }

            if (value > -129 && value < 128)
            {
                emitter.Emit(OpCodes.Ldc_I4_S, (sbyte)value);
            }
            else
            {
                emitter.Emit(OpCodes.Ldc_I4, value);
            }
        }

        /// <summary>
        /// Performs a cast of the value currently on top of the stack to the given <paramref name="type"/>.
        /// </summary>
        /// <param name="emitter">The target <see cref="IEmitter"/>.</param>
        /// <param name="type">The <see cref="Type"/> for which the value will be casted into.</param>
        public static void Cast(this IEmitter emitter, Type type) => emitter.Emit(OpCodes.Castclass, type);

        /// <summary>
        /// Returns from the current method.
        /// </summary>
        /// <param name="emitter">The target <see cref="IEmitter"/>.</param>
        public static void Return(this IEmitter emitter) => emitter.Emit(OpCodes.Ret);
    }

    /// <summary>
    /// Represents a set of configurable options when creating a new instance of the container.
    /// </summary>
    internal class ContainerOptions
    {
        private static readonly Lazy<ContainerOptions> DefaultOptions =
            new Lazy<ContainerOptions>(CreateDefaultContainerOptions);

        /// <summary>
        /// Initializes a new instance of the <see cref="ContainerOptions"/> class.
        /// </summary>
        public ContainerOptions()
        {
            EnableVariance = true;
            EnablePropertyInjection = true;
            LogFactory = t => message => { };
            EnableCurrentScope = true;
            EnableOptionalArguments = false;
        }

        /// <summary>
        /// Gets the default <see cref="ContainerOptions"/> used across all <see cref="ServiceContainer"/> instances.
        /// </summary>
        public static ContainerOptions Default => DefaultOptions.Value;

        /// <summary>
        /// Gets or sets a value indicating whether variance is applied when resolving an <see cref="IEnumerable{T}"/>.
        /// </summary>
        /// <remarks>
        /// The default value is true.
        /// </remarks>
        public bool EnableVariance { get; set; }

        /// <summary>
        /// Gets or sets a function that determines if variance should be applied to a given <see cref="IEnumerable{T}"/>.
        /// </summary>
        /// <remarks>
        /// The default is to apply variance to all <see cref="IEnumerable{T}"/> services.
        /// This filter will only be applied if the <see cref="EnableVariance"/> is set to 'true'.
        /// </remarks>
        public Func<Type, bool> VarianceFilter { get; set; } = _ => true;

        /// <summary>
        /// Gets or sets the log factory that creates the delegate used for logging.
        /// </summary>
        public Func<Type, Action<LogEntry>> LogFactory { get; set; }

        /// <summary>
        /// Gets or sets the function that determines the default service name.
        /// The default is to use the service registered without a service name as the default service.
        /// </summary>
        public Func<string[], string> DefaultServiceSelector { get; set; } = services => string.Empty;

        /// <summary>
        /// Gets or sets a value indicating whether property injection is enabled.
        /// </summary>
        /// <remarks>
        /// The default value is true.
        /// </remarks>
        public bool EnablePropertyInjection { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether a current scope is maintained when starting and ending scopes.
        /// If services are requested directly from the scope, there we can turn this feature off to improve
        /// the performance when starting and ending scopes.
        /// </summary>
        /// <remarks>
        /// The default value is true for backward compatibility.
        /// </remarks>
        public bool EnableCurrentScope { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether optional arguments should be allowed when resolving constructor dependencies.
        /// </summary>
        /// <remarks>
        /// The default value is false.
        /// </remarks>
        public bool EnableOptionalArguments { get; set; }

        private static ContainerOptions CreateDefaultContainerOptions() => new ContainerOptions();
    }

    /// <summary>
    /// Represents a log entry.
    /// </summary>
    internal class LogEntry
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LogEntry"/> class.
        /// </summary>
        /// <param name="level">The <see cref="LogLevel"/> of this entry.</param>
        /// <param name="message">The log message.</param>
        public LogEntry(LogLevel level, string message)
        {
            Level = level;
            Message = message;
        }

        /// <summary>
        /// Gets the <see cref="LogLevel"/> for this entry.
        /// </summary>
        public LogLevel Level { get; private set; }

        /// <summary>
        /// Gets the log message for this entry.
        /// </summary>
        public string Message { get; private set; }
    }

    /// <summary>
    /// An ultra lightweight service container.
    /// </summary>
    internal class ServiceContainer : IServiceContainer
    {
        private const string UnresolvedDependencyError = "Unresolved dependency {0}";
        private readonly Action<LogEntry> log;
        private readonly Func<Type, Type[], IMethodSkeleton> methodSkeletonFactory;
        private readonly ServiceRegistry<Action<IEmitter>> emitters = new ServiceRegistry<Action<IEmitter>>();
        private readonly ServiceRegistry<Delegate> constructorDependencyFactories = new ServiceRegistry<Delegate>();
        private readonly ServiceRegistry<Delegate> propertyDependencyFactories = new ServiceRegistry<Delegate>();
        private readonly ServiceRegistry<ServiceRegistration> availableServices = new ServiceRegistry<ServiceRegistration>();

        private readonly object lockObject = new object();
        private readonly ContainerOptions options;
        private readonly Storage<object> constants = new Storage<object>();
        private readonly Storage<ILifetime> disposableLifeTimes = new Storage<ILifetime>();
        private readonly Storage<DecoratorRegistration> decorators = new Storage<DecoratorRegistration>();
        private readonly Storage<ServiceOverride> overrides = new Storage<ServiceOverride>();
        private readonly Storage<FactoryRule> factoryRules = new Storage<FactoryRule>();
        private readonly Storage<Initializer> initializers = new Storage<Initializer>();

        private readonly Stack<Action<IEmitter>> dependencyStack = new Stack<Action<IEmitter>>();

        private readonly Lazy<IConstructionInfoProvider> constructionInfoProvider;

        private readonly LazyConcurrentDictionary<ServiceRegistration, int> servicesToDelegatesIndex =
            new LazyConcurrentDictionary<ServiceRegistration, int>();

        private ImmutableHashTable<Type, GetInstanceDelegate> delegates =
            ImmutableHashTable<Type, GetInstanceDelegate>.Empty;

        private ImmutableHashTable<Tuple<Type, string>, GetInstanceDelegate> namedDelegates =
            ImmutableHashTable<Tuple<Type, string>, GetInstanceDelegate>.Empty;

        private ImmutableHashTree<Type, Func<object[], Scope, object, object>> propertyInjectionDelegates =
            ImmutableHashTree<Type, Func<object[], Scope, object, object>>.Empty;

        private bool isLocked;
        private Type defaultLifetimeType;

        /// <summary>
        /// Initializes a new instance of the <see cref="ServiceContainer"/> class.
        /// </summary>
        public ServiceContainer()
            : this(ContainerOptions.Default)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ServiceContainer"/> class.
        /// </summary>
        /// <param name="configureOptions">A delegate used to configure <see cref="ContainerOptions"/>.</param>
        public ServiceContainer(Action<ContainerOptions> configureOptions)
        {
            this.options = new ContainerOptions();
            configureOptions(options);
            log = options.LogFactory(typeof(ServiceContainer));
            var concreteTypeExtractor = new CachedTypeExtractor(new ConcreteTypeExtractor());
            CompositionRootTypeExtractor = new CachedTypeExtractor(new CompositionRootTypeExtractor(new CompositionRootAttributeExtractor()));
            CompositionRootExecutor = new CompositionRootExecutor(this, type => (ICompositionRoot)Activator.CreateInstance(type));
            ServiceNameProvider = new ServiceNameProvider();
            PropertyDependencySelector = options.EnablePropertyInjection
                ? (IPropertyDependencySelector)new PropertyDependencySelector(new PropertySelector())
                : new PropertyDependencyDisabler();
            GenericArgumentMapper = new GenericArgumentMapper();
            AssemblyScanner = new AssemblyScanner(concreteTypeExtractor, CompositionRootTypeExtractor, CompositionRootExecutor, GenericArgumentMapper);
            ConstructorDependencySelector = new ConstructorDependencySelector();
            ConstructorSelector = new MostResolvableConstructorSelector(CanGetInstance, options.EnableOptionalArguments);
            constructionInfoProvider = new Lazy<IConstructionInfoProvider>(CreateConstructionInfoProvider);
            methodSkeletonFactory = (returnType, parameterTypes) => new DynamicMethodSkeleton(returnType, parameterTypes);
#if NET452 || NETSTANDARD1_3 || NETSTANDARD1_6 || NETSTANDARD2_0 || NET46 || NETCOREAPP2_0
            ScopeManagerProvider = new PerLogicalCallContextScopeManagerProvider();
#else
            ScopeManagerProvider = new PerThreadScopeManagerProvider();
#endif
#if NET452 || NET46 || NETSTANDARD1_6 || NETSTANDARD2_0 || NETCOREAPP2_0
            AssemblyLoader = new AssemblyLoader();
#endif

        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ServiceContainer"/> class.
        /// </summary>
        /// <param name="options">The <see cref="ContainerOptions"/> instances that represents the configurable options.</param>
        public ServiceContainer(ContainerOptions options)
        : this(o =>
        {
            o.LogFactory = options.LogFactory;
            o.DefaultServiceSelector = options.DefaultServiceSelector;
            o.EnableCurrentScope = options.EnableCurrentScope;
            o.EnablePropertyInjection = options.EnablePropertyInjection;
            o.EnableVariance = options.EnableVariance;
            o.VarianceFilter = options.VarianceFilter;
            o.EnableOptionalArguments = options.EnableOptionalArguments;
        })
        {
        }

        private ServiceContainer(
            ContainerOptions options,
            ServiceRegistry<Delegate> constructorDependencyFactories,
            ServiceRegistry<Delegate> propertyDependencyFactories,
            ServiceRegistry<ServiceRegistration> availableServices,
            Storage<DecoratorRegistration> decorators,
            Storage<ServiceOverride> overrides,
            Storage<FactoryRule> factoryRules,
            Storage<Initializer> initializers,
            Lazy<IConstructionInfoProvider> constructionInfoProvider,
            Func<Type, Type[], IMethodSkeleton> methodSkeletonFactory,
            Action<LogEntry> log,
            ICompositionRootExecutor compositionRootExecutor,
            IServiceNameProvider serviceNameProvider,
            IPropertyDependencySelector propertyDependencySelector,
            IGenericArgumentMapper genericArgumentMapper,
            IAssemblyScanner assemblyScanner,
            IConstructorDependencySelector constructorDependencySelector,
            IConstructorSelector constructorSelector,
#if NET452 || NET46 || NETSTANDARD1_6 || NETCOREAPP2_0
            IAssemblyLoader assemblyLoader,
#endif
            IScopeManagerProvider scopeManagerProvider)
        {
            this.options = options;
            this.constructorDependencyFactories = constructorDependencyFactories;
            this.propertyDependencyFactories = propertyDependencyFactories;
            this.availableServices = availableServices;
            this.decorators = decorators;
            this.overrides = overrides;
            this.factoryRules = factoryRules;
            this.initializers = initializers;
            this.constructionInfoProvider = constructionInfoProvider;
            this.methodSkeletonFactory = methodSkeletonFactory;
            this.log = log;
            CompositionRootExecutor = compositionRootExecutor;
            ServiceNameProvider = serviceNameProvider;
            PropertyDependencySelector = propertyDependencySelector;
            GenericArgumentMapper = genericArgumentMapper;
            AssemblyScanner = assemblyScanner;
            ConstructorDependencySelector = constructorDependencySelector;
            ConstructorSelector = constructorSelector;
            ScopeManagerProvider = scopeManagerProvider;
#if NET452 || NET46 || NETSTANDARD1_6 || NETCOREAPP2_0
            AssemblyLoader = assemblyLoader;
            foreach (var availableService in AvailableServices)
            {
                this.Register(availableService);
            }
#endif
        }

        /// <summary>
        /// Gets or sets the <see cref="IScopeManagerProvider"/> that is responsible
        /// for providing the <see cref="IScopeManager"/> used to manage scopes.
        /// </summary>
        public IScopeManagerProvider ScopeManagerProvider { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="IPropertyDependencySelector"/> instance that
        /// is responsible for selecting the property dependencies for a given type.
        /// </summary>
        public IPropertyDependencySelector PropertyDependencySelector { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="ITypeExtractor"/> that is responsible
        /// for extracting composition roots types from an assembly.
        /// </summary>
        public ITypeExtractor CompositionRootTypeExtractor { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="IServiceNameProvider"/> that is responsible
        /// for providing a service name for a given service during assembly scanning.
        /// </summary>
        public IServiceNameProvider ServiceNameProvider { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="ICompositionRootExecutor"/> that is responsible
        /// for executing composition roots.
        /// </summary>
        public ICompositionRootExecutor CompositionRootExecutor { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="IConstructorDependencySelector"/> instance that
        /// is responsible for selecting the constructor dependencies for a given constructor.
        /// </summary>
        public IConstructorDependencySelector ConstructorDependencySelector { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="IConstructorSelector"/> instance that is responsible
        /// for selecting the constructor to be used when creating new service instances.
        /// </summary>
        public IConstructorSelector ConstructorSelector { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="IGenericArgumentMapper"/> that is responsible for
        /// mapping generic arguments.
        /// </summary>
        public IGenericArgumentMapper GenericArgumentMapper { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="IAssemblyScanner"/> instance that is responsible for scanning assemblies.
        /// </summary>
        public IAssemblyScanner AssemblyScanner { get; set; }
#if NET452 || NETSTANDARD1_6 || NETSTANDARD2_0 || NET46 || NETCOREAPP2_0

        /// <summary>
        /// Gets or sets the <see cref="IAssemblyLoader"/> instance that is responsible for loading assemblies during assembly scanning.
        /// </summary>
        public IAssemblyLoader AssemblyLoader { get; set; }
#endif

        /// <summary>
        /// Gets a list of <see cref="ServiceRegistration"/> instances that represents the registered services.
        /// </summary>
        public IEnumerable<ServiceRegistration> AvailableServices
        {
            get
            {
                return availableServices.Values.SelectMany(t => t.Values);
            }
        }

        private ILifetime DefaultLifetime => (ILifetime)(defaultLifetimeType != null ? Activator.CreateInstance(defaultLifetimeType) : null);

        /// <inheritdoc/>
        public bool CanGetInstance(Type serviceType, string serviceName)
        {
            if (serviceType.IsFuncRepresentingService() || serviceType.IsFuncRepresentingNamedService() || serviceType.IsFuncWithParameters() || serviceType.IsLazy())
            {
                var returnType = serviceType.GenericTypeArguments.Last();
                return GetEmitMethod(returnType, serviceName) != null || availableServices.ContainsKey(serviceType);
            }

            return GetEmitMethod(serviceType, serviceName) != null;
        }

        /// <inheritdoc/>
        public Scope BeginScope()
        {
            if (options.EnableCurrentScope)
            {
                return ScopeManagerProvider.GetScopeManager(this).BeginScope();
            }
            else
            {
                return new Scope(this);
            }
        }

        /// <inheritdoc/>
        public object InjectProperties(object instance)
        {
            var type = instance.GetType();

            var del = propertyInjectionDelegates.Search(type);

            if (del == null)
            {
                del = CreatePropertyInjectionDelegate(type);
                propertyInjectionDelegates = propertyInjectionDelegates.Add(type, del);
            }

            return del(constants.Items, null, instance);
        }

        /// <inheritdoc/>
        public IServiceRegistry Register<TService>(Func<IServiceFactory, TService> factory, string serviceName, ILifetime lifetime)
        {
            RegisterServiceFromLambdaExpression<TService>(factory, lifetime, serviceName);
            return this;
        }

        /// <inheritdoc/>
        public IServiceRegistry RegisterFallback(Func<Type, string, bool> predicate, Func<ServiceRequest, object> factory)
            => RegisterFallback(predicate, factory, DefaultLifetime);

        /// <inheritdoc/>
        public IServiceRegistry RegisterFallback(Func<Type, string, bool> predicate, Func<ServiceRequest, object> factory, ILifetime lifetime)
        {
            factoryRules.Add(new FactoryRule { CanCreateInstance = predicate, Factory = factory, LifeTime = lifetime });
            return this;
        }

        /// <inheritdoc/>
        public IServiceRegistry Register(ServiceRegistration serviceRegistration)
        {
            var services = GetAvailableServices(serviceRegistration.ServiceType);
            var sr = serviceRegistration;
            services.AddOrUpdate(
                serviceRegistration.ServiceName,
                s => AddServiceRegistration(sr),
                (k, existing) => UpdateServiceRegistration(existing, sr));
            return this;
        }

        /// <inheritdoc/>
        public IServiceRegistry RegisterAssembly(Assembly assembly)
        {
            Type[] compositionRootTypes = CompositionRootTypeExtractor.Execute(assembly);
            if (compositionRootTypes.Length == 0)
            {
                RegisterAssembly(assembly, (serviceType, implementingType) => true);
            }
            else
            {
                AssemblyScanner.Scan(assembly, this);
            }

            return this;
        }

        /// <inheritdoc/>
        public IServiceRegistry RegisterAssembly(Assembly assembly, Func<Type, Type, bool> shouldRegister)
            => RegisterAssembly(assembly, () => DefaultLifetime, shouldRegister);

        /// <inheritdoc/>
        public IServiceRegistry RegisterAssembly(Assembly assembly, Func<ILifetime> lifetimeFactory)
            => RegisterAssembly(assembly, lifetimeFactory, (serviceType, implementingType) => true);

        /// <inheritdoc/>
        public IServiceRegistry RegisterAssembly(Assembly assembly, Func<ILifetime> lifetimeFactory, Func<Type, Type, bool> shouldRegister)
            => RegisterAssembly(assembly, lifetimeFactory, shouldRegister, ServiceNameProvider.GetServiceName);

        /// <inheritdoc/>
        public IServiceRegistry RegisterAssembly(Assembly assembly, Func<ILifetime> lifetimeFactory, Func<Type, Type, bool> shouldRegister, Func<Type, Type, string> serviceNameProvider)
        {
            AssemblyScanner.Scan(assembly, this, lifetimeFactory, shouldRegister, serviceNameProvider);
            return this;
        }

        /// <inheritdoc/>
        public IServiceRegistry RegisterFrom<TCompositionRoot>()
            where TCompositionRoot : ICompositionRoot, new()
        {
            CompositionRootExecutor.Execute(typeof(TCompositionRoot));
            return this;
        }

        /// <inheritdoc/>
        public IServiceRegistry RegisterFrom<TCompositionRoot>(TCompositionRoot compositionRoot)
           where TCompositionRoot : ICompositionRoot
        {
            CompositionRootExecutor.Execute(compositionRoot);
            return this;
        }

        /// <inheritdoc/>
        public IServiceRegistry RegisterConstructorDependency<TDependency>(Func<IServiceFactory, ParameterInfo, TDependency> factory)
        {
            if (isLocked)
            {
                var message =
                    $"Attempt to register a constructor dependency {typeof(TDependency)} after the first call to GetInstance." +
                    $"This might lead to incorrect behavior if a service with a {typeof(TDependency)} dependency has already been resolved";

                log.Warning(message);
            }

            GetConstructorDependencyFactories(typeof(TDependency)).AddOrUpdate(
                string.Empty,
                s => factory,
                (s, e) => isLocked ? e : factory);
            return this;
        }

        /// <inheritdoc/>
        public IServiceRegistry RegisterConstructorDependency<TDependency>(Func<IServiceFactory, ParameterInfo, object[], TDependency> factory)
        {
            if (isLocked)
            {
                var message =
                    $"Attempt to register a constructor dependency {typeof(TDependency)} after the first call to GetInstance." +
                    $"This might lead to incorrect behavior if a service with a {typeof(TDependency)} dependency has already been resolved";

                log.Warning(message);
            }

            GetConstructorDependencyFactories(typeof(TDependency)).AddOrUpdate(
                string.Empty,
                s => factory,
                (s, e) => isLocked ? e : factory);
            return this;
        }

        /// <inheritdoc/>
        public IServiceRegistry RegisterPropertyDependency<TDependency>(Func<IServiceFactory, PropertyInfo, TDependency> factory)
        {
            if (isLocked)
            {
                var message =
                    $"Attempt to register a property dependency {typeof(TDependency)} after the first call to GetInstance." +
                    $"This might lead to incorrect behavior if a service with a {typeof(TDependency)} dependency has already been resolved";

                log.Warning(message);
            }

            GetPropertyDependencyFactories(typeof(TDependency)).AddOrUpdate(
                string.Empty,
                s => factory,
                (s, e) => isLocked ? e : factory);
            return this;
        }

#if NET452 || NETSTANDARD1_6 || NETSTANDARD2_0 || NET46 || NETCOREAPP2_0
        /// <inheritdoc/>
        public IServiceRegistry RegisterAssembly(string searchPattern)
        {
            foreach (Assembly assembly in AssemblyLoader.Load(searchPattern))
            {
                RegisterAssembly(assembly);
            }

            return this;
        }
#endif

        /// <inheritdoc/>
        public IServiceRegistry Decorate(Type serviceType, Type decoratorType, Func<ServiceRegistration, bool> predicate)
        {
            var decoratorRegistration = new DecoratorRegistration { ServiceType = serviceType, ImplementingType = decoratorType, CanDecorate = predicate };
            Decorate(decoratorRegistration);
            return this;
        }

        /// <inheritdoc/>
        public IServiceRegistry Decorate(Type serviceType, Type decoratorType)
        {
            Decorate(serviceType, decoratorType, si => true);
            return this;
        }

        /// <inheritdoc/>
        public IServiceRegistry Decorate<TService, TDecorator>()
            where TDecorator : TService
        {
            Decorate(typeof(TService), typeof(TDecorator));
            return this;
        }

        /// <inheritdoc/>
        public IServiceRegistry Decorate<TService>(Func<IServiceFactory, TService, TService> factory)
        {
            var decoratorRegistration = new DecoratorRegistration { FactoryExpression = factory, ServiceType = typeof(TService), CanDecorate = si => true };
            Decorate(decoratorRegistration);
            return this;
        }

        /// <inheritdoc/>
        public IServiceRegistry Decorate(DecoratorRegistration decoratorRegistration)
        {
            int index = decorators.Add(decoratorRegistration);
            decoratorRegistration.Index = index;
            return this;
        }

        /// <inheritdoc/>
        public IServiceRegistry Override(Func<ServiceRegistration, bool> serviceSelector, Func<IServiceFactory, ServiceRegistration, ServiceRegistration> serviceRegistrationFactory)
        {
            var serviceOverride = new ServiceOverride(serviceSelector, serviceRegistrationFactory);
            overrides.Add(serviceOverride);
            return this;
        }

        /// <inheritdoc/>
        public IServiceRegistry Initialize(Func<ServiceRegistration, bool> predicate, Action<IServiceFactory, object> processor)
        {
            initializers.Add(new Initializer { Predicate = predicate, Initialize = processor });
            return this;
        }

        /// <inheritdoc/>
        public IServiceRegistry Register(Type serviceType, Type implementingType, ILifetime lifetime)
        {
            Register(serviceType, implementingType, string.Empty, lifetime);
            return this;
        }

        /// <inheritdoc/>
        public IServiceRegistry Register(Type serviceType, Type implementingType, string serviceName, ILifetime lifetime)
        {
            RegisterService(serviceType, implementingType, lifetime, serviceName);
            return this;
        }

        /// <inheritdoc/>
        public IServiceRegistry Register<TService, TImplementation>()
            where TImplementation : TService
        {
            Register(typeof(TService), typeof(TImplementation));
            return this;
        }

        /// <inheritdoc/>
        public IServiceRegistry Register<TService, TImplementation>(ILifetime lifetime)
            where TImplementation : TService
        {
            Register(typeof(TService), typeof(TImplementation), lifetime);
            return this;
        }

        /// <inheritdoc/>
        public IServiceRegistry Register<TService, TImplementation>(string serviceName)
            where TImplementation : TService
        {
            Register<TService, TImplementation>(serviceName, lifetime: null);
            return this;
        }

        /// <inheritdoc/>
        public IServiceRegistry Register<TService, TImplementation>(string serviceName, ILifetime lifetime)
            where TImplementation : TService
        {
            Register(typeof(TService), typeof(TImplementation), serviceName, lifetime);
            return this;
        }

        /// <inheritdoc/>
        public IServiceRegistry Register<TService>(Func<IServiceFactory, TService> factory, ILifetime lifetime)
        {
            RegisterServiceFromLambdaExpression<TService>(factory, lifetime, string.Empty);
            return this;
        }

        /// <inheritdoc/>
        public IServiceRegistry Register<TService>(Func<IServiceFactory, TService> factory, string serviceName)
        {
            RegisterServiceFromLambdaExpression<TService>(factory, null, serviceName);
            return this;
        }

        /// <inheritdoc/>
        public IServiceRegistry Register<TService>()
        {
            Register<TService, TService>();
            return this;
        }

        /// <inheritdoc/>
        public IServiceRegistry Register(Type serviceType)
        {
            Register(serviceType, serviceType);
            return this;
        }

        /// <inheritdoc/>
        public IServiceRegistry Register(Type serviceType, ILifetime lifetime)
        {
            Register(serviceType, serviceType, lifetime);
            return this;
        }

        /// <inheritdoc/>
        public IServiceRegistry Register<TService>(ILifetime lifetime)
        {
            Register<TService, TService>(lifetime);
            return this;
        }

        /// <inheritdoc/>
        public IServiceRegistry RegisterInstance<TService>(TService instance, string serviceName)
        {
            RegisterInstance(typeof(TService), instance, serviceName);
            return this;
        }

        /// <inheritdoc/>
        public IServiceRegistry RegisterInstance<TService>(TService instance)
        {
            RegisterInstance(typeof(TService), instance);
            return this;
        }

        /// <inheritdoc/>
        public IServiceRegistry RegisterInstance(Type serviceType, object instance)
        {
            RegisterInstance(serviceType, instance, string.Empty);
            return this;
        }

        /// <inheritdoc/>
        public IServiceRegistry RegisterInstance(Type serviceType, object instance, string serviceName)
        {
            Ensure.IsNotNull(instance, "instance");
            Ensure.IsNotNull(serviceType, "type");
            Ensure.IsNotNull(serviceName, "serviceName");
            RegisterValue(serviceType, instance, serviceName);
            return this;
        }

        /// <inheritdoc/>
        public IServiceRegistry Register<TService>(Func<IServiceFactory, TService> factory)
        {
            RegisterServiceFromLambdaExpression<TService>(factory, null, string.Empty);
            return this;
        }

        /// <inheritdoc/>
        public IServiceRegistry Register<T, TService>(Func<IServiceFactory, T, TService> factory)
        {
            RegisterServiceFromLambdaExpression<TService>(factory, null, string.Empty);
            return this;
        }

        /// <inheritdoc/>
        public IServiceRegistry Register<T, TService>(Func<IServiceFactory, T, TService> factory, string serviceName)
        {
            RegisterServiceFromLambdaExpression<TService>(factory, null, serviceName);
            return this;
        }

        /// <inheritdoc/>
        public IServiceRegistry Register<T1, T2, TService>(Func<IServiceFactory, T1, T2, TService> factory)
        {
            RegisterServiceFromLambdaExpression<TService>(factory, null, string.Empty);
            return this;
        }

        /// <inheritdoc/>
        public IServiceRegistry Register<T1, T2, TService>(Func<IServiceFactory, T1, T2, TService> factory, string serviceName)
        {
            RegisterServiceFromLambdaExpression<TService>(factory, null, serviceName);
            return this;
        }

        /// <inheritdoc/>
        public IServiceRegistry Register<T1, T2, T3, TService>(Func<IServiceFactory, T1, T2, T3, TService> factory)
        {
            RegisterServiceFromLambdaExpression<TService>(factory, null, string.Empty);
            return this;
        }

        /// <inheritdoc/>
        public IServiceRegistry Register<T1, T2, T3, TService>(Func<IServiceFactory, T1, T2, T3, TService> factory, string serviceName)
        {
            RegisterServiceFromLambdaExpression<TService>(factory, null, serviceName);
            return this;
        }

        /// <inheritdoc/>
        public IServiceRegistry Register<T1, T2, T3, T4, TService>(Func<IServiceFactory, T1, T2, T3, T4, TService> factory)
        {
            RegisterServiceFromLambdaExpression<TService>(factory, null, string.Empty);
            return this;
        }

        /// <inheritdoc/>
        public IServiceRegistry Register<T1, T2, T3, T4, TService>(Func<IServiceFactory, T1, T2, T3, T4, TService> factory, string serviceName)
        {
            RegisterServiceFromLambdaExpression<TService>(factory, null, serviceName);
            return this;
        }

        /// <inheritdoc/>
        public IServiceRegistry Register(Type serviceType, Type implementingType, string serviceName)
        {
            RegisterService(serviceType, implementingType, null, serviceName);
            return this;
        }

        /// <inheritdoc/>
        public IServiceRegistry Register(Type serviceType, Type implementingType)
        {
            RegisterService(serviceType, implementingType, null, string.Empty);
            return this;
        }

        /// <inheritdoc/>
        public IServiceRegistry RegisterOrdered(Type serviceType, Type[] implementingTypes, Func<Type, ILifetime> lifeTimeFactory)
        {
            return RegisterOrdered(serviceType, implementingTypes, lifeTimeFactory, i => i.ToString().PadLeft(3, '0'));
        }

        /// <inheritdoc/>
        public IServiceRegistry RegisterOrdered(
            Type serviceType,
            Type[] implementingTypes,
            Func<Type, ILifetime> lifeTimeFactory,
            Func<int, string> serviceNameFormatter)
        {
            var offset = GetAvailableServices(serviceType).Count;
            foreach (var implementingType in implementingTypes)
            {
                offset++;
                Register(serviceType, implementingType, serviceNameFormatter(offset), lifeTimeFactory(implementingType));
            }

            return this;
        }

        /// <inheritdoc/>
        public void Compile(Func<ServiceRegistration, bool> predicate)
        {
            var rootServices = AvailableServices.Where(predicate).ToArray();
            foreach (var rootService in rootServices)
            {
                if (rootService.ServiceType.GetTypeInfo().IsGenericTypeDefinition)
                {
                    log.Warning($"Unable to precompile open generic type '{GetPrettyName(rootService.ServiceType)}'");
                    continue;
                }

                if (string.IsNullOrWhiteSpace(rootService.ServiceName))
                {
                    CreateDefaultDelegate(rootService.ServiceType, true);
                }
                else
                {
                    CreateNamedDelegate(Tuple.Create(rootService.ServiceType, rootService.ServiceName), true);
                }
            }

            string GetPrettyName(Type type)
            {
                if (type.GetTypeInfo().IsGenericType)
                {
                    return $"{type.FullName.Substring(0, type.FullName.LastIndexOf("`", StringComparison.OrdinalIgnoreCase))}<{string.Join(", ", type.GetTypeInfo().GenericTypeParameters.Select(GetPrettyName))}>";
                }

                return type.Name;
            }
        }

        /// <inheritdoc/>
        public void Compile() => Compile(sr => true);

        /// <inheritdoc/>
        public void Compile<TService>(string serviceName = null)
        {
            if (string.IsNullOrWhiteSpace(serviceName))
            {
                CreateDefaultDelegate(typeof(TService), true);
            }
            else
            {
                CreateNamedDelegate(Tuple.Create(typeof(TService), serviceName), true);
            }
        }

        /// <inheritdoc/>
        public object GetInstance(Type serviceType)
        {
            var instanceDelegate = delegates.Search(serviceType);
            if (instanceDelegate == null)
            {
                instanceDelegate = CreateDefaultDelegate(serviceType, throwError: true);
            }

            return instanceDelegate(constants.Items, null);
        }

        /// <inheritdoc/>
        public object GetInstance(Type serviceType, object[] arguments)
        {
            var instanceDelegate = delegates.Search(serviceType);
            if (instanceDelegate == null)
            {
                instanceDelegate = CreateDefaultDelegate(serviceType, throwError: true);
            }

            object[] constantsWithArguments = constants.Items.Concat(new object[] { arguments }).ToArray();

            return instanceDelegate(constantsWithArguments, null);
        }

        /// <inheritdoc/>
        public object GetInstance(Type serviceType, string serviceName, object[] arguments)
        {
            var key = Tuple.Create(serviceType, serviceName);
            var instanceDelegate = namedDelegates.Search(key);
            if (instanceDelegate == null)
            {
                instanceDelegate = CreateNamedDelegate(key, throwError: true);
            }

            object[] constantsWithArguments = constants.Items.Concat(new object[] { arguments }).ToArray();

            return instanceDelegate(constantsWithArguments, null);
        }

        /// <inheritdoc/>
        public object TryGetInstance(Type serviceType)
        {
            var instanceDelegate = delegates.Search(serviceType);
            if (instanceDelegate == null)
            {
                instanceDelegate = CreateDefaultDelegate(serviceType, throwError: false);
            }

            return instanceDelegate(constants.Items, null);
        }

        /// <inheritdoc/>
        public object TryGetInstance(Type serviceType, string serviceName)
        {
            var key = Tuple.Create(serviceType, serviceName);
            var instanceDelegate = namedDelegates.Search(key);
            if (instanceDelegate == null)
            {
                instanceDelegate = CreateNamedDelegate(key, throwError: false);
            }

            return instanceDelegate(constants.Items, null);
        }

        /// <inheritdoc/>
        public object GetInstance(Type serviceType, string serviceName)
        {
            var key = Tuple.Create(serviceType, serviceName);
            var instanceDelegate = namedDelegates.Search(key);
            if (instanceDelegate == null)
            {
                instanceDelegate = CreateNamedDelegate(key, throwError: true);
            }

            return instanceDelegate(constants.Items, null);
        }

        /// <inheritdoc/>
        public IEnumerable<object> GetAllInstances(Type serviceType)
        {
            return (IEnumerable<object>)GetInstance(serviceType.GetEnumerableType());
        }

        /// <inheritdoc/>
        public object Create(Type serviceType)
        {
            Register(serviceType);
            return GetInstance(serviceType);
        }

        /// <inheritdoc/>
        public IServiceRegistry SetDefaultLifetime<T>()
            where T : ILifetime, new()
        {
            defaultLifetimeType = typeof(T);
            return this;
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            var disposableLifetimeInstances = disposableLifeTimes.Items
                .Where(lt => lt is IDisposable).Cast<IDisposable>().Reverse();
            foreach (var disposableLifetimeInstance in disposableLifetimeInstances)
            {
                disposableLifetimeInstance.Dispose();
            }
        }

        /// <summary>
        /// Creates a clone of the current <see cref="ServiceContainer"/>.
        /// </summary>
        /// <returns>A new <see cref="ServiceContainer"/> instance.</returns>
        public ServiceContainer Clone()
        {
            return new ServiceContainer(
                options,
                constructorDependencyFactories,
                propertyDependencyFactories,
                availableServices,
                decorators,
                overrides,
                factoryRules,
                initializers,
                constructionInfoProvider,
                methodSkeletonFactory,
                log,
                CompositionRootExecutor,
                ServiceNameProvider,
                PropertyDependencySelector,
                GenericArgumentMapper,
                AssemblyScanner,
                ConstructorDependencySelector,
                ConstructorSelector,
#if NET452 || NET46 || NETSTANDARD1_6 || NETCOREAPP2_0
                AssemblyLoader,
#endif
                ScopeManagerProvider);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal object GetInstance(Type serviceType, Scope scope)
        {
            var instanceDelegate = delegates.Search(serviceType);
            if (instanceDelegate == null)
            {
                instanceDelegate = CreateDefaultDelegate(serviceType, throwError: true);
            }

            return instanceDelegate(constants.Items, scope);
        }

        internal object GetInstance(Type serviceType, Scope scope, string serviceName)
        {
            var key = Tuple.Create(serviceType, serviceName);
            var instanceDelegate = namedDelegates.Search(key);
            if (instanceDelegate == null)
            {
                instanceDelegate = CreateNamedDelegate(key, throwError: true);
            }

            return instanceDelegate(constants.Items, scope);
        }

        internal IEnumerable<object> GetAllInstances(Type serviceType, Scope scope)
        {
            return (IEnumerable<object>)GetInstance(serviceType.GetEnumerableType(), scope);
        }

        internal object GetInstance(Type serviceType, object[] arguments, Scope scope)
        {
            var instanceDelegate = delegates.Search(serviceType);
            if (instanceDelegate == null)
            {
                instanceDelegate = CreateDefaultDelegate(serviceType, throwError: true);
            }

            object[] constantsWithArguments = constants.Items.Concat(new object[] { arguments }).ToArray();

            return instanceDelegate(constantsWithArguments, scope);
        }

        internal object GetInstance(Type serviceType, string serviceName, object[] arguments, Scope scope)
        {
            var key = Tuple.Create(serviceType, serviceName);
            var instanceDelegate = namedDelegates.Search(key);
            if (instanceDelegate == null)
            {
                instanceDelegate = CreateNamedDelegate(key, throwError: true);
            }

            object[] constantsWithArguments = constants.Items.Concat(new object[] { arguments }).ToArray();

            return instanceDelegate(constantsWithArguments, scope);
        }

        internal object TryGetInstance(Type serviceType, Scope scope)
        {
            var instanceDelegate = delegates.Search(serviceType);
            if (instanceDelegate == null)
            {
                instanceDelegate = CreateDefaultDelegate(serviceType, throwError: false);
            }

            return instanceDelegate(constants.Items, scope);
        }

        internal object TryGetInstance(Type serviceType, string serviceName, Scope scope)
        {
            var key = Tuple.Create(serviceType, serviceName);
            var instanceDelegate = namedDelegates.Search(key);
            if (instanceDelegate == null)
            {
                instanceDelegate = CreateNamedDelegate(key, throwError: false);
            }

            return instanceDelegate(constants.Items, scope);
        }

        internal object Create(Type serviceType, Scope scope)
        {
            Register(serviceType);
            return GetInstance(serviceType, scope);
        }

        private static void EmitEnumerable(IList<Action<IEmitter>> serviceEmitters, Type elementType, IEmitter emitter)
        {
            EmitNewArray(serviceEmitters, elementType, emitter);
        }

        private static void EmitNewArray(IList<Action<IEmitter>> emitMethods, Type elementType, IEmitter emitter)
        {
            LocalBuilder array = emitter.DeclareLocal(elementType.MakeArrayType());
            emitter.Push(emitMethods.Count);
            emitter.PushNewArray(elementType);
            emitter.Store(array);

            for (int index = 0; index < emitMethods.Count; index++)
            {
                emitter.Push(array);
                emitter.Push(index);
                emitMethods[index](emitter);
                emitter.UnboxOrCast(elementType);
                emitter.Emit(OpCodes.Stelem, elementType);
            }

            emitter.Push(array);
        }

        private static ILifetime CloneLifeTime(ILifetime lifetime)
        {
            if (lifetime is ICloneableLifeTime cloneable)
            {
                return cloneable.Clone();
            }

            return lifetime == null ? null : (ILifetime)Activator.CreateInstance(lifetime.GetType());
        }

        private static ConstructorDependency GetConstructorDependencyThatRepresentsDecoratorTarget(
            DecoratorRegistration decoratorRegistration, ConstructionInfo constructionInfo)
        {
            var constructorDependency =
                constructionInfo.ConstructorDependencies.FirstOrDefault(
                    cd =>
                        cd.ServiceType == decoratorRegistration.ServiceType
                        || (cd.ServiceType.IsLazy()
                            && cd.ServiceType.GetTypeInfo().GenericTypeArguments[0] == decoratorRegistration.ServiceType));
            return constructorDependency;
        }

        private static void PushRuntimeArguments(IEmitter emitter)
        {
            MethodInfo loadMethod = typeof(RuntimeArgumentsLoader).GetTypeInfo().GetDeclaredMethod("Load");
            emitter.Emit(OpCodes.Ldarg_0);
            emitter.Emit(OpCodes.Call, loadMethod);
        }

        private DecoratorRegistration CreateClosedGenericDecoratorRegistration(
            ServiceRegistration serviceRegistration, DecoratorRegistration openGenericDecorator)
        {
            Type implementingType = openGenericDecorator.ImplementingType;
            Type serviceType = serviceRegistration.ServiceType;
            Type[] genericTypeArguments = serviceType.GenericTypeArguments;

            if (!TryCreateClosedGenericDecoratorType(serviceType, implementingType, out var closedGenericDecoratorType))
            {
                log.Info($"Skipping decorator [{implementingType.FullName}] since it is incompatible with the service type [{serviceType.FullName}]");
                return null;
            }

            var decoratorInfo = new DecoratorRegistration
            {
                ServiceType = serviceRegistration.ServiceType,
                ImplementingType = closedGenericDecoratorType,
                CanDecorate = openGenericDecorator.CanDecorate,
                Index = openGenericDecorator.Index,
            };
            return decoratorInfo;
        }

        private bool TryCreateClosedGenericDecoratorType(Type serviceType, Type implementingType, out Type closedGenericDecoratorType)
        {
            closedGenericDecoratorType = null;
            var mapResult = GenericArgumentMapper.Map(serviceType, implementingType);
            if (!mapResult.IsValid)
            {
                return false;
            }

            closedGenericDecoratorType = TypeHelper.TryMakeGenericType(implementingType, mapResult.GetMappedArguments());
            if (closedGenericDecoratorType == null)
            {
                return false;
            }

            if (!serviceType.GetTypeInfo().IsAssignableFrom(closedGenericDecoratorType.GetTypeInfo()))
            {
                return false;
            }

            return true;
        }

        private Func<object[], Scope, object, object> CreatePropertyInjectionDelegate(Type concreteType)
        {
            lock (lockObject)
            {
                IMethodSkeleton methodSkeleton = methodSkeletonFactory(typeof(object), new[] { typeof(object[]), typeof(Scope), typeof(object) });

                ConstructionInfo constructionInfo = new ConstructionInfo();
                constructionInfo.PropertyDependencies.AddRange(PropertyDependencySelector.Execute(concreteType));
                constructionInfo.ImplementingType = concreteType;

                var emitter = methodSkeleton.GetEmitter();
                emitter.PushArgument(2);
                emitter.Cast(concreteType);
                try
                {
                    EmitPropertyDependencies(constructionInfo, emitter);
                }
                catch (Exception)
                {
                    dependencyStack.Clear();
                    throw;
                }

                emitter.Return();

                isLocked = true;

                return (Func<object[], Scope, object, object>)methodSkeleton.CreateDelegate(typeof(Func<object[], Scope, object, object>));
            }
        }

        private ConstructionInfoProvider CreateConstructionInfoProvider()
        {
            return new ConstructionInfoProvider(CreateTypeConstructionInfoBuilder());
        }

        private TypeConstructionInfoBuilder CreateTypeConstructionInfoBuilder()
        {
            return new TypeConstructionInfoBuilder(
                ConstructorSelector,
                ConstructorDependencySelector,
                PropertyDependencySelector,
                GetConstructorDependencyDelegate,
                GetPropertyDependencyExpression);
        }

        private Delegate GetConstructorDependencyDelegate(Type type, string serviceName)
        {
            GetConstructorDependencyFactories(type).TryGetValue(serviceName, out Delegate dependencyDelegate);
            return dependencyDelegate;
        }

        private Delegate GetPropertyDependencyExpression(Type type, string serviceName)
        {
            GetPropertyDependencyFactories(type).TryGetValue(serviceName, out Delegate dependencyDelegate);
            return dependencyDelegate;
        }

        private GetInstanceDelegate CreateDynamicMethodDelegate(Action<IEmitter> serviceEmitter)
        {
            var methodSkeleton = methodSkeletonFactory(typeof(object), new[] { typeof(object[]), typeof(Scope) });
            IEmitter emitter = methodSkeleton.GetEmitter();
            serviceEmitter(emitter);
            if (emitter.StackType.GetTypeInfo().IsValueType)
            {
                emitter.Emit(OpCodes.Box, emitter.StackType);
            }

            Instruction lastInstruction = emitter.Instructions.Last();

            if (lastInstruction.Code == OpCodes.Castclass)
            {
                emitter.Instructions.Remove(lastInstruction);
            }

            emitter.Return();

            isLocked = true;

            return (GetInstanceDelegate)methodSkeleton.CreateDelegate(typeof(GetInstanceDelegate));
        }

        private Action<IEmitter> GetEmitMethod(Type serviceType, string serviceName)
        {
            Action<IEmitter> emitMethod = GetRegisteredEmitMethod(serviceType, serviceName);

            if (emitMethod == null)
            {
                emitMethod = TryGetFallbackEmitMethod(serviceType, serviceName);
            }

            if (emitMethod == null)
            {
                AssemblyScanner.Scan(serviceType.GetTypeInfo().Assembly, this);
                emitMethod = GetRegisteredEmitMethod(serviceType, serviceName);
            }

            if (emitMethod == null)
            {
                emitMethod = TryGetFallbackEmitMethod(serviceType, serviceName);
            }

            return CreateEmitMethodWrapper(emitMethod, serviceType, serviceName);
        }

        private Action<IEmitter> TryGetFallbackEmitMethod(Type serviceType, string serviceName)
        {
            Action<IEmitter> emitMethod = null;
            var rule = factoryRules.Items.FirstOrDefault(r => r.CanCreateInstance(serviceType, serviceName));
            if (rule != null)
            {
                emitMethod = CreateServiceEmitterBasedOnFactoryRule(rule, serviceType, serviceName);

                RegisterEmitMethod(serviceType, serviceName, emitMethod);
            }

            return emitMethod;
        }

        private Action<IEmitter> CreateEmitMethodWrapper(Action<IEmitter> emitter, Type serviceType, string serviceName)
        {
            if (emitter == null)
            {
                return null;
            }

            return ms =>
            {
                if (dependencyStack.Contains(emitter))
                {
                    throw new InvalidOperationException(
                        string.Format("Recursive dependency detected: ServiceType:{0}, ServiceName:{1}]", serviceType, serviceName));
                }

                dependencyStack.Push(emitter);
                try
                {
                    emitter(ms);
                }
                finally
                {
                    if (dependencyStack.Count > 0)
                    {
                        dependencyStack.Pop();
                    }
                }
            };
        }

        private Action<IEmitter> GetRegisteredEmitMethod(Type serviceType, string serviceName)
        {
            var registrations = GetEmitMethods(serviceType);

            if (string.IsNullOrWhiteSpace(serviceName))
            {
                if (registrations.Count > 1)
                {
                    var serviceNames = registrations.Keys.OrderBy(k => k).ToArray();
                    var defaultServiceName = string.Empty;
                    serviceName = options.DefaultServiceSelector(serviceNames);
                }
            }

            registrations.TryGetValue(serviceName, out Action<IEmitter> emitMethod);
            return emitMethod ?? CreateEmitMethodForUnknownService(serviceType, serviceName);
        }

        private ServiceRegistration AddServiceRegistration(ServiceRegistration serviceRegistration)
        {
            var emitMethod = ResolveEmitMethod(serviceRegistration);
            RegisterEmitMethod(serviceRegistration.ServiceType, serviceRegistration.ServiceName, emitMethod);

            return serviceRegistration;
        }

        private void RegisterEmitMethod(Type serviceType, string serviceName, Action<IEmitter> emitMethod)
        {
            GetEmitMethods(serviceType).TryAdd(serviceName, emitMethod);
        }

        private ServiceRegistration UpdateServiceRegistration(ServiceRegistration existingRegistration, ServiceRegistration newRegistration)
        {
            if (isLocked)
            {
                var message = $"Cannot overwrite existing serviceregistration {existingRegistration} after the first call to GetInstance.";
                log.Warning(message);
                return existingRegistration;
            }

            Action<IEmitter> emitMethod = ResolveEmitMethod(newRegistration);
            var serviceEmitters = GetEmitMethods(newRegistration.ServiceType);
            serviceEmitters[newRegistration.ServiceName] = emitMethod;
            return newRegistration;
        }

        private DecoratorRegistration[] GetDecorators(ServiceRegistration serviceRegistration)
        {
            var registeredDecorators = decorators.Items.Where(d => d.ServiceType == serviceRegistration.ServiceType).ToList();

            registeredDecorators.AddRange(GetOpenGenericDecoratorRegistrations(serviceRegistration));
            registeredDecorators.AddRange(GetDeferredDecoratorRegistrations(serviceRegistration));
            return registeredDecorators.OrderBy(d => d.Index).ToArray();
        }

        private IEnumerable<DecoratorRegistration> GetOpenGenericDecoratorRegistrations(
            ServiceRegistration serviceRegistration)
        {
            var registrations = new List<DecoratorRegistration>();
            var serviceTypeInfo = serviceRegistration.ServiceType.GetTypeInfo();
            if (serviceTypeInfo.IsGenericType)
            {
                var openGenericServiceType = serviceTypeInfo.GetGenericTypeDefinition();
                var openGenericDecorators = decorators.Items.Where(d => d.ServiceType == openGenericServiceType);
                registrations.AddRange(
                    openGenericDecorators.Select(
                        openGenericDecorator =>
                            CreateClosedGenericDecoratorRegistration(serviceRegistration, openGenericDecorator)).Where(dr => dr != null));
            }

            return registrations;
        }

        private IEnumerable<DecoratorRegistration> GetDeferredDecoratorRegistrations(
            ServiceRegistration serviceRegistration)
        {
            var registrations = new List<DecoratorRegistration>();

            var deferredDecorators =
                decorators.Items.Where(ds => ds.CanDecorate(serviceRegistration) && ds.HasDeferredImplementingType);
            foreach (var deferredDecorator in deferredDecorators)
            {
                var decoratorRegistration = new DecoratorRegistration
                {
                    ServiceType = serviceRegistration.ServiceType,
                    ImplementingType =
                        deferredDecorator.ImplementingTypeFactory(this, serviceRegistration),
                    CanDecorate = sr => true,
                    Index = deferredDecorator.Index,
                };
                registrations.Add(decoratorRegistration);
            }

            return registrations;
        }

        private void EmitNewDecoratorInstance(DecoratorRegistration decoratorRegistration, IEmitter emitter, Action<IEmitter> pushInstance)
        {
            ConstructionInfo constructionInfo = GetConstructionInfo(decoratorRegistration);
            var constructorDependency = GetConstructorDependencyThatRepresentsDecoratorTarget(
                decoratorRegistration, constructionInfo);

            if (constructorDependency != null)
            {
                constructorDependency.IsDecoratorTarget = true;
            }

            if (constructionInfo.FactoryDelegate != null)
            {
                EmitNewDecoratorUsingFactoryDelegate(constructionInfo.FactoryDelegate, emitter, pushInstance);
            }
            else
            {
                EmitNewInstanceUsingImplementingType(emitter, constructionInfo, pushInstance);
            }
        }

        private void EmitNewDecoratorUsingFactoryDelegate(Delegate factoryDelegate, IEmitter emitter, Action<IEmitter> pushInstance)
        {
            var factoryDelegateIndex = constants.Add(factoryDelegate);
            Type funcType = factoryDelegate.GetType();
            emitter.PushConstant(factoryDelegateIndex, funcType);
            var serviceFactoryIndex = constants.Add(this);
            emitter.PushConstant(serviceFactoryIndex, typeof(IServiceFactory));
            var scopeManagerIndex = CreateScopeManagerIndex();
            emitter.PushConstant(scopeManagerIndex, typeof(IScopeManager));
            emitter.PushArgument(1);
            emitter.Emit(OpCodes.Call, ServiceFactoryLoader.LoadServiceFactoryMethod);
            pushInstance(emitter);
            MethodInfo invokeMethod = funcType.GetTypeInfo().GetDeclaredMethod("Invoke");
            emitter.Emit(OpCodes.Callvirt, invokeMethod);
        }

        private void EmitNewInstance(ServiceRegistration serviceRegistration, IEmitter emitter)
        {
            if (serviceRegistration.Value != null)
            {
                int index = constants.Add(serviceRegistration.Value);
                Type serviceType = serviceRegistration.ServiceType;
                emitter.PushConstant(index, serviceType);
            }
            else
            {
                var constructionInfo = GetConstructionInfo(serviceRegistration);

                if (serviceRegistration.FactoryExpression != null)
                {
                    EmitNewInstanceUsingFactoryDelegate(serviceRegistration, emitter);
                }
                else
                {
                    EmitNewInstanceUsingImplementingType(emitter, constructionInfo, null);
                }
            }
        }

        private void EmitDecorators(ServiceRegistration serviceRegistration, IEnumerable<DecoratorRegistration> serviceDecorators, IEmitter emitter, Action<IEmitter> decoratorTargetEmitMethod)
        {
            foreach (DecoratorRegistration decorator in serviceDecorators)
            {
                if (!decorator.CanDecorate(serviceRegistration))
                {
                    continue;
                }

                Action<IEmitter> currentDecoratorTargetEmitter = decoratorTargetEmitMethod;
                DecoratorRegistration currentDecorator = decorator;
                decoratorTargetEmitMethod = e => EmitNewDecoratorInstance(currentDecorator, e, currentDecoratorTargetEmitter);
            }

            decoratorTargetEmitMethod(emitter);
        }

        private void EmitNewInstanceUsingImplementingType(IEmitter emitter, ConstructionInfo constructionInfo, Action<IEmitter> decoratorTargetEmitMethod)
        {
            EmitConstructorDependencies(constructionInfo, emitter, decoratorTargetEmitMethod);
            emitter.Emit(OpCodes.Newobj, constructionInfo.Constructor);
            EmitPropertyDependencies(constructionInfo, emitter);
        }

        private void EmitNewInstanceUsingFactoryDelegate(ServiceRegistration serviceRegistration, IEmitter emitter)
        {
            var factoryDelegateIndex = constants.Add(serviceRegistration.FactoryExpression);
            Type funcType = serviceRegistration.FactoryExpression.GetType();
            MethodInfo invokeMethod = funcType.GetTypeInfo().GetDeclaredMethod("Invoke");
            emitter.PushConstant(factoryDelegateIndex, funcType);
            var parameters = invokeMethod.GetParameters();
            if (parameters.Length == 1 && parameters[0].ParameterType == typeof(ServiceRequest))
            {
                var createServiceRequestMethod = ServiceRequestHelper.CreateServiceRequestMethod.MakeGenericMethod(serviceRegistration.ServiceType);
                emitter.Emit(OpCodes.Ldstr, serviceRegistration.ServiceName);
                var serviceFactoryIndex = constants.Add(this);
                emitter.PushConstant(serviceFactoryIndex, typeof(IServiceFactory));
                var scopeManagerIndex = CreateScopeManagerIndex();
                emitter.PushConstant(scopeManagerIndex, typeof(IScopeManager));
                emitter.PushArgument(1);
                emitter.Emit(OpCodes.Call, ServiceFactoryLoader.LoadServiceFactoryMethod);
                emitter.Emit(OpCodes.Call, createServiceRequestMethod);

                emitter.Call(invokeMethod);
                emitter.UnboxOrCast(serviceRegistration.ServiceType);
            }
            else
            {
                var serviceFactoryIndex = constants.Add(this);
                emitter.PushConstant(serviceFactoryIndex, typeof(IServiceFactory));
                var scopeManagerIndex = CreateScopeManagerIndex();
                emitter.PushConstant(scopeManagerIndex, typeof(IScopeManager));
                emitter.PushArgument(1);
                emitter.Emit(OpCodes.Call, ServiceFactoryLoader.LoadServiceFactoryMethod);

                if (parameters.Length > 1)
                {
                    emitter.PushArguments(parameters.Skip(1).ToArray());
                }

                emitter.Call(invokeMethod);
            }
        }

        private void EmitConstructorDependencies(ConstructionInfo constructionInfo, IEmitter emitter, Action<IEmitter> decoratorTargetEmitter)
        {
            foreach (ConstructorDependency dependency in constructionInfo.ConstructorDependencies)
            {
                if (!dependency.IsDecoratorTarget)
                {
                    EmitConstructorDependency(emitter, dependency);
                }
                else
                {
                    if (dependency.ServiceType.IsLazy())
                    {
                        var scopeVariable = emitter.DeclareLocal(typeof(Scope));

                        // Push the scope into the stack
                        emitter.PushArgument(1);

                        int scopeManagerIndex = CreateScopeManagerIndex();

                        // Push the scope manager into the stack.
                        emitter.PushConstant(scopeManagerIndex, typeof(IScopeManager));

                        // Get the scope
                        emitter.Emit(OpCodes.Call, ScopeLoader.GetThisOrCurrentScopeMethod);

                        emitter.Store(scopeVariable);

                        var instanceDelegateIndex = CreateInstanceDelegateIndex(decoratorTargetEmitter);

                        // Push the GetInstanceDelegate that represents emitting the decoratee.
                        emitter.PushConstant(instanceDelegateIndex, typeof(GetInstanceDelegate));

                        // push the constants
                        emitter.PushArgument(0);

                        emitter.Push(scopeVariable);

                        var createScopedLazyFromDelegateMethod = LazyHelper.CreateScopedLazyFromDelegateMethod.MakeGenericMethod(dependency.ServiceType.GetTypeInfo().GenericTypeArguments.Last());

                        emitter.Emit(OpCodes.Call, createScopedLazyFromDelegateMethod);
                    }
                    else
                    {
                        decoratorTargetEmitter(emitter);
                    }
                }
            }
        }

        private void EmitConstructorDependency(IEmitter emitter, Dependency dependency)
        {
            var emitMethod = GetEmitMethodForDependency(dependency);

            try
            {
                emitMethod(emitter);
                emitter.UnboxOrCast(dependency.ServiceType);
            }
            catch (InvalidOperationException ex)
            {
                throw new InvalidOperationException(string.Format(UnresolvedDependencyError, dependency), ex);
            }
        }

        private void EmitPropertyDependency(IEmitter emitter, PropertyDependency propertyDependency, LocalBuilder instanceVariable)
        {
            var propertyDependencyEmitMethod = GetEmitMethodForDependency(propertyDependency);

            if (propertyDependencyEmitMethod == null)
            {
                return;
            }

            emitter.Push(instanceVariable);
            propertyDependencyEmitMethod(emitter);
            emitter.UnboxOrCast(propertyDependency.ServiceType);
            emitter.Call(propertyDependency.Property.SetMethod);
        }

        private Action<IEmitter> GetEmitMethodForDependency(Dependency dependency)
        {
            if (dependency.FactoryExpression != null)
            {
                return skeleton => EmitDependencyUsingFactoryExpression(skeleton, dependency);
            }

            Action<IEmitter> emitter = null;
            var dependencyName = string.IsNullOrWhiteSpace(dependency.ServiceName) ? dependency.Name : dependency.ServiceName;

            var registrations = GetEmitMethods(dependency.ServiceType);
            if (registrations.Count > 1)
            {
                if (registrations.TryGetValue(dependencyName, out emitter))
                {
                    return emitter;
                }
            }

            emitter = GetEmitMethod(dependency.ServiceType, dependency.ServiceName);

            if (emitter == null)
            {
                emitter = GetEmitMethod(dependency.ServiceType, dependency.Name);
                if (emitter == null && dependency.IsRequired)
                {
                    if (dependency is ConstructorDependency constructorDependency && constructorDependency.Parameter.HasDefaultValue && options.EnableOptionalArguments)
                    {
                        emitter = GetEmitMethodForDefaultValue(constructorDependency);
                    }
                    else
                    {
                        throw new InvalidOperationException(string.Format(UnresolvedDependencyError, dependency));
                    }
                }
            }

            return emitter;
        }

#if NETSTANDARD1_1 || NETSTANDARD1_3 || NETSTANDARD1_6 || NETSTANDARD2_0
        private Action<IEmitter> GetEmitMethodForDefaultValue(ConstructorDependency constructorDependency)
        {
            Type parameterType = constructorDependency.Parameter.ParameterType;
            return (emitter) =>
            {
                var defaultValue = constructorDependency.Parameter.DefaultValue;
                if (defaultValue == null)
                {
                    defaultValue = TypeHelper.GetDefaultValue(parameterType);
                }

                emitter.PushConstantValue(defaultValue, parameterType);
            };
        }
#endif

        // NOTE: altered from NET452 || NET46 || NETCOREAPP2_0
#if NETFRAMEWORK || NETCOREAPP
        private Action<IEmitter> GetEmitMethodForDefaultValue(ConstructorDependency constructorDependency)
        {
            Type parameterType = constructorDependency.Parameter.ParameterType;

            if (parameterType.GetTypeInfo().IsEnum)
            {
                parameterType = Enum.GetUnderlyingType(parameterType);
            }

            return (emitter) =>
            {
                var parameter = constructorDependency.Parameter;

                if (parameterType == typeof(bool))
                {
                    var defaultValue = ((bool)parameter.DefaultValue) ? 1 : 0;
                    emitter.Emit(OpCodes.Ldc_I4, defaultValue);
                }
                else if (parameterType == typeof(byte))
                {
                    int defaultValue = (byte)parameter.DefaultValue;
                    emitter.Emit(OpCodes.Ldc_I4, defaultValue);
                }
                else if (parameterType == typeof(sbyte))
                {
                    int defaultValue = (sbyte)parameter.DefaultValue;
                    emitter.Emit(OpCodes.Ldc_I4, defaultValue);
                }
                else if (parameterType == typeof(short))
                {
                    int defaultValue = (short)parameter.DefaultValue;
                    emitter.Emit(OpCodes.Ldc_I4, defaultValue);
                }
                else if (parameterType == typeof(ushort))
                {
                    int defaultValue = (ushort)parameter.DefaultValue;
                    emitter.Emit(OpCodes.Ldc_I4, defaultValue);
                }
                else if (parameterType == typeof(uint))
                {
                    int defaultValue = (int)(uint)parameter.DefaultValue;
                    emitter.Emit(OpCodes.Ldc_I4, defaultValue);
                }
                else if (parameterType == typeof(int))
                {
                    int defaultValue = (int)parameter.DefaultValue;
                    emitter.Emit(OpCodes.Ldc_I4, defaultValue);
                }
                else if (parameterType == typeof(long))
                {
                    long defaultValue = (long)parameter.DefaultValue;
                    emitter.Emit(OpCodes.Ldc_I8, defaultValue);
                }
                else if (parameterType == typeof(ulong))
                {
                    long defaultValue = (long)(ulong)parameter.DefaultValue;
                    emitter.Emit(OpCodes.Ldc_I8, defaultValue);
                }
                else if (parameterType == typeof(string))
                {
                    if (parameter.DefaultValue == null)
                    {
                        emitter.Emit(OpCodes.Ldnull);
                    }
                    else
                    {
                        emitter.Emit(OpCodes.Ldstr, (string)parameter.DefaultValue);
                    }
                }
                else if (parameterType.GetTypeInfo().IsClass || parameterType.GetTypeInfo().IsInterface)
                {
                    emitter.Emit(OpCodes.Ldnull);
                }
                else if (parameterType.GetTypeInfo().IsValueType)
                {
                    var local = emitter.DeclareLocal(parameterType);
                    emitter.Emit(OpCodes.Ldloca, local.LocalIndex);
                    emitter.Emit(OpCodes.Initobj, parameterType);
                    emitter.Emit(OpCodes.Ldloc, local.LocalIndex);
                }
            };
        }
#endif

        private void EmitDependencyUsingFactoryExpression(IEmitter emitter, Dependency dependency)
        {
            var actions = new List<Action<IEmitter>>();
            var parameters = dependency.FactoryExpression.GetMethodInfo().GetParameters();

            foreach (var parameter in parameters)
            {
                if (parameter.ParameterType == typeof(IServiceFactory))
                {
                    var serviceFactoryIndex = constants.Add(this);
                    var scopeManagerIndex = CreateScopeManagerIndex();
                    actions.Add(e => e.PushConstant(serviceFactoryIndex, typeof(IServiceFactory)));
                    actions.Add(e => e.PushConstant(scopeManagerIndex, typeof(IScopeManager)));
                    actions.Add(e => e.PushArgument(1));
                    actions.Add(e => e.Emit(OpCodes.Call, ServiceFactoryLoader.LoadServiceFactoryMethod));
                }

                if (parameter.ParameterType == typeof(ParameterInfo))
                {
                    actions.Add(e => e.PushConstant(constants.Add(((ConstructorDependency)dependency).Parameter), typeof(ParameterInfo)));
                }

                if (parameter.ParameterType == typeof(PropertyInfo))
                {
                    actions.Add(e => e.PushConstant(constants.Add(((PropertyDependency)dependency).Property), typeof(PropertyInfo)));
                }

                if (parameter.ParameterType == typeof(object[]))
                {
                    actions.Add(e => PushRuntimeArguments(e));
                }
            }

            var factoryDelegateIndex = constants.Add(dependency.FactoryExpression);
            Type funcType = dependency.FactoryExpression.GetType();
            MethodInfo invokeMethod = funcType.GetTypeInfo().GetDeclaredMethod("Invoke");
            emitter.PushConstant(factoryDelegateIndex, funcType);

            foreach (var action in actions)
            {
                action(emitter);
            }

            emitter.Call(invokeMethod);
        }

        private void EmitPropertyDependencies(ConstructionInfo constructionInfo, IEmitter emitter)
        {
            if (constructionInfo.PropertyDependencies.Count == 0)
            {
                return;
            }

            LocalBuilder instanceVariable = emitter.DeclareLocal(constructionInfo.ImplementingType);
            emitter.Store(instanceVariable);
            foreach (var propertyDependency in constructionInfo.PropertyDependencies)
            {
                EmitPropertyDependency(emitter, propertyDependency, instanceVariable);
            }

            emitter.Push(instanceVariable);
        }

        private Action<IEmitter> CreateEmitMethodForUnknownService(Type serviceType, string serviceName)
        {
            Action<IEmitter> emitter = null;
            if (CanRedirectRequestForDefaultServiceToSingleNamedService(serviceType, serviceName))
            {
                emitter = CreateServiceEmitterBasedOnSingleNamedInstance(serviceType);
            }
            else if (serviceType.IsLazy())
            {
                emitter = CreateEmitMethodBasedOnLazyServiceRequest(serviceType);
            }
            else if (serviceType.IsFuncWithParameters())
            {
                emitter = CreateEmitMethodBasedParameterizedFuncRequest(serviceType, serviceName);
            }
            else if (serviceType.IsFuncRepresentingService() || serviceType.IsFuncRepresentingNamedService())
            {
                emitter = CreateEmitMethodBasedOnFuncServiceRequest(serviceType);
            }
            else if (serviceType.IsEnumerableOfT())
            {
                emitter = CreateEmitMethodBasedOnClosedGenericServiceRequest(serviceType, serviceName);
                if (emitter == null)
                {
                    emitter = CreateEmitMethodForEnumerableServiceServiceRequest(serviceType);
                }
            }
            else if (serviceType.IsArray)
            {
                emitter = CreateEmitMethodForArrayServiceRequest(serviceType);
            }
            else if (serviceType.IsReadOnlyCollectionOfT() || serviceType.IsReadOnlyListOfT())
            {
                emitter = CreateEmitMethodBasedOnClosedGenericServiceRequest(serviceType, serviceName);
                if (emitter == null)
                {
                    emitter = CreateEmitMethodForReadOnlyCollectionServiceRequest(serviceType);
                }
            }
            else if (serviceType.IsListOfT())
            {
                emitter = CreateEmitMethodBasedOnClosedGenericServiceRequest(serviceType, serviceName);
                if (emitter == null)
                {
                    emitter = CreateEmitMethodForListServiceRequest(serviceType);
                }
            }
            else if (serviceType.IsCollectionOfT())
            {
                emitter = CreateEmitMethodBasedOnClosedGenericServiceRequest(serviceType, serviceName);
                if (emitter == null)
                {
                    emitter = CreateEmitMethodForListServiceRequest(serviceType);
                }
            }
            else if (serviceType.IsClosedGeneric())
            {
                emitter = CreateEmitMethodBasedOnClosedGenericServiceRequest(serviceType, serviceName);
            }

            return emitter;
        }

        private Action<IEmitter> CreateEmitMethodBasedOnFuncServiceRequest(Type serviceType)
        {
            var returnType = serviceType.GetTypeInfo().GenericTypeArguments.Last();
            if (serviceType.IsFuncRepresentingService())
            {
                var createScopedGenericFuncMethod = FuncHelper.CreateScopedGenericFuncMethod.MakeGenericMethod(returnType);
                return e =>
                {
                    e.PushConstant(constants.Add(this), typeof(ServiceContainer));

                    int scopeManagerIndex = CreateScopeManagerIndex();

                    // Push the scope into the stack
                    e.PushArgument(1);

                    // Push the scope manager into the stack.
                    e.PushConstant(scopeManagerIndex, typeof(IScopeManager));

                    // Get the scope
                    e.Emit(OpCodes.Call, ScopeLoader.GetThisOrCurrentScopeMethod);

                    e.Emit(OpCodes.Call, createScopedGenericFuncMethod);
                };
            }
            else
            {
                var createScopedGenericNamedFuncMethod = FuncHelper.CreateScopedGenericNamedFuncMethod.MakeGenericMethod(returnType);
                return e =>
                {
                    e.PushConstant(constants.Add(this), typeof(ServiceContainer));

                    int scopeManagerIndex = CreateScopeManagerIndex();

                    // Push the scope into the stack
                    e.PushArgument(1);

                    // Push the scope manager into the stack.
                    e.PushConstant(scopeManagerIndex, typeof(IScopeManager));

                    // Get the scope
                    e.Emit(OpCodes.Call, ScopeLoader.GetThisOrCurrentScopeMethod);

                    e.Emit(OpCodes.Call, createScopedGenericNamedFuncMethod);
                };
            }
        }

        private Action<IEmitter> CreateEmitMethodBasedParameterizedFuncRequest(Type serviceType, string serviceName)
        {
            Delegate getInstanceDelegate;
            if (string.IsNullOrEmpty(serviceName))
            {
                getInstanceDelegate = CreateGetInstanceWithParametersDelegate(serviceType);
            }
            else
            {
                getInstanceDelegate = ReflectionHelper.CreateGetNamedInstanceWithParametersDelegate(
                    this,
                    serviceType,
                    serviceName);
            }

            var constantIndex = constants.Add(getInstanceDelegate);
            return e => e.PushConstant(constantIndex, serviceType);
        }

        private Delegate CreateGetInstanceWithParametersDelegate(Type serviceType)
        {
            var getInstanceMethod = ReflectionHelper.GetGetInstanceWithParametersMethod(serviceType);
            return getInstanceMethod.CreateDelegate(serviceType, this);
        }

        private Action<IEmitter> CreateServiceEmitterBasedOnFactoryRule(FactoryRule rule, Type serviceType, string serviceName)
        {
            var serviceRegistration = new ServiceRegistration
            {
                ServiceType = serviceType,
                ServiceName = serviceName,
                FactoryExpression = rule.Factory,
                Lifetime = CloneLifeTime(rule.LifeTime) ?? DefaultLifetime,
            };
            if (rule.LifeTime != null)
            {
                return emitter => EmitLifetime(serviceRegistration, e => EmitNewInstanceWithDecorators(serviceRegistration, e), emitter);
            }

            return emitter => EmitNewInstanceWithDecorators(serviceRegistration, emitter);
        }

        private Action<IEmitter> CreateEmitMethodForArrayServiceRequest(Type serviceType)
        {
            Action<IEmitter> enumerableEmitter = CreateEmitMethodForEnumerableServiceServiceRequest(serviceType);
            return enumerableEmitter;
        }

        private Action<IEmitter> CreateEmitMethodForListServiceRequest(Type serviceType)
        {
            // Note replace this with getEmitMethod();
            Action<IEmitter> enumerableEmitter = CreateEmitMethodForEnumerableServiceServiceRequest(serviceType);

            MethodInfo openGenericToArrayMethod = typeof(Enumerable).GetTypeInfo().GetDeclaredMethod("ToList");
            MethodInfo closedGenericToListMethod = openGenericToArrayMethod.MakeGenericMethod(TypeHelper.GetElementType(serviceType));
            return ms =>
            {
                enumerableEmitter(ms);
                ms.Emit(OpCodes.Call, closedGenericToListMethod);
            };
        }

        private Action<IEmitter> CreateEmitMethodForReadOnlyCollectionServiceRequest(Type serviceType)
        {
            Type elementType = TypeHelper.GetElementType(serviceType);
            Type closedGenericReadOnlyCollectionType = typeof(ReadOnlyCollection<>).MakeGenericType(elementType);
            ConstructorInfo constructorInfo =
                closedGenericReadOnlyCollectionType.GetTypeInfo().DeclaredConstructors.Single();

            Action<IEmitter> listEmitMethod = CreateEmitMethodForListServiceRequest(serviceType);

            return emitter =>
            {
                listEmitMethod(emitter);
                emitter.New(constructorInfo);
            };
        }

        private Action<IEmitter> CreateEmitMethodBasedOnLazyServiceRequest(Type serviceType)
        {
            var returnType = serviceType.GetTypeInfo().GenericTypeArguments.Last();
            var createScopedLazyMethod = LazyHelper.CreateScopedLazyMethod.MakeGenericMethod(returnType);
            return e =>
            {
                e.PushConstant(constants.Add(this), typeof(ServiceContainer));

                int scopeManagerIndex = CreateScopeManagerIndex();

                // Push the scope into the stack
                e.PushArgument(1);

                // Push the scope manager into the stack.
                e.PushConstant(scopeManagerIndex, typeof(IScopeManager));

                // Get the scope
                e.Emit(OpCodes.Call, ScopeLoader.GetThisOrCurrentScopeMethod);

                e.Emit(OpCodes.Call, createScopedLazyMethod);
            };
        }

        private ThreadSafeDictionary<string, ServiceRegistration> GetOpenGenericServiceRegistrations(Type openGenericServiceType)
        {
            var services = GetAvailableServices(openGenericServiceType);
            return services;
        }

        private Action<IEmitter> CreateEmitMethodBasedOnClosedGenericServiceRequest(Type closedGenericServiceType, string serviceName)
        {
            Type openGenericServiceType = closedGenericServiceType.GetGenericTypeDefinition();
            var openGenericServiceRegistrations =
                GetOpenGenericServiceRegistrations(openGenericServiceType);

            Dictionary<string, ClosedGenericCandidate> candidates = new Dictionary<string, ClosedGenericCandidate>(StringComparer.OrdinalIgnoreCase);

            foreach (var openGenericServiceRegistration in openGenericServiceRegistrations.Values)
            {
                var closedGenericImplementingTypeCandidate = GenericArgumentMapper.TryMakeGenericType(closedGenericServiceType, openGenericServiceRegistration.ImplementingType);
                if (closedGenericImplementingTypeCandidate != null)
                {
                    candidates.Add(openGenericServiceRegistration.ServiceName, new ClosedGenericCandidate(closedGenericImplementingTypeCandidate, openGenericServiceRegistration.Lifetime));
                }
            }

            ClosedGenericCandidate candidate;

            // We have a request for the default service
            if (string.IsNullOrWhiteSpace(serviceName))
            {
                var defaultServiceName = string.Empty;
                if (candidates.Count > 0)
                {
                    defaultServiceName = options.DefaultServiceSelector(candidates.Keys.OrderBy(k => k).ToArray());
                }

                if (candidates.TryGetValue(defaultServiceName, out candidate))
                {
                    return RegisterAndGetEmitMethod();
                }
                else if (candidates.Count == 1)
                {
                    candidate = candidates.First().Value;
                    return RegisterAndGetEmitMethod();
                }
                else
                {
                    return null;
                }
            }
            else
            {
                if (candidates.TryGetValue(serviceName, out candidate))
                {
                    return RegisterAndGetEmitMethod();
                }
                else
                {
                    return null;
                }
            }

            Action<IEmitter> RegisterAndGetEmitMethod()
            {
                var serviceRegistration = new ServiceRegistration
                {
                    ServiceType = closedGenericServiceType,
                    ImplementingType = candidate.ClosedGenericImplentingType,
                    ServiceName = serviceName,
                    Lifetime = CloneLifeTime(candidate.Lifetime) ?? DefaultLifetime,
                };
                Register(serviceRegistration);
                return GetEmitMethod(serviceRegistration.ServiceType, serviceRegistration.ServiceName);
            }
        }

        private Action<IEmitter> CreateEmitMethodForEnumerableServiceServiceRequest(Type serviceType)
        {
            Type actualServiceType = TypeHelper.GetElementType(serviceType);

            if (actualServiceType.GetTypeInfo().IsGenericType)
            {
                Type openGenericServiceType = actualServiceType.GetGenericTypeDefinition();
                var openGenericServiceRegistrations = GetOpenGenericServiceRegistrations(openGenericServiceType);

                var constructableOpenGenericServices = openGenericServiceRegistrations.Values.Select(r => new { r.Lifetime, r.ServiceName, closedGenericImplementingType = GenericArgumentMapper.TryMakeGenericType(actualServiceType, r.ImplementingType) })
                .Where(t => t.closedGenericImplementingType != null);

                foreach (var constructableOpenGenericService in constructableOpenGenericServices)
                {
                    var serviceRegistration = new ServiceRegistration
                    {
                        ServiceType = actualServiceType,
                        ImplementingType = constructableOpenGenericService.closedGenericImplementingType,
                        ServiceName = constructableOpenGenericService.ServiceName,
                        Lifetime = CloneLifeTime(constructableOpenGenericService.Lifetime) ?? DefaultLifetime,
                    };
                    Register(serviceRegistration);
                }
            }

            List<Action<IEmitter>> emitMethods;

            if (options.EnableVariance && options.VarianceFilter(serviceType))
            {
                emitMethods = emitters
                    .Where(kv => actualServiceType.GetTypeInfo().IsAssignableFrom(kv.Key.GetTypeInfo()))
                    .SelectMany(kv => kv.Value).OrderBy(kv => kv.Key).Select(kv => kv.Value)
                    .ToList();
            }
            else
            {
                emitMethods = GetEmitMethods(actualServiceType).OrderBy(kv => kv.Key).Select(kv => kv.Value).ToList();
            }

            if (dependencyStack.Count > 0 && emitMethods.Contains(dependencyStack.Peek()))
            {
                emitMethods.Remove(dependencyStack.Peek());
            }

            return e => EmitEnumerable(emitMethods, actualServiceType, e);
        }

        private Action<IEmitter> CreateServiceEmitterBasedOnSingleNamedInstance(Type serviceType)
        {
            return GetEmitMethod(serviceType, GetEmitMethods(serviceType).First().Key);
        }

        private bool CanRedirectRequestForDefaultServiceToSingleNamedService(Type serviceType, string serviceName)
        {
            return string.IsNullOrEmpty(serviceName) && GetEmitMethods(serviceType).Count == 1;
        }

        private ConstructionInfo GetConstructionInfo(Registration registration)
        {
            return constructionInfoProvider.Value.GetConstructionInfo(registration);
        }

        private ThreadSafeDictionary<string, Action<IEmitter>> GetEmitMethods(Type serviceType)
        {
            return emitters.GetOrAdd(serviceType, s => new ThreadSafeDictionary<string, Action<IEmitter>>(StringComparer.CurrentCultureIgnoreCase));
        }

        private ThreadSafeDictionary<string, ServiceRegistration> GetAvailableServices(Type serviceType)
        {
            return availableServices.GetOrAdd(serviceType, s => new ThreadSafeDictionary<string, ServiceRegistration>(StringComparer.CurrentCultureIgnoreCase));
        }

        private ThreadSafeDictionary<string, Delegate> GetConstructorDependencyFactories(Type dependencyType)
        {
            return constructorDependencyFactories.GetOrAdd(
                dependencyType,
                d => new ThreadSafeDictionary<string, Delegate>(StringComparer.CurrentCultureIgnoreCase));
        }

        private ThreadSafeDictionary<string, Delegate> GetPropertyDependencyFactories(Type dependencyType)
        {
            return propertyDependencyFactories.GetOrAdd(
                dependencyType,
                d => new ThreadSafeDictionary<string, Delegate>(StringComparer.CurrentCultureIgnoreCase));
        }

        private void RegisterService(Type serviceType, Type implementingType, ILifetime lifetime, string serviceName)
        {
            Ensure.IsNotNull(serviceType, "type");
            Ensure.IsNotNull(implementingType, "implementingType");
            Ensure.IsNotNull(serviceName, "serviceName");
            EnsureConstructable(serviceType, implementingType);
            var serviceRegistration = new ServiceRegistration { ServiceType = serviceType, ImplementingType = implementingType, ServiceName = serviceName, Lifetime = lifetime ?? DefaultLifetime };
            Register(serviceRegistration);
        }

        private void EnsureConstructable(Type serviceType, Type implementingType)
        {
            if (implementingType.GetTypeInfo().ContainsGenericParameters)
            {
                try
                {
                    GenericArgumentMapper.Map(serviceType, implementingType).GetMappedArguments();
                }
                catch (InvalidOperationException ex)
                {
                    throw new ArgumentOutOfRangeException(nameof(implementingType), ex.Message);
                }
            }
            else
            if (!serviceType.GetTypeInfo().IsAssignableFrom(implementingType.GetTypeInfo()))
            {
                throw new ArgumentOutOfRangeException(nameof(implementingType), $"The implementing type {implementingType.FullName} is not assignable from {serviceType.FullName}.");
            }
        }

        private Action<IEmitter> ResolveEmitMethod(ServiceRegistration serviceRegistration)
        {
            return emitter =>
            {
                var serviceOverrides = overrides.Items.Where(so => so.CanOverride(serviceRegistration)).ToArray();
                foreach (var serviceOverride in serviceOverrides)
                {
                    serviceRegistration = serviceOverride.Execute(this, serviceRegistration);
                }

                if (serviceRegistration.Lifetime == null)
                {
                    EmitNewInstanceWithDecorators(serviceRegistration, emitter);
                }
                else
                {
                    EmitLifetime(serviceRegistration, e => EmitNewInstanceWithDecorators(serviceRegistration, e), emitter);
                }
            };
        }

        private void EmitNewInstanceWithDecorators(ServiceRegistration serviceRegistration, IEmitter emitter)
        {
            var serviceDecorators = GetDecorators(serviceRegistration);
            if (serviceDecorators.Length > 0)
            {
                EmitDecorators(serviceRegistration, serviceDecorators, emitter, dm => EmitNewInstance(serviceRegistration, dm));
            }
            else
            {
                EmitNewInstance(serviceRegistration, emitter);
            }

            var processors = initializers.Items.Where(i => i.Predicate(serviceRegistration)).ToArray();
            if (processors.Length == 0)
            {
                return;
            }

            LocalBuilder instanceVariable = emitter.DeclareLocal(serviceRegistration.ServiceType);
            emitter.Store(instanceVariable);
            foreach (var postProcessor in processors)
            {
                Type delegateType = postProcessor.Initialize.GetType();
                var delegateIndex = constants.Add(postProcessor.Initialize);
                emitter.PushConstant(delegateIndex, delegateType);

                var serviceFactoryIndex = constants.Add(this);
                emitter.PushConstant(serviceFactoryIndex, typeof(IServiceFactory));
                var scopeManagerIndex = CreateScopeManagerIndex();
                emitter.PushConstant(scopeManagerIndex, typeof(IScopeManager));
                emitter.PushArgument(1);
                emitter.Emit(OpCodes.Call, ServiceFactoryLoader.LoadServiceFactoryMethod);
                emitter.Push(instanceVariable);
                MethodInfo invokeMethod = delegateType.GetTypeInfo().GetDeclaredMethod("Invoke");
                emitter.Call(invokeMethod);
            }

            emitter.Push(instanceVariable);
        }

        private void EmitLifetime(ServiceRegistration serviceRegistration, Action<IEmitter> emitMethod, IEmitter emitter)
        {
            if (serviceRegistration.Lifetime is PerScopeLifetime)
            {
                int instanceDelegateIndex = servicesToDelegatesIndex.GetOrAdd(serviceRegistration, _ => CreateInstanceDelegateIndex(emitMethod));
                PushScope(emitter);

                emitter.Emit(OpCodes.Call, ScopeLoader.ValidateScopeMethod.MakeGenericMethod(serviceRegistration.ServiceType));

                // Push the getinstance delegate
                emitter.PushConstant(instanceDelegateIndex, typeof(GetInstanceDelegate));

                emitter.PushArgument(0);

                emitter.Push(instanceDelegateIndex);

                emitter.Call(ScopeLoader.GetScopedInstanceMethod);
            }
            else if (serviceRegistration.Lifetime is PerRequestLifeTime)
            {
                var scopeVariable = emitter.DeclareLocal(typeof(Scope));
                int instanceDelegateIndex = servicesToDelegatesIndex.GetOrAdd(serviceRegistration, _ => CreateInstanceDelegateIndex(emitMethod));
                var invokeMethod = typeof(GetInstanceDelegate).GetTypeInfo().GetDeclaredMethod("Invoke");
                emitter.PushConstant(instanceDelegateIndex, typeof(GetInstanceDelegate));
                emitter.PushArgument(0);

                PushScope(emitter);

                emitter.Store(scopeVariable);
                emitter.Push(scopeVariable);
                emitter.Emit(OpCodes.Callvirt, invokeMethod);
                emitter.Push(scopeVariable);
                emitter.Emit(OpCodes.Call, ScopeLoader.ValidateTrackedTransientMethod);
            }
            else
            {
                var nonClosingGetInstanceMethod = LifetimeHelper.GetNonClosingGetInstanceMethod(serviceRegistration.Lifetime.GetType());
                if (nonClosingGetInstanceMethod != null)
                {
                    int instanceDelegateIndex = servicesToDelegatesIndex.GetOrAdd(serviceRegistration, _ => CreateInstanceDelegateIndex(emitMethod));
                    int lifetimeIndex = CreateLifetimeIndex(serviceRegistration.Lifetime);
                    emitter.PushConstant(lifetimeIndex, serviceRegistration.Lifetime.GetType());
                    emitter.PushConstant(instanceDelegateIndex, typeof(GetInstanceDelegate));
                    PushScope(emitter);
                    emitter.PushArgument(0);
                    emitter.Call(nonClosingGetInstanceMethod);
                }
                else
                {
                    int instanceDelegateIndex = servicesToDelegatesIndex.GetOrAdd(serviceRegistration, _ => CreateInstanceDelegateIndex(emitMethod));

                    int lifetimeIndex = CreateLifetimeIndex(serviceRegistration.Lifetime);

                    var scopeVariable = emitter.DeclareLocal(typeof(Scope));

                    // Push the scope into the stack
                    PushScope(emitter);

                    // Store the scope
                    emitter.Store(scopeVariable);

                    // Push the lifetime onto the stack
                    emitter.PushConstant(lifetimeIndex, typeof(ILifetime));

                    emitter.PushConstant(instanceDelegateIndex, typeof(GetInstanceDelegate));

                    // Push the constants arguments
                    emitter.PushArgument(0);

                    // Push the scope
                    emitter.Push(scopeVariable);

                    // Create the scoped function
                    emitter.Emit(OpCodes.Call, FuncHelper.CreateScopedFuncMethod);

                    emitter.Push(scopeVariable);

                    emitter.Call(LifetimeHelper.GetInstanceMethod);
                }
            }

            if (IsNotServiceFactory(serviceRegistration.ServiceType))
            {
                disposableLifeTimes.Add(serviceRegistration.Lifetime);
            }

            bool IsNotServiceFactory(Type serviceType)
            {
                return !typeof(IServiceFactory).GetTypeInfo().IsAssignableFrom(serviceType.GetTypeInfo());
            }
        }

        private void PushScope(IEmitter emitter)
        {
            if (options.EnableCurrentScope)
            {
                int scopeManagerIndex = CreateScopeManagerIndex();

                // Push the scope into the stack
                emitter.PushArgument(1);

                // Push the scope manager into the stack.
                emitter.PushConstant(scopeManagerIndex, typeof(IScopeManager));

                // Get the scope
                emitter.Emit(OpCodes.Call, ScopeLoader.GetThisOrCurrentScopeMethod);
            }
            else
            {
                // Push the scope onto the stack.
                emitter.PushArgument(1);
            }
        }

        private int CreateScopeManagerIndex()
        {
            return constants.Add(ScopeManagerProvider.GetScopeManager(this));
        }

        private int CreateInstanceDelegateIndex(Action<IEmitter> emitMethod)
        {
            return constants.Add(CreateDynamicMethodDelegate(emitMethod));
        }

        private int CreateLifetimeIndex(ILifetime lifetime)
        {
            return constants.Add(lifetime);
        }

        private GetInstanceDelegate CreateDefaultDelegate(Type serviceType, bool throwError)
        {
            log.Info($"Compiling delegate for resolving service : {serviceType}");

            var instanceDelegate = CreateDelegate(serviceType, string.Empty, throwError);
            if (instanceDelegate == null)
            {
                return (args, scope) => null;
            }

            Interlocked.Exchange(ref delegates, delegates.Add(serviceType, instanceDelegate));
            return instanceDelegate;
        }

        private GetInstanceDelegate CreateNamedDelegate(Tuple<Type, string> key, bool throwError)
        {
            log.Info($"Compiling delegate for resolving service : {key.Item1}, name: {key.Item2}");
            var instanceDelegate = CreateDelegate(key.Item1, key.Item2, throwError);
            if (instanceDelegate == null)
            {
                return (args, scope) => null;
            }

            Interlocked.Exchange(ref namedDelegates, namedDelegates.Add(key, instanceDelegate));
            return instanceDelegate;
        }

        private GetInstanceDelegate CreateDelegate(Type serviceType, string serviceName, bool throwError)
        {
            lock (lockObject)
            {
                var serviceEmitter = GetEmitMethod(serviceType, serviceName);
                if (serviceEmitter == null && throwError)
                {
                    throw new InvalidOperationException(
                        string.Format("Unable to resolve type: {0}, service name: {1}", serviceType, serviceName));
                }

                if (serviceEmitter != null)
                {
                    try
                    {
                        return CreateDynamicMethodDelegate(serviceEmitter);
                    }
                    catch (InvalidOperationException ex)
                    {
                        dependencyStack.Clear();
                        throw new InvalidOperationException(
                            string.Format("Unable to resolve type: {0}, service name: {1}", serviceType, serviceName),
                            ex);
                    }
                }

                return null;
            }
        }

        private void RegisterValue(Type serviceType, object value, string serviceName)
        {
            var serviceRegistration = new ServiceRegistration
            {
                ServiceType = serviceType,
                ServiceName = serviceName,
                Value = value,
                Lifetime = new PerContainerLifetime(),
            };
            Register(serviceRegistration);
        }

        private void RegisterServiceFromLambdaExpression<TService>(Delegate factory, ILifetime lifetime, string serviceName)
        {
            var serviceRegistration = new ServiceRegistration
            {
                ServiceType = typeof(TService),
                FactoryExpression = factory,
                ServiceName = serviceName,
                Lifetime = lifetime ?? DefaultLifetime,
            };
            Register(serviceRegistration);
        }

        private struct ClosedGenericCandidate
        {
            public ClosedGenericCandidate(Type closedGenericImplentingType, ILifetime lifetime)
            {
                ClosedGenericImplentingType = closedGenericImplentingType;
                Lifetime = lifetime;
            }

            public Type ClosedGenericImplentingType { get; }

            public ILifetime Lifetime { get; }
        }

        private class Storage<T>
        {
            public T[] Items = new T[0];

            private readonly object lockObject = new object();

            public int Add(T value)
            {
                int index = Array.IndexOf(Items, value);
                if (index == -1)
                {
                    return TryAddValue(value);
                }

                return index;
            }

            private int TryAddValue(T value)
            {
                lock (lockObject)
                {
                    int index = Array.IndexOf(Items, value);
                    if (index == -1)
                    {
                        index = AddValue(value);
                    }

                    return index;
                }
            }

            private int AddValue(T value)
            {
                int index = Items.Length;
                T[] snapshot = CreateSnapshot();
                snapshot[index] = value;
                Items = snapshot;
                return index;
            }

            private T[] CreateSnapshot()
            {
                var snapshot = new T[Items.Length + 1];
                Array.Copy(Items, snapshot, Items.Length);
                return snapshot;
            }
        }

        private class PropertyDependencyDisabler : IPropertyDependencySelector
        {
            public IEnumerable<PropertyDependency> Execute(Type type)
            {
                return new PropertyDependency[0];
            }
        }

        private class DynamicMethodSkeleton : IMethodSkeleton
        {
            private IEmitter emitter;
            private DynamicMethod dynamicMethod;

            public DynamicMethodSkeleton(Type returnType, Type[] parameterTypes)
            {
                CreateDynamicMethod(returnType, parameterTypes);
            }

            public IEmitter GetEmitter()
            {
                return emitter;
            }

            public Delegate CreateDelegate(Type delegateType)
            {
                return dynamicMethod.CreateDelegate(delegateType);
            }

            // NOTE: altered from NET452 || NET46 || NETCOREAPP2_0
#if NETFRAMEWORK || NETCOREAPP
            private void CreateDynamicMethod(Type returnType, Type[] parameterTypes)
            {
                dynamicMethod = new DynamicMethod(
                    "DynamicMethod", returnType, parameterTypes, typeof(ServiceContainer).GetTypeInfo().Module, true);
                emitter = new Emitter(dynamicMethod.GetILGenerator(), parameterTypes);
            }
#endif
#if NETSTANDARD1_1 || NETSTANDARD1_3 || NETSTANDARD1_6 || NETSTANDARD2_0
            private void CreateDynamicMethod(Type returnType, Type[] parameterTypes)
            {
                dynamicMethod = new DynamicMethod(returnType, parameterTypes);
                emitter = new Emitter(dynamicMethod.GetILGenerator(), parameterTypes);
            }
#endif
        }

        private class ServiceRegistry<T> : ThreadSafeDictionary<Type, ThreadSafeDictionary<string, T>>
        {
        }

        private class FactoryRule
        {
            public Func<Type, string, bool> CanCreateInstance { get; set; }

            public Func<ServiceRequest, object> Factory { get; set; }

            public ILifetime LifeTime { get; set; }
        }

        private class Initializer
        {
            public Func<ServiceRegistration, bool> Predicate { get; set; }

            public Action<IServiceFactory, object> Initialize { get; set; }
        }

        private class ServiceOverride
        {
            private readonly object lockObject = new object();

            private readonly Func<IServiceFactory, ServiceRegistration, ServiceRegistration> serviceRegistrationFactory;

            private bool hasExecuted;

            public ServiceOverride(Func<ServiceRegistration, bool> canOverride, Func<IServiceFactory, ServiceRegistration, ServiceRegistration> serviceRegistrationFactory)
            {
                CanOverride = canOverride;
                this.serviceRegistrationFactory = serviceRegistrationFactory;
            }

            public Func<ServiceRegistration, bool> CanOverride { get; }

            [ExcludeFromCodeCoverage]
            public ServiceRegistration Execute(IServiceFactory serviceFactory, ServiceRegistration serviceRegistration)
            {
                // Excluded since the double checked lock is virtually impossible to produce.
                if (hasExecuted)
                {
                    return serviceRegistration;
                }
                else
                {
                    lock (lockObject)
                    {
                        if (hasExecuted)
                        {
                            return serviceRegistration;
                        }
                        else
                        {
                            hasExecuted = true;
                            var registration = serviceRegistrationFactory(serviceFactory, serviceRegistration);
                            return registration;
                        }
                    }
                }
            }
        }
    }

    /// <summary>
    /// A base class for implementing <see cref="IScopeManagerProvider"/>
    /// that ensures that only one <see cref="IScopeManager"/> is created.
    /// </summary>
    internal abstract class ScopeManagerProvider : IScopeManagerProvider
    {
        private readonly object lockObject = new object();

        private IScopeManager scopeManager;

        /// <inheritdoc/>
        public IScopeManager GetScopeManager(IServiceFactory serviceFactory)
        {
            if (scopeManager == null)
            {
                lock (lockObject)
                {
                    if (scopeManager == null)
                    {
                        scopeManager = CreateScopeManager(serviceFactory);
                    }
                }
            }

            return scopeManager;
        }

        /// <summary>
        /// Creates a new <see cref="IScopeManager"/> instance.
        /// </summary>
        /// <param name="serviceFactory">The <see cref="IServiceFactory"/> to be associated with the <see cref="IScopeManager"/>.</param>
        /// <returns><see cref="IScopeManager"/>.</returns>
        protected abstract IScopeManager CreateScopeManager(IServiceFactory serviceFactory);
    }

    /// <summary>
    /// A <see cref="IScopeManagerProvider"/> that provides a <see cref="PerThreadScopeManager"/> per thread.
    /// </summary>
    internal class PerThreadScopeManagerProvider : ScopeManagerProvider
    {
        /// <inheritdoc/>
        protected override IScopeManager CreateScopeManager(IServiceFactory serviceFactory)
        {
            return new PerThreadScopeManager(serviceFactory);
        }
    }

#if NET452 || NETSTANDARD1_3 || NETSTANDARD1_6 || NETSTANDARD2_0 || NET46 || NETCOREAPP2_0

    /// <summary>
    /// Manages a set of <see cref="Scope"/> instances.
    /// </summary>
    internal class PerLogicalCallContextScopeManager : ScopeManager
    {
        private readonly LogicalThreadStorage<Scope> currentScope = new LogicalThreadStorage<Scope>();

        /// <summary>
        /// Initializes a new instance of the <see cref="PerLogicalCallContextScopeManager"/> class.
        /// </summary>
        /// <param name="serviceFactory">The <see cref="IServiceFactory"/> to be associated with this <see cref="ScopeManager"/>.</param>
        public PerLogicalCallContextScopeManager(IServiceFactory serviceFactory)
            : base(serviceFactory)
        {
        }

        /// <inheritdoc/>
        public override Scope CurrentScope
        {
            get { return GetThisScopeOrFirstValidAncestor(currentScope.Value); }
            set { currentScope.Value = value; }
        }
    }

    /// <summary>
    /// A <see cref="IScopeManagerProvider"/> that creates an <see cref="IScopeManager"/>
    /// that is capable of managing scopes across async points.
    /// </summary>
    internal class PerLogicalCallContextScopeManagerProvider : ScopeManagerProvider
    {
        /// <inheritdoc/>
        protected override IScopeManager CreateScopeManager(IServiceFactory serviceFactory)
        {
            return new PerLogicalCallContextScopeManager(serviceFactory);
        }
    }
#endif

    /// <summary>
    /// A thread safe dictionary.
    /// </summary>
    /// <typeparam name="TKey">The type of the keys in the dictionary.</typeparam>
    /// <typeparam name="TValue">The type of the values in the dictionary.</typeparam>
    internal class ThreadSafeDictionary<TKey, TValue> : ConcurrentDictionary<TKey, TValue>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ThreadSafeDictionary{TKey,TValue}"/> class.
        /// </summary>
        public ThreadSafeDictionary()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ThreadSafeDictionary{TKey,TValue}"/> class using the
        /// given <see cref="IEqualityComparer{T}"/>.
        /// </summary>
        /// <param name="comparer">The <see cref="IEqualityComparer{T}"/> implementation to use when comparing keys.</param>
        public ThreadSafeDictionary(IEqualityComparer<TKey> comparer)
            : base(comparer)
        {
        }
    }

    /// <summary>
    /// A wrapper around <see cref="ConcurrentDictionary{TKey, TValue}"/> that ensures
    /// that the value factory is only executed once.
    /// </summary>
    /// <typeparam name="TKey">The type of the key.</typeparam>
    /// <typeparam name="TValue">The type of the value.</typeparam>
    internal class LazyConcurrentDictionary<TKey, TValue>
    {
        private readonly ConcurrentDictionary<TKey, Lazy<TValue>> concurrentDictionary;

        /// <summary>
        /// Initializes a new instance of the <see cref="LazyConcurrentDictionary{TKey, TValue}"/> class.
        /// </summary>
        public LazyConcurrentDictionary()
        {
            this.concurrentDictionary = new ConcurrentDictionary<TKey, Lazy<TValue>>();
        }

        /// <summary>
        /// Adds a key/value pair to the <see cref="LazyConcurrentDictionary{TKey, TValue}"/>
        /// by using the specified function if the key does not already exist, or returns
        /// the existing value if the key exists.
        /// </summary>
        /// <param name="key">The key of the element to add.</param>
        /// <param name="valueFactory">The function used to generate a value for the key.</param>
        /// <returns>The value for the key.</returns>
        public TValue GetOrAdd(TKey key, Func<TKey, TValue> valueFactory)
        {
            var lazyResult = this.concurrentDictionary.GetOrAdd(key, k => new Lazy<TValue>(() => valueFactory(k), LazyThreadSafetyMode.ExecutionAndPublication));
            return lazyResult.Value;
        }
    }

#if NETSTANDARD1_1 || NETSTANDARD1_3 || NETSTANDARD1_6 || NETSTANDARD2_0

    /// <summary>
    /// Defines and represents a dynamic method that can be compiled and executed.
    /// </summary>
    internal class DynamicMethod
    {
        private readonly Type returnType;

        private readonly Type[] parameterTypes;

        private readonly ParameterExpression[] parameters;

        private readonly ILGenerator generator;

        /// <summary>
        /// Initializes a new instance of the <see cref="DynamicMethod"/> class.
        /// </summary>
        /// <param name="returnType">A <see cref="Type"/> object that specifies the return type of the dynamic method.</param>
        /// <param name="parameterTypes">An array of <see cref="Type"/> objects specifying the types of the parameters of the dynamic method, or null if the method has no parameters.</param>
        public DynamicMethod(Type returnType, Type[] parameterTypes)
        {
            this.returnType = returnType;
            this.parameterTypes = parameterTypes;
            parameters = parameterTypes.Select(Expression.Parameter).ToArray();
            generator = new ILGenerator(parameters);
        }

        /// <summary>
        /// Completes the dynamic method and creates a delegate that can be used to execute it.
        /// </summary>
        /// <param name="delegateType">A delegate type whose signature matches that of the dynamic method.</param>
        /// <returns>A delegate of the specified type, which can be used to execute the dynamic method.</returns>
        public Delegate CreateDelegate(Type delegateType)
        {
            var lambda = Expression.Lambda(delegateType, generator.CurrentExpression, parameters);
            return lambda.Compile();
        }

        /// <summary>
        /// Completes the dynamic method and creates a delegate that can be used to execute it, specifying the delegate type and an object the delegate is bound to.
        /// </summary>
        /// <param name="delegateType">A delegate type whose signature matches that of the dynamic method, minus the first parameter.</param>
        /// <param name="target">An object the delegate is bound to. Must be of the same type as the first parameter of the dynamic method.</param>
        /// <returns>A delegate of the specified type, which can be used to execute the dynamic method with the specified target object.</returns>
        public Delegate CreateDelegate(Type delegateType, object target)
        {
            Type delegateTypeWithTargetParameter =
                Expression.GetDelegateType(parameterTypes.Concat(new[] { returnType }).ToArray());
            var lambdaWithTargetParameter = Expression.Lambda(
                delegateTypeWithTargetParameter, generator.CurrentExpression, true, parameters);

            Expression[] arguments = new Expression[] { Expression.Constant(target) }.Concat(parameters.Cast<Expression>().Skip(1)).ToArray();
            var invokeExpression = Expression.Invoke(lambdaWithTargetParameter, arguments);

            var lambda = Expression.Lambda(delegateType, invokeExpression, parameters.Skip(1));
            return lambda.Compile();
        }

        /// <summary>
        /// Returns a <see cref="ILGenerator"/> for the method.
        /// </summary>
        /// <returns>An <see cref="ILGenerator"/> object for the method.</returns>
        public ILGenerator GetILGenerator()
        {
            return generator;
        }
    }

    /// <summary>
    /// A generator that transforms <see cref="OpCodes"/> into an expression tree.
    /// </summary>
    internal class ILGenerator
    {
        private readonly ParameterExpression[] parameters;
        private readonly Stack<Expression> stack = new Stack<Expression>();
        private readonly List<LocalBuilder> locals = new List<LocalBuilder>();
        private readonly List<Expression> expressions = new List<Expression>();

        /// <summary>
        /// Initializes a new instance of the <see cref="ILGenerator"/> class.
        /// </summary>
        /// <param name="parameters">An array of parameters used by the target <see cref="DynamicMethod"/>.</param>
        public ILGenerator(ParameterExpression[] parameters)
        {
            this.parameters = parameters;
        }

        /// <summary>
        /// Gets the current expression based the emitted <see cref="OpCodes"/>.
        /// </summary>
        public Expression CurrentExpression
        {
            get
            {
                var variables = locals.Select(l => l.Variable).ToList();
                var ex = new List<Expression>(expressions) { stack.Peek() };
                return Expression.Block(variables, ex);
            }
        }

        /// <summary>
        /// Puts the specified instruction and metadata token for the specified constructor onto the Microsoft intermediate language (MSIL) stream of instructions.
        /// </summary>
        /// <param name="code">The MSIL instruction to be emitted onto the stream.</param>
        /// <param name="constructor">A <see cref="ConstructorInfo"/> representing a constructor.</param>
        public void Emit(OpCode code, ConstructorInfo constructor)
        {
            if (code == OpCodes.Newobj)
            {
                var parameterCount = constructor.GetParameters().Length;
                var expression = Expression.New(constructor, Pop(parameterCount));
                stack.Push(expression);
            }
            else
            {
                throw new NotSupportedException(code.ToString());
            }
        }

        /// <summary>
        /// Puts the specified instruction onto the stream of instructions.
        /// </summary>
        /// <param name="code">The Microsoft Intermediate Language (MSIL) instruction to be put onto the stream.</param>
        public void Emit(OpCode code)
        {
            if (code == OpCodes.Ldarg_0)
            {
                stack.Push(parameters[0]);
            }
            else if (code == OpCodes.Ldarg_1)
            {
                stack.Push(parameters[1]);
            }
            else if (code == OpCodes.Ldarg_2)
            {
                stack.Push(parameters[2]);
            }
            else if (code == OpCodes.Ldarg_3)
            {
                stack.Push(parameters[3]);
            }
            else if (code == OpCodes.Ldloc_0)
            {
                stack.Push(locals[0].Variable);
            }
            else if (code == OpCodes.Ldloc_1)
            {
                stack.Push(locals[1].Variable);
            }
            else if (code == OpCodes.Ldloc_2)
            {
                stack.Push(locals[2].Variable);
            }
            else if (code == OpCodes.Ldloc_3)
            {
                stack.Push(locals[3].Variable);
            }
            else if (code == OpCodes.Stloc_0)
            {
                Expression valueExpression = stack.Pop();
                var assignExpression = Expression.Assign(locals[0].Variable, valueExpression);
                expressions.Add(assignExpression);
            }
            else if (code == OpCodes.Stloc_1)
            {
                Expression valueExpression = stack.Pop();
                var assignExpression = Expression.Assign(locals[1].Variable, valueExpression);
                expressions.Add(assignExpression);
            }
            else if (code == OpCodes.Stloc_2)
            {
                Expression valueExpression = stack.Pop();
                var assignExpression = Expression.Assign(locals[2].Variable, valueExpression);
                expressions.Add(assignExpression);
            }
            else if (code == OpCodes.Stloc_3)
            {
                Expression valueExpression = stack.Pop();
                var assignExpression = Expression.Assign(locals[3].Variable, valueExpression);
                expressions.Add(assignExpression);
            }
            else if (code == OpCodes.Ldelem_Ref)
            {
                Expression[] indexes = { stack.Pop() };
                for (int i = 0; i < indexes.Length; i++)
                {
                    indexes[0] = Expression.Convert(indexes[i], typeof(int));
                }

                Expression array = stack.Pop();
                stack.Push(Expression.ArrayAccess(array, indexes));
            }
            else if (code == OpCodes.Ldlen)
            {
                Expression array = stack.Pop();
                stack.Push(Expression.ArrayLength(array));
            }
            else if (code == OpCodes.Conv_I4)
            {
                stack.Push(Expression.Convert(stack.Pop(), typeof(int)));
            }
            else if (code == OpCodes.Ldc_I4_0)
            {
                stack.Push(Expression.Constant(0, typeof(int)));
            }
            else if (code == OpCodes.Ldc_I4_1)
            {
                stack.Push(Expression.Constant(1, typeof(int)));
            }
            else if (code == OpCodes.Ldc_I4_2)
            {
                stack.Push(Expression.Constant(2, typeof(int)));
            }
            else if (code == OpCodes.Ldc_I4_3)
            {
                stack.Push(Expression.Constant(3, typeof(int)));
            }
            else if (code == OpCodes.Ldc_I4_4)
            {
                stack.Push(Expression.Constant(4, typeof(int)));
            }
            else if (code == OpCodes.Ldc_I4_5)
            {
                stack.Push(Expression.Constant(5, typeof(int)));
            }
            else if (code == OpCodes.Ldc_I4_6)
            {
                stack.Push(Expression.Constant(6, typeof(int)));
            }
            else if (code == OpCodes.Ldc_I4_7)
            {
                stack.Push(Expression.Constant(7, typeof(int)));
            }
            else if (code == OpCodes.Ldc_I4_8)
            {
                stack.Push(Expression.Constant(8, typeof(int)));
            }
            else if (code == OpCodes.Sub)
            {
                var right = stack.Pop();
                var left = stack.Pop();
                stack.Push(Expression.Subtract(left, right));
            }
            else if (code == OpCodes.Ret)
            {
            }
            else if (code == OpCodes.Ldnull)
            {
                stack.Push(Expression.Constant(null));
            }
            else
            {
                throw new NotSupportedException(code.ToString());
            }
        }

        /// <summary>
        /// Puts the specified instruction onto the Microsoft intermediate language (MSIL) stream followed by the index of the given local variable.
        /// </summary>
        /// <param name="code">The MSIL instruction to be emitted onto the stream.</param>
        /// <param name="localBuilder">A local variable.</param>
        public void Emit(OpCode code, LocalBuilder localBuilder)
        {
            if (code == OpCodes.Stloc)
            {
                Expression valueExpression = stack.Pop();
                var assignExpression = Expression.Assign(localBuilder.Variable, valueExpression);
                expressions.Add(assignExpression);
            }
            else if (code == OpCodes.Ldloc)
            {
                stack.Push(localBuilder.Variable);
            }
            else
            {
                throw new NotSupportedException(code.ToString());
            }
        }

        /// <summary>
        /// Puts the specified instruction and numerical argument onto the Microsoft intermediate language (MSIL) stream of instructions.
        /// </summary>
        /// <param name="code">The MSIL instruction to be put onto the stream.</param>
        /// <param name="arg">The numerical argument pushed onto the stream immediately after the instruction.</param>
        public void Emit(OpCode code, int arg)
        {
            if (code == OpCodes.Ldc_I4)
            {
                stack.Push(Expression.Constant(arg, typeof(int)));
            }
            else if (code == OpCodes.Ldarg)
            {
                stack.Push(parameters[arg]);
            }
            else if (code == OpCodes.Ldloc)
            {
                stack.Push(locals[arg].Variable);
            }
            else if (code == OpCodes.Stloc)
            {
                Expression valueExpression = stack.Pop();
                var assignExpression = Expression.Assign(locals[arg].Variable, valueExpression);
                expressions.Add(assignExpression);
            }
            else
            {
                throw new NotSupportedException(code.ToString());
            }
        }

        /// <summary>
        /// Puts the specified instruction and numerical argument onto the Microsoft intermediate language (MSIL) stream of instructions.
        /// </summary>
        /// <param name="code">The MSIL instruction to be put onto the stream.</param>
        /// <param name="arg">The numerical argument pushed onto the stream immediately after the instruction.</param>
        public void Emit(OpCode code, long arg)
        {
            if (code == OpCodes.Ldc_I8)
            {
                stack.Push(Expression.Constant(arg, typeof(long)));
            }
            else
            {
                throw new NotSupportedException(code.ToString());
            }
        }

        /// <summary>
        /// Pushes the argument value as a constant expression.
        /// </summary>
        /// <param name="arg">The argument value to be pushed.</param>
        /// <param name="type">The type of the argument value to be pushed.</param>
        public void PushConstantValue(object arg, Type type)
        {
            stack.Push(Expression.Constant(arg, type));
        }

        /// <summary>
        /// Puts the specified instruction and numerical argument onto the Microsoft intermediate language (MSIL) stream of instructions.
        /// </summary>
        /// <param name="code">The MSIL instruction to be put onto the stream.</param>
        /// <param name="arg">The numerical argument pushed onto the stream immediately after the instruction.</param>
        public void Emit(OpCode code, sbyte arg)
        {
            if (code == OpCodes.Ldc_I4_S)
            {
                stack.Push(Expression.Constant((int)arg, typeof(int)));
            }
            else
            {
                throw new NotSupportedException(code.ToString());
            }
        }

        /// <summary>
        /// Puts the specified instruction and numerical argument onto the Microsoft intermediate language (MSIL) stream of instructions.
        /// </summary>
        /// <param name="code">The MSIL instruction to be put onto the stream.</param>
        /// <param name="arg">The numerical argument pushed onto the stream immediately after the instruction.</param>
        public void Emit(OpCode code, byte arg)
        {
            if (code == OpCodes.Ldloc_S)
            {
                stack.Push(locals[arg].Variable);
            }
            else if (code == OpCodes.Ldarg_S)
            {
                stack.Push(parameters[arg]);
            }
            else if (code == OpCodes.Stloc_S)
            {
                Expression valueExpression = stack.Pop();
                var assignExpression = Expression.Assign(locals[arg].Variable, valueExpression);
                expressions.Add(assignExpression);
            }
            else
            {
                throw new NotSupportedException(code.ToString());
            }
        }

        /// <summary>
        /// Puts the specified instruction onto the Microsoft intermediate language (MSIL) stream followed by the metadata token for the given string.
        /// </summary>
        /// <param name="code">The MSIL instruction to be emitted onto the stream.</param>
        /// <param name="arg">The String to be emitted.</param>
        public void Emit(OpCode code, string arg)
        {
            if (code == OpCodes.Ldstr)
            {
                stack.Push(Expression.Constant(arg, typeof(string)));
            }
            else
            {
                throw new NotSupportedException(code.ToString());
            }
        }

        /// <summary>
        /// Declares a local variable of the specified type.
        /// </summary>
        /// <param name="type">A <see cref="Type"/> object that represents the type of the local variable.</param>
        /// <returns>The declared local variable.</returns>
        public LocalBuilder DeclareLocal(Type type)
        {
            var localBuilder = new LocalBuilder(type, locals.Count);
            locals.Add(localBuilder);
            return localBuilder;
        }

        /// <summary>
        /// Puts the specified instruction onto the Microsoft intermediate language (MSIL) stream followed by the metadata token for the given type.
        /// </summary>
        /// <param name="code">The MSIL instruction to be put onto the stream.</param>
        /// <param name="type">A <see cref="Type"/>.</param>
        public void Emit(OpCode code, Type type)
        {
            if (code == OpCodes.Newarr)
            {
                stack.Push(Expression.NewArrayBounds(type, Pop(1)));
            }
            else if (code == OpCodes.Stelem)
            {
                var value = stack.Pop();
                var index = stack.Pop();
                var array = stack.Pop();
                var arrayAccess = Expression.ArrayAccess(array, index);

                var assignExpression = Expression.Assign(arrayAccess, value);
                expressions.Add(assignExpression);
            }
            else if (code == OpCodes.Castclass)
            {
                stack.Push(Expression.Convert(stack.Pop(), type));
            }
            else if (code == OpCodes.Box)
            {
                stack.Push(Expression.Convert(stack.Pop(), typeof(object)));
            }
            else if (code == OpCodes.Unbox_Any)
            {
                stack.Push(Expression.Convert(stack.Pop(), type));
            }
            else
            {
                throw new NotSupportedException(code.ToString());
            }
        }

        /// <summary>
        /// Puts the specified instruction onto the Microsoft intermediate language (MSIL) stream followed by the metadata token for the given method.
        /// </summary>
        /// <param name="code">The MSIL instruction to be emitted onto the stream.</param>
        /// <param name="methodInfo">A <see cref="MethodInfo"/> representing a method.</param>
        public void Emit(OpCode code, MethodInfo methodInfo)
        {
            if (code == OpCodes.Callvirt || code == OpCodes.Call)
            {
                var parameterCount = methodInfo.GetParameters().Length;
                Expression[] arguments = Pop(parameterCount);

                MethodCallExpression methodCallExpression;

                if (!methodInfo.IsStatic)
                {
                    var instance = stack.Pop();
                    methodCallExpression = Expression.Call(instance, methodInfo, arguments);
                }
                else
                {
                    methodCallExpression = Expression.Call(null, methodInfo, arguments);
                }

                if (methodInfo.ReturnType == typeof(void))
                {
                    expressions.Add(methodCallExpression);
                }
                else
                {
                    stack.Push(methodCallExpression);
                }
            }
            else
            {
                throw new NotSupportedException(code.ToString());
            }
        }

        private Expression[] Pop(int numberOfElements)
        {
            var expressionsToPop = new Expression[numberOfElements];

            for (int i = 0; i < numberOfElements; i++)
            {
                expressionsToPop[i] = stack.Pop();
            }

            return expressionsToPop.Reverse().ToArray();
        }
    }

    /// <summary>
    /// Represents a local variable within a method or constructor.
    /// </summary>
    internal class LocalBuilder
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LocalBuilder"/> class.
        /// </summary>
        /// <param name="type">The <see cref="Type"/> of the variable that this <see cref="LocalBuilder"/> represents.</param>
        /// <param name="localIndex">The zero-based index of the local variable within the method body.</param>
        public LocalBuilder(Type type, int localIndex)
        {
            Variable = Expression.Parameter(type);
            LocalType = type;
            LocalIndex = localIndex;
        }

        /// <summary>
        /// Gets the <see cref="ParameterExpression"/> that represents the variable.
        /// </summary>
        public ParameterExpression Variable { get; private set; }

        /// <summary>
        /// Gets the type of the local variable.
        /// </summary>
        public Type LocalType { get; private set; }

        /// <summary>
        /// Gets the zero-based index of the local variable within the method body.
        /// </summary>
        public int LocalIndex { get; private set; }
    }
#endif

    /// <summary>
    /// Selects the <see cref="ConstructionInfo"/> from a given type that represents the most resolvable constructor.
    /// </summary>
    internal class MostResolvableConstructorSelector : IConstructorSelector
    {
        private readonly Func<Type, string, bool> canGetInstance;
        private readonly bool enableOptionalArguments;

        /// <summary>
        /// Initializes a new instance of the <see cref="MostResolvableConstructorSelector"/> class.
        /// </summary>
        /// <param name="canGetInstance">A function delegate that determines if a service type can be resolved.</param>
        /// <param name="enableOptionalArguments">Determines if optional arguments should be considered a resolvable dependency.</param>
        public MostResolvableConstructorSelector(Func<Type, string, bool> canGetInstance, bool enableOptionalArguments)
        {
            this.canGetInstance = canGetInstance;
            this.enableOptionalArguments = enableOptionalArguments;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MostResolvableConstructorSelector"/> class.
        /// </summary>
        /// <param name="canGetInstance">A function delegate that determines if a service type can be resolved.</param>
        public MostResolvableConstructorSelector(Func<Type, string, bool> canGetInstance)
        : this(canGetInstance, false)
        {
        }

        /// <inheritdoc/>
        public ConstructorInfo Execute(Type implementingType)
        {
            ConstructorInfo[] constructorCandidates = implementingType.GetTypeInfo().DeclaredConstructors.Where(c => c.IsPublic && !c.IsStatic).ToArray();
            if (constructorCandidates.Length == 0)
            {
                throw new InvalidOperationException("Missing public constructor for Type: " + implementingType.FullName);
            }

            if (constructorCandidates.Length == 1)
            {
                return constructorCandidates[0];
            }

            foreach (var constructorCandidate in constructorCandidates.OrderByDescending(c => c.GetParameters().Count()))
            {
                ParameterInfo[] parameters = constructorCandidate.GetParameters();
                if (CanCreateParameterDependencies(parameters))
                {
                    return constructorCandidate;
                }
            }

            throw new InvalidOperationException("No resolvable constructor found for Type: " + implementingType.FullName);
        }

        /// <summary>
        /// Gets the service name based on the given <paramref name="parameter"/>.
        /// </summary>
        /// <param name="parameter">The <see cref="ParameterInfo"/> for which to get the service name.</param>
        /// <returns>The name of the service for the given <paramref name="parameter"/>.</returns>
        protected virtual string GetServiceName(ParameterInfo parameter)
        {
            return parameter.Name;
        }

        private bool CanCreateParameterDependencies(IEnumerable<ParameterInfo> parameters)
        {
            return parameters.All(CanCreateParameterDependency);
        }

        private bool CanCreateParameterDependency(ParameterInfo parameterInfo)
        {
            return canGetInstance(parameterInfo.ParameterType, string.Empty) || canGetInstance(parameterInfo.ParameterType, GetServiceName(parameterInfo)) || (parameterInfo.HasDefaultValue && enableOptionalArguments);
        }
    }

    /// <summary>
    /// Selects the constructor dependencies for a given <see cref="ConstructorInfo"/>.
    /// </summary>
    internal class ConstructorDependencySelector : IConstructorDependencySelector
    {
        /// <inheritdoc/>
        public virtual IEnumerable<ConstructorDependency> Execute(ConstructorInfo constructor)
        {
            return
                constructor.GetParameters()
                    .OrderBy(p => p.Position)
                    .Select(
                        p =>
                            new ConstructorDependency
                            {
                                ServiceName = string.Empty,
                                ServiceType = p.ParameterType,
                                Parameter = p,
                                IsRequired = true,
                            });
        }
    }

    /// <summary>
    /// Selects the property dependencies for a given <see cref="Type"/>.
    /// </summary>
    internal class PropertyDependencySelector : IPropertyDependencySelector
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PropertyDependencySelector"/> class.
        /// </summary>
        /// <param name="propertySelector">The <see cref="IPropertySelector"/> that is
        /// responsible for selecting a list of injectable properties.</param>
        public PropertyDependencySelector(IPropertySelector propertySelector)
        {
            PropertySelector = propertySelector;
        }

        /// <summary>
        /// Gets the <see cref="IPropertySelector"/> that is responsible for selecting a
        /// list of injectable properties.
        /// </summary>
        protected IPropertySelector PropertySelector { get; private set; }

        /// <inheritdoc/>
        public virtual IEnumerable<PropertyDependency> Execute(Type type)
        {
            return PropertySelector.Execute(type).Select(
                p => new PropertyDependency { Property = p, ServiceName = string.Empty, ServiceType = p.PropertyType });
        }
    }

    /// <summary>
    /// Builds a <see cref="ConstructionInfo"/> instance based on the implementing <see cref="Type"/>.
    /// </summary>
    internal class TypeConstructionInfoBuilder : IConstructionInfoBuilder
    {
        private readonly IConstructorSelector constructorSelector;
        private readonly IConstructorDependencySelector constructorDependencySelector;
        private readonly IPropertyDependencySelector propertyDependencySelector;
        private readonly Func<Type, string, Delegate> getConstructorDependencyExpression;

        private readonly Func<Type, string, Delegate> getPropertyDependencyExpression;

        /// <summary>
        /// Initializes a new instance of the <see cref="TypeConstructionInfoBuilder"/> class.
        /// </summary>
        /// <param name="constructorSelector">The <see cref="IConstructorSelector"/> that is responsible
        /// for selecting the constructor to be used for constructor injection.</param>
        /// <param name="constructorDependencySelector">The <see cref="IConstructorDependencySelector"/> that is
        /// responsible for selecting the constructor dependencies for a given <see cref="ConstructionInfo"/>.</param>
        /// <param name="propertyDependencySelector">The <see cref="IPropertyDependencySelector"/> that is responsible
        /// for selecting the property dependencies for a given <see cref="Type"/>.</param>
        /// <param name="getConstructorDependencyExpression">A function delegate that returns the registered constructor dependency expression, if any.</param>
        /// <param name="getPropertyDependencyExpression">A function delegate that returns the registered property dependency expression, if any.</param>
        public TypeConstructionInfoBuilder(
            IConstructorSelector constructorSelector,
            IConstructorDependencySelector constructorDependencySelector,
            IPropertyDependencySelector propertyDependencySelector,
            Func<Type, string, Delegate> getConstructorDependencyExpression,
            Func<Type, string, Delegate> getPropertyDependencyExpression)
        {
            this.constructorSelector = constructorSelector;
            this.constructorDependencySelector = constructorDependencySelector;
            this.propertyDependencySelector = propertyDependencySelector;
            this.getConstructorDependencyExpression = getConstructorDependencyExpression;
            this.getPropertyDependencyExpression = getPropertyDependencyExpression;
        }

        /// <summary>
        /// Analyzes the <paramref name="registration"/> and returns a <see cref="ConstructionInfo"/> instance.
        /// </summary>
        /// <param name="registration">The <see cref="Registration"/> that represents the implementing type to analyze.</param>
        /// <returns>A <see cref="ConstructionInfo"/> instance.</returns>
        public ConstructionInfo Execute(Registration registration)
        {
            if (registration.FactoryExpression != null)
            {
                return new ConstructionInfo() { FactoryDelegate = registration.FactoryExpression };
            }

            var implementingType = registration.ImplementingType;
            var constructionInfo = new ConstructionInfo
            {
                ImplementingType = implementingType,
            };
            constructionInfo.PropertyDependencies.AddRange(GetPropertyDependencies(implementingType));
            constructionInfo.Constructor = constructorSelector.Execute(implementingType);
            constructionInfo.ConstructorDependencies.AddRange(GetConstructorDependencies(constructionInfo.Constructor));

            return constructionInfo;
        }

        private IEnumerable<ConstructorDependency> GetConstructorDependencies(ConstructorInfo constructorInfo)
        {
            var constructorDependencies = constructorDependencySelector.Execute(constructorInfo).ToArray();
            foreach (var constructorDependency in constructorDependencies)
            {
                constructorDependency.FactoryExpression =
                    getConstructorDependencyExpression(
                        constructorDependency.ServiceType,
                        constructorDependency.ServiceName);
            }

            return constructorDependencies;
        }

        private IEnumerable<PropertyDependency> GetPropertyDependencies(Type implementingType)
        {
            var propertyDependencies = propertyDependencySelector.Execute(implementingType).ToArray();
            foreach (var property in propertyDependencies)
            {
                property.FactoryExpression =
                    getPropertyDependencyExpression(
                        property.ServiceType,
                        property.ServiceName);
            }

            return propertyDependencies;
        }
    }

    /// <summary>
    /// Keeps track of a <see cref="ConstructionInfo"/> instance for each <see cref="Registration"/>.
    /// </summary>
    internal class ConstructionInfoProvider : IConstructionInfoProvider
    {
        private readonly IConstructionInfoBuilder constructionInfoBuilder;
        private readonly ThreadSafeDictionary<Registration, ConstructionInfo> cache = new ThreadSafeDictionary<Registration, ConstructionInfo>();

        /// <summary>
        /// Initializes a new instance of the <see cref="ConstructionInfoProvider"/> class.
        /// </summary>
        /// <param name="constructionInfoBuilder">The <see cref="IConstructionInfoBuilder"/> that
        /// is responsible for building a <see cref="ConstructionInfo"/> instance based on a given <see cref="Registration"/>.</param>
        public ConstructionInfoProvider(IConstructionInfoBuilder constructionInfoBuilder)
        {
            this.constructionInfoBuilder = constructionInfoBuilder;
        }

        /// <inheritdoc/>
        public ConstructionInfo GetConstructionInfo(Registration registration)
        {
            return cache.GetOrAdd(registration, constructionInfoBuilder.Execute);
        }
    }

    /// <summary>
    /// Contains information about a service request that originates from a rule based service registration.
    /// </summary>
    internal class ServiceRequest
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ServiceRequest"/> class.
        /// </summary>
        /// <param name="serviceType">The <see cref="Type"/> of the requested service.</param>
        /// <param name="serviceName">The name of the requested service.</param>
        /// <param name="serviceFactory">The <see cref="IServiceFactory"/> to be associated with this <see cref="ServiceRequest"/>.</param>
        public ServiceRequest(Type serviceType, string serviceName, IServiceFactory serviceFactory)
        {
            ServiceType = serviceType;
            ServiceName = serviceName;
            ServiceFactory = serviceFactory;
        }

        /// <summary>
        /// Gets the service type.
        /// </summary>
        public Type ServiceType { get; private set; }

        /// <summary>
        /// Gets the service name.
        /// </summary>
        public string ServiceName { get; private set; }

        /// <summary>
        /// Gets the <see cref="IServiceFactory"/> that is associated with this <see cref="ServiceRequest"/>.
        /// </summary>
        public IServiceFactory ServiceFactory { get; private set; }
    }

    /// <summary>
    /// Base class for concrete registrations within the service container.
    /// </summary>
    internal abstract class Registration
    {
        /// <summary>
        /// Gets or sets the service <see cref="Type"/>.
        /// </summary>
        public Type ServiceType { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="Type"/> that implements the <see cref="Registration.ServiceType"/>.
        /// </summary>
        public virtual Type ImplementingType { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="LambdaExpression"/> used to create a service instance.
        /// </summary>
        public Delegate FactoryExpression { get; set; }
    }

    /// <summary>
    /// Contains information about a registered decorator.
    /// </summary>
    internal class DecoratorRegistration : Registration
    {
        /// <summary>
        /// Gets or sets a function delegate that determines if the decorator can decorate the service
        /// represented by the supplied <see cref="ServiceRegistration"/>.
        /// </summary>
        public Func<ServiceRegistration, bool> CanDecorate { get; set; }

        /// <summary>
        /// Gets or sets a <see cref="Lazy{T}"/> that defers resolving of the decorators implementing type.
        /// </summary>
        public Func<IServiceFactory, ServiceRegistration, Type> ImplementingTypeFactory { get; set; }

        /// <summary>
        /// Gets or sets the index of this <see cref="DecoratorRegistration"/>.
        /// </summary>
        public int Index { get; set; }

        /// <summary>
        /// Gets a value indicating whether this registration has a deferred implementing type.
        /// </summary>
        public bool HasDeferredImplementingType
        {
            get
            {
                return ImplementingType == null && FactoryExpression == null;
            }
        }
    }

    /// <summary>
    /// Contains information about a registered service.
    /// </summary>
    internal class ServiceRegistration : Registration
    {
        /// <summary>
        /// Gets or sets the name of the service.
        /// </summary>
        public string ServiceName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the <see cref="ILifetime"/> instance that controls the lifetime of the service.
        /// </summary>
        public ILifetime Lifetime { get; set; }

        /// <summary>
        /// Gets or sets the value that represents the instance of the service.
        /// </summary>
        public object Value { get; set; }

        /// <summary>
        /// Serves as a hash function for a particular type.
        /// </summary>
        /// <returns>
        /// A hash code for the current <see cref="T:System.Object"/>.
        /// </returns>
        public override int GetHashCode()
        {
            return ServiceType.GetHashCode() ^ ServiceName.GetHashCode();
        }

        /// <summary>
        /// Determines whether the specified <see cref="T:System.Object"/> is equal to the current <see cref="T:System.Object"/>.
        /// </summary>
        /// <returns>
        /// True if the specified <see cref="T:System.Object"/> is equal to the current <see cref="T:System.Object"/>; otherwise, false.
        /// </returns>
        /// <param name="obj">The <see cref="T:System.Object"/> to compare with the current <see cref="T:System.Object"/>.</param>
        public override bool Equals(object obj)
        {
            if (!(obj is ServiceRegistration other))
            {
                return false;
            }

            var result = ServiceName == other.ServiceName && ServiceType == other.ServiceType;
            return result;
        }

        /// <summary>
        /// Returns a string representation of the <see cref="ServiceRegistration"/>.
        /// </summary>
        /// <returns>A string representation of the <see cref="ServiceRegistration"/>.</returns>
        public override string ToString()
        {
            var lifeTime = Lifetime?.ToString() ?? "Transient";
            return $"ServiceType: '{ServiceType}', ServiceName: '{ServiceName}', ImplementingType: '{ImplementingType}', Lifetime: '{lifeTime}'";
        }
    }

    /// <summary>
    /// Represents the result from mapping generic arguments.
    /// </summary>
    internal class GenericMappingResult
    {
        private readonly string[] genericParameterNames;
        private readonly IDictionary<string, Type> genericArgumentMap;
        private readonly Type genericServiceType;
        private readonly Type openGenericImplementingType;

        /// <summary>
        /// Initializes a new instance of the <see cref="GenericMappingResult"/> class.
        /// </summary>
        /// <param name="genericParameterNames">The name of the generic parameters found in the <paramref name="openGenericImplementingType"/>.</param>
        /// <param name="genericArgumentMap">A <see cref="IDictionary{TKey,TValue}"/> that contains the mapping
        /// between a parameter name and the corresponding parameter or argument from the <paramref name="genericServiceType"/>.</param>
        /// <param name="genericServiceType">The generic type containing the arguments/parameters to be mapped to the generic arguments/parameters of the <paramref name="openGenericImplementingType"/>.</param>
        /// <param name="openGenericImplementingType">The open generic implementing type.</param>
        internal GenericMappingResult(string[] genericParameterNames, IDictionary<string, Type> genericArgumentMap, Type genericServiceType, Type openGenericImplementingType)
        {
            this.genericParameterNames = genericParameterNames;
            this.genericArgumentMap = genericArgumentMap;
            this.genericServiceType = genericServiceType;
            this.openGenericImplementingType = openGenericImplementingType;
        }

        /// <summary>
        /// Gets a value indicating whether the <see cref="GenericMappingResult"/> is valid.
        /// </summary>
        public bool IsValid
        {
            get
            {
                if (!genericServiceType.GetTypeInfo().IsGenericType && openGenericImplementingType.GetTypeInfo().ContainsGenericParameters)
                {
                    return false;
                }

                return genericParameterNames.All(n => genericArgumentMap.ContainsKey(n));
            }
        }

        /// <summary>
        /// Gets a list of the mapped arguments/parameters.
        /// In the case of an closed generic service, this list can be used to
        /// create a new generic type from the open generic implementing type.
        /// </summary>
        /// <returns>A list of the mapped arguments/parameters.</returns>
        public Type[] GetMappedArguments()
        {
            var missingParameters = genericParameterNames.Where(n => !genericArgumentMap.ContainsKey(n)).ToArray();
            if (missingParameters.Any())
            {
                var missingParametersString = missingParameters.Aggregate((current, next) => current + "," + next);
                string message = $"The generic parameter(s) {missingParametersString} found in type {openGenericImplementingType.FullName} cannot be mapped from {genericServiceType.FullName}";
                throw new InvalidOperationException(message);
            }

            return genericParameterNames.Select(parameterName => genericArgumentMap[parameterName]).ToArray();
        }
    }

    /// <summary>
    /// Contains information about how to create a service instance.
    /// </summary>
    internal class ConstructionInfo
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ConstructionInfo"/> class.
        /// </summary>
        public ConstructionInfo()
        {
            PropertyDependencies = new List<PropertyDependency>();
            ConstructorDependencies = new List<ConstructorDependency>();
        }

        /// <summary>
        /// Gets or sets the implementing type that represents the concrete class to create.
        /// </summary>
        public Type ImplementingType { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="ConstructorInfo"/> that is used to create a service instance.
        /// </summary>
        public ConstructorInfo Constructor { get; set; }

        /// <summary>
        /// Gets a list of <see cref="PropertyDependency"/> instances that represent
        /// the property dependencies for the target service instance.
        /// </summary>
        public List<PropertyDependency> PropertyDependencies { get; private set; }

        /// <summary>
        /// Gets a list of <see cref="ConstructorDependency"/> instances that represent
        /// the property dependencies for the target service instance.
        /// </summary>
        public List<ConstructorDependency> ConstructorDependencies { get; private set; }

        /// <summary>
        /// Gets or sets the function delegate to be used to create the service instance.
        /// </summary>
        public Delegate FactoryDelegate { get; set; }
    }

    /// <summary>
    /// Represents a class dependency.
    /// </summary>
    internal abstract class Dependency
    {
        /// <summary>
        /// Gets or sets the service <see cref="Type"/> of the <see cref="Dependency"/>.
        /// </summary>
        public Type ServiceType { get; set; }

        /// <summary>
        /// Gets or sets the service name of the <see cref="Dependency"/>.
        /// </summary>
        public string ServiceName { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="FactoryExpression"/> that represent getting the value of the <see cref="Dependency"/>.
        /// </summary>
        public Delegate FactoryExpression { get; set; }

        /// <summary>
        /// Gets the name of the dependency accessor.
        /// </summary>
        public abstract string Name { get; }

        /// <summary>
        /// Gets or sets a value indicating whether this dependency is required.
        /// </summary>
        public bool IsRequired { get; set; }

        /// <summary>
        /// Returns textual information about the dependency.
        /// </summary>
        /// <returns>A string that describes the dependency.</returns>
        public override string ToString()
        {
            var sb = new StringBuilder();
            return sb.AppendFormat("[Requested dependency: ServiceType:{0}, ServiceName:{1}]", ServiceType, ServiceName).ToString();
        }
    }

    /// <summary>
    /// Represents a property dependency.
    /// </summary>
    internal class PropertyDependency : Dependency
    {
        /// <summary>
        /// Gets or sets the <see cref="MethodInfo"/> that is used to set the property value.
        /// </summary>
        public PropertyInfo Property { get; set; }

        /// <summary>
        /// Gets the name of the dependency accessor.
        /// </summary>
        public override string Name
        {
            get
            {
                return Property.Name;
            }
        }

        /// <summary>
        /// Returns textual information about the dependency.
        /// </summary>
        /// <returns>A string that describes the dependency.</returns>
        public override string ToString()
        {
            return string.Format("[Target Type: {0}], [Property: {1}({2})]", Property.DeclaringType, Property.Name, Property.PropertyType) + ", " + base.ToString();
        }
    }

    /// <summary>
    /// Represents a constructor dependency.
    /// </summary>
    internal class ConstructorDependency : Dependency
    {
        /// <summary>
        /// Gets or sets the <see cref="ParameterInfo"/> for this <see cref="ConstructorDependency"/>.
        /// </summary>
        public ParameterInfo Parameter { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether that this parameter represents
        /// the decoration target passed into a decorator instance.
        /// </summary>
        public bool IsDecoratorTarget { get; set; }

        /// <summary>
        /// Gets the name of the dependency accessor.
        /// </summary>
        public override string Name
        {
            get
            {
                return Parameter.Name;
            }
        }

        /// <summary>
        /// Returns textual information about the dependency.
        /// </summary>
        /// <returns>A string that describes the dependency.</returns>
        public override string ToString()
        {
            return string.Format("[Target Type: {0}], [Parameter: {1}({2})]", Parameter.Member.DeclaringType, Parameter.Name, Parameter.ParameterType) + ", " + base.ToString();
        }
    }

    /// <summary>
    /// Ensures that only one instance of a given service can exist within the current <see cref="IServiceContainer"/>.
    /// </summary>
    [LifeSpan(30)]
    internal class PerContainerLifetime : ILifetime, IDisposable, ICloneableLifeTime
    {
        private readonly object syncRoot = new object();
        private volatile object singleton;

        /// <inheritdoc/>
        public object GetInstance(Func<object> createInstance, Scope scope)
        {
            throw new NotImplementedException("Optimized");
        }

        /// <summary>
        /// An optimized non-closing version of the GetInstance method used to avoid closing over the "current" scope.
        /// </summary>
        /// <param name="createInstance">The delegate used to create the service instance.</param>
        /// <param name="scope">The current scope.</param>
        /// <param name="arguments">An array containing "constants" to be passed to the underlying dynamic method.</param>
        /// <returns>The service instance.</returns>
        public object GetInstance(GetInstanceDelegate createInstance, Scope scope, object[] arguments)
        {
            if (singleton != null)
            {
                return singleton;
            }

            lock (syncRoot)
            {
                if (singleton == null)
                {
                    singleton = createInstance(arguments, scope);
                }
            }

            return singleton;
        }

        /// <summary>
        /// Disposes the service instances managed by this <see cref="PerContainerLifetime"/> instance.
        /// </summary>
        public void Dispose()
        {
            if (singleton is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }

        /// <inheritdoc/>
        public ILifetime Clone()
        {
            return new PerContainerLifetime();
        }
    }

    /// <summary>
    /// Ensures that a new instance is created for each request in addition to tracking disposable instances.
    /// </summary>
    [LifeSpan(10)]
    internal class PerRequestLifeTime : ILifetime, ICloneableLifeTime
    {
        /// <inheritdoc/>
        public object GetInstance(Func<object> createInstance, Scope scope)
        {
            throw new NotImplementedException("Optimized");
        }

        /// <summary>
        /// Clones this lifetime.
        /// </summary>
        /// <returns>A new clone of this lifetime.</returns>
        public ILifetime Clone()
        {
            return new PerRequestLifeTime();
        }
    }

    /// <summary>
    /// Ensures that only one service instance can exist within a given <see cref="Scope"/>.
    /// </summary>
    /// <remarks>
    /// If the service instance implements <see cref="IDisposable"/>,
    /// it will be disposed when the <see cref="Scope"/> ends.
    /// </remarks>
    [LifeSpan(20)]
    internal class PerScopeLifetime : ILifetime, ICloneableLifeTime
    {
        /// <summary>
        /// Returns the same service instance within the current <see cref="Scope"/>.
        /// </summary>
        /// <param name="createInstance">The function delegate used to create a new service instance.</param>
        /// <param name="scope">The <see cref="Scope"/> of the current service request.</param>
        /// <returns>The requested services instance.</returns>
        public object GetInstance(Func<object> createInstance, Scope scope)
        {
            throw new NotImplementedException("Optimized");
        }

        /// <inheritdoc/>
        public ILifetime Clone()
        {
            return new PerScopeLifetime();
        }
    }

    /// <summary>
    /// A base class for implementing <see cref="IScopeManager"/>.
    /// </summary>
    internal abstract class ScopeManager : IScopeManager
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ScopeManager"/> class.
        /// </summary>
        /// <param name="serviceFactory">The <see cref="IServiceFactory"/> to be associated with this <see cref="ScopeManager"/>.</param>
        protected ScopeManager(IServiceFactory serviceFactory)
        {
            ServiceFactory = serviceFactory;
        }

        /// <inheritdoc/>
        public abstract Scope CurrentScope { get; set; }

        /// <inheritdoc/>
        public IServiceFactory ServiceFactory { get; }

        /// <inheritdoc/>
        public Scope BeginScope()
        {
            var currentScope = CurrentScope;

            var scope = new Scope(this, currentScope);

            CurrentScope = scope;
            return scope;
        }

        /// <inheritdoc/>
        public void EndScope(Scope scope)
        {
            Scope parentScope = scope.ParentScope;

            // Only update the current scope if the scope being
            // ended is the current scope.
            if (ReferenceEquals(CurrentScope, scope))
            {
                CurrentScope = parentScope;
            }
        }

        /// <summary>
        /// Ensures that we return a valid scope.
        /// </summary>
        /// <param name="scope">The scope to be validated.</param>
        /// <returns>The given <paramref name="scope"/> or the first valid ancestor.</returns>
        protected Scope GetThisScopeOrFirstValidAncestor(Scope scope)
        {
            // The scope could possible been disposed on another thread
            // or logical thread context.
            while (scope != null && scope.IsDisposed)
            {
                scope = scope.ParentScope;
            }

            // Update the current scope so that the previous current
            // scope can be garbage collected.
            CurrentScope = scope;
            return scope;
        }
    }

    /// <summary>
    /// A <see cref="IScopeManager"/> that manages scopes per thread.
    /// </summary>
    internal class PerThreadScopeManager : ScopeManager
    {
        private readonly ThreadLocal<Scope> threadLocalScope = new ThreadLocal<Scope>();

        /// <summary>
        /// Initializes a new instance of the <see cref="PerThreadScopeManager"/> class.
        /// </summary>
        /// <param name="serviceFactory">The <see cref="IServiceFactory"/> to be associated with this <see cref="ScopeManager"/>.</param>
        public PerThreadScopeManager(IServiceFactory serviceFactory)
            : base(serviceFactory)
        {
        }

        /// <inheritdoc/>
        public override Scope CurrentScope
        {
            get { return GetThisScopeOrFirstValidAncestor(threadLocalScope.Value); }
            set { threadLocalScope.Value = value; }
        }
    }

    /// <summary>
    /// Represents a scope.
    /// </summary>
    internal class Scope : IServiceFactory, IDisposable
    {
        /// <summary>
        /// Gets a value indicating whether this scope has been disposed.
        /// </summary>
        public bool IsDisposed;

        /// <summary>
        /// Gets the parent <see cref="Scope"/>.
        /// </summary>
        public Scope ParentScope;

        private readonly object lockObject = new object();

        private readonly IScopeManager scopeManager;

        private readonly ServiceContainer serviceFactory;

        private List<IDisposable> disposableObjects;

        private ImmutableMapTree<object> createdInstances = ImmutableMapTree<object>.Empty;

        /// <summary>
        /// Initializes a new instance of the <see cref="Scope"/> class.
        /// </summary>
        /// <param name="scopeManager">The <see cref="scopeManager"/> that manages this <see cref="Scope"/>.</param>
        /// <param name="parentScope">The parent <see cref="Scope"/>.</param>
        public Scope(IScopeManager scopeManager, Scope parentScope)
        {
            this.scopeManager = scopeManager;
            serviceFactory = (ServiceContainer)scopeManager.ServiceFactory;
            ParentScope = parentScope;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Scope"/> class.
        /// </summary>
        /// <param name="serviceFactory">The <see cref="IServiceFactory"/> that created this <see cref="Scope"/>.</param>
        public Scope(ServiceContainer serviceFactory)
        {
            this.serviceFactory = serviceFactory;
        }

        /// <summary>
        /// Raised when the <see cref="Scope"/> is completed.
        /// </summary>
        public event EventHandler<EventArgs> Completed;

        /// <summary>
        /// Registers the <paramref name="disposable"/> so that it is disposed when the scope is completed.
        /// </summary>
        /// <param name="disposable">The <see cref="IDisposable"/> object to register.</param>
        public void TrackInstance(IDisposable disposable)
        {
            lock (lockObject)
            {
                if (disposableObjects == null)
                {
                    disposableObjects = new List<IDisposable>();
                }

                disposableObjects.Add(disposable);
            }
        }

        /// <summary>
        /// Disposes all instances tracked by this scope.
        /// </summary>
        public void Dispose()
        {
            if (disposableObjects != null && disposableObjects.Count > 0)
            {
                HashSet<IDisposable> disposedObjects = new HashSet<IDisposable>(ReferenceEqualityComparer<IDisposable>.Default);

                for (var i = disposableObjects.Count - 1; i >= 0; i--)
                {
                    if (disposableObjects[i] is IDisposable disposable)
                    {
                        if (disposedObjects.Add(disposable))
                        {
                            disposable.Dispose();
                        }
                    }
                }
            }

            scopeManager?.EndScope(this);
            var completedHandler = Completed;
            completedHandler?.Invoke(this, new EventArgs());
            IsDisposed = true;
        }

        /// <inheritdoc/>
        public Scope BeginScope() => serviceFactory.BeginScope();

        /// <inheritdoc/>
        public object GetInstance(Type serviceType) =>
            serviceFactory.GetInstance(serviceType, this);

        /// <inheritdoc/>
        public object GetInstance(Type serviceType, string serviceName) =>
            serviceFactory.GetInstance(serviceType, this, serviceName);

        /// <inheritdoc/>
        public object GetInstance(Type serviceType, object[] arguments) =>
            serviceFactory.GetInstance(serviceType, arguments, this);

        /// <inheritdoc/>
        public object GetInstance(Type serviceType, string serviceName, object[] arguments)
            => serviceFactory.GetInstance(serviceType, serviceName, arguments, this);

        /// <inheritdoc/>
        public object TryGetInstance(Type serviceType)
            => serviceFactory.TryGetInstance(serviceType, this);

        /// <inheritdoc/>
        public object TryGetInstance(Type serviceType, string serviceName)
            => serviceFactory.TryGetInstance(serviceType, serviceName, this);

        /// <inheritdoc/>
        public IEnumerable<object> GetAllInstances(Type serviceType)
            => serviceFactory.GetAllInstances(serviceType, this);

        /// <inheritdoc/>
        public object Create(Type serviceType) => serviceFactory.Create(serviceType, this);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal object GetScopedInstance(GetInstanceDelegate getInstanceDelegate, object[] arguments, int instanceDelegateIndex)
        {
            var createdInstance = createdInstances.Search(instanceDelegateIndex);
            if (createdInstance != null)
            {
                return createdInstance;
            }

            lock (lockObject)
            {
                createdInstance = createdInstances.Search(instanceDelegateIndex);
                if (createdInstance == null)
                {
                    createdInstance = getInstanceDelegate(arguments, this);
                    if (createdInstance is IDisposable disposable)
                    {
                        TrackInstance(disposable);
                    }

                    Interlocked.Exchange(ref createdInstances, createdInstances.Add(instanceDelegateIndex, createdInstance));
                }

                return createdInstance;
            }
        }

        private class ReferenceEqualityComparer<T> : IEqualityComparer<T>
        {
            public static readonly ReferenceEqualityComparer<T> Default
                = new ReferenceEqualityComparer<T>();

            public bool Equals(T x, T y)
            {
                return ReferenceEquals(x, y);
            }

            public int GetHashCode(T obj)
            {
                return obj.GetHashCode();
            }
        }
    }

    /// <summary>
    /// Use to indicate the lifespan of a given <see cref="ILifetime"/>.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    internal class LifeSpanAttribute : Attribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LifeSpanAttribute"/> class.
        /// </summary>
        /// <param name="value">A value that indicates the lifespan of a given <see cref="ILifetime"/>.</param>
        public LifeSpanAttribute(int value)
        {
            Value = value;
        }

        /// <summary>
        /// Gets a value that indicates the lifespan of a given <see cref="ILifetime"/>.
        /// </summary>
        public int Value { get; }
    }

    /// <summary>
    /// Used at the assembly level to describe the composition root(s) for the target assembly.
    /// </summary>
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
    internal class CompositionRootTypeAttribute : Attribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CompositionRootTypeAttribute"/> class.
        /// </summary>
        /// <param name="compositionRootType">A <see cref="Type"/> that implements the <see cref="ICompositionRoot"/> interface.</param>
        public CompositionRootTypeAttribute(Type compositionRootType)
        {
            CompositionRootType = compositionRootType;
        }

        /// <summary>
        /// Gets the <see cref="Type"/> that implements the <see cref="ICompositionRoot"/> interface.
        /// </summary>
        public Type CompositionRootType { get; private set; }
    }

    /// <summary>
    /// A class that is capable of extracting attributes of type
    /// <see cref="CompositionRootTypeAttribute"/> from an <see cref="Assembly"/>.
    /// </summary>
    internal class CompositionRootAttributeExtractor : ICompositionRootAttributeExtractor
    {
        /// <summary>
        /// Gets a list of attributes of type <see cref="CompositionRootTypeAttribute"/> from
        /// the given <paramref name="assembly"/>.
        /// </summary>
        /// <param name="assembly">The assembly from which to extract
        /// <see cref="CompositionRootTypeAttribute"/> attributes.</param>
        /// <returns>A list of attributes of type <see cref="CompositionRootTypeAttribute"/>.</returns>
        public CompositionRootTypeAttribute[] GetAttributes(Assembly assembly)
        {
            return assembly.GetCustomAttributes(typeof(CompositionRootTypeAttribute))
                .Cast<CompositionRootTypeAttribute>().ToArray();
        }
    }

    /// <summary>
    /// Extracts concrete <see cref="ICompositionRoot"/> implementations from an <see cref="Assembly"/>.
    /// </summary>
    internal class CompositionRootTypeExtractor : ITypeExtractor
    {
        private readonly ICompositionRootAttributeExtractor compositionRootAttributeExtractor;

        /// <summary>
        /// Initializes a new instance of the <see cref="CompositionRootTypeExtractor"/> class.
        /// </summary>
        /// <param name="compositionRootAttributeExtractor">The <see cref="ICompositionRootAttributeExtractor"/>
        /// that is responsible for extracting attributes of type <see cref="CompositionRootTypeAttribute"/> from
        /// a given <see cref="Assembly"/>.</param>
        public CompositionRootTypeExtractor(ICompositionRootAttributeExtractor compositionRootAttributeExtractor) =>
            this.compositionRootAttributeExtractor = compositionRootAttributeExtractor;

        /// <summary>
        /// Extracts concrete <see cref="ICompositionRoot"/> implementations found in the given <paramref name="assembly"/>.
        /// </summary>
        /// <param name="assembly">The <see cref="Assembly"/> for which to extract types.</param>
        /// <returns>A set of concrete <see cref="ICompositionRoot"/> implementations found in the given <paramref name="assembly"/>.</returns>
        public Type[] Execute(Assembly assembly)
        {
            CompositionRootTypeAttribute[] compositionRootAttributes =
                compositionRootAttributeExtractor.GetAttributes(assembly);

            if (compositionRootAttributes.Length > 0)
            {
                return compositionRootAttributes.Select(a => a.CompositionRootType).ToArray();
            }

            return
                assembly.DefinedTypes.Where(
                        t => !t.IsAbstract && typeof(ICompositionRoot).GetTypeInfo().IsAssignableFrom(t))
                    .Cast<Type>()
                    .ToArray();
        }
    }

    /// <summary>
    /// A <see cref="ITypeExtractor"/> cache decorator.
    /// </summary>
    internal class CachedTypeExtractor : ITypeExtractor
    {
        private readonly ITypeExtractor typeExtractor;

        private readonly ThreadSafeDictionary<Assembly, Type[]> cache =
            new ThreadSafeDictionary<Assembly, Type[]>();

        /// <summary>
        /// Initializes a new instance of the <see cref="CachedTypeExtractor"/> class.
        /// </summary>
        /// <param name="typeExtractor">The target <see cref="ITypeExtractor"/>.</param>
        public CachedTypeExtractor(ITypeExtractor typeExtractor) => this.typeExtractor = typeExtractor;

        /// <summary>
        /// Extracts types found in the given <paramref name="assembly"/>.
        /// </summary>
        /// <param name="assembly">The <see cref="Assembly"/> for which to extract types.</param>
        /// <returns>A set of types found in the given <paramref name="assembly"/>.</returns>
        public Type[] Execute(Assembly assembly) => cache.GetOrAdd(assembly, typeExtractor.Execute);
    }

    /// <summary>
    /// Extracts concrete types from an <see cref="Assembly"/>.
    /// </summary>
    internal class ConcreteTypeExtractor : ITypeExtractor
    {
        private static readonly List<Type> InternalTypes = new List<Type>();

        static ConcreteTypeExtractor()
        {
            InternalTypes.Add(typeof(ConstructorDependency));
            InternalTypes.Add(typeof(PropertyDependency));
            InternalTypes.Add(typeof(ThreadSafeDictionary<,>));
            InternalTypes.Add(typeof(Scope));
            InternalTypes.Add(typeof(PerContainerLifetime));
            InternalTypes.Add(typeof(PerScopeLifetime));
            InternalTypes.Add(typeof(ServiceRegistration));
            InternalTypes.Add(typeof(DecoratorRegistration));
            InternalTypes.Add(typeof(ServiceRequest));
            InternalTypes.Add(typeof(Registration));
            InternalTypes.Add(typeof(ServiceContainer));
            InternalTypes.Add(typeof(ConstructionInfo));
#if NET452 || NET46 || NETSTANDARD1_6 || NETSTANDARD2_0 || NETCOREAPP2_0
            InternalTypes.Add(typeof(AssemblyLoader));
#endif
            InternalTypes.Add(typeof(TypeConstructionInfoBuilder));
            InternalTypes.Add(typeof(ConstructionInfoProvider));
            InternalTypes.Add(typeof(MostResolvableConstructorSelector));
            InternalTypes.Add(typeof(PerRequestLifeTime));
            InternalTypes.Add(typeof(PropertySelector));
            InternalTypes.Add(typeof(AssemblyScanner));
            InternalTypes.Add(typeof(ConstructorDependencySelector));
            InternalTypes.Add(typeof(PropertyDependencySelector));
            InternalTypes.Add(typeof(CompositionRootTypeAttribute));
            InternalTypes.Add(typeof(ConcreteTypeExtractor));
            InternalTypes.Add(typeof(CompositionRootExecutor));
            InternalTypes.Add(typeof(CompositionRootTypeExtractor));
            InternalTypes.Add(typeof(CachedTypeExtractor));
            InternalTypes.Add(typeof(ImmutableList<>));
            InternalTypes.Add(typeof(KeyValue<,>));
            InternalTypes.Add(typeof(ImmutableHashTree<,>));
            InternalTypes.Add(typeof(ImmutableHashTable<,>));
            InternalTypes.Add(typeof(PerThreadScopeManagerProvider));
            InternalTypes.Add(typeof(Emitter));
            InternalTypes.Add(typeof(Instruction));
            InternalTypes.Add(typeof(Instruction<>));
            InternalTypes.Add(typeof(GetInstanceDelegate));
            InternalTypes.Add(typeof(ContainerOptions));
            InternalTypes.Add(typeof(CompositionRootAttributeExtractor));
#if NET452 || NET46 || NETSTANDARD1_3 || NETSTANDARD1_6 || NETSTANDARD2_0 || NETCOREAPP2_0
            InternalTypes.Add(typeof(PerLogicalCallContextScopeManagerProvider));
            InternalTypes.Add(typeof(PerLogicalCallContextScopeManager));
            InternalTypes.Add(typeof(LogicalThreadStorage<>));
#endif
#if NETSTANDARD1_1 || NETSTANDARD1_3 || NETSTANDARD1_6 || NETSTANDARD2_0
            InternalTypes.Add(typeof(DynamicMethod));
            InternalTypes.Add(typeof(ILGenerator));
            InternalTypes.Add(typeof(LocalBuilder));
#endif
        }

        /// <summary>
        /// Extracts concrete types found in the given <paramref name="assembly"/>.
        /// </summary>
        /// <param name="assembly">The <see cref="Assembly"/> for which to extract types.</param>
        /// <returns>A set of concrete types found in the given <paramref name="assembly"/>.</returns>
        public Type[] Execute(Assembly assembly)
        {
            return
                assembly.DefinedTypes.Where(info => IsConcreteType(info))
                    .Except(InternalTypes.Select(i => i.GetTypeInfo()))
                    .Cast<Type>()
                    .ToArray();
        }

        private static bool IsConcreteType(TypeInfo typeInfo)
        {
            return typeInfo.IsClass
                   && !typeInfo.IsNestedPrivate
                   && !typeInfo.IsAbstract
                   && !Equals(typeInfo.Assembly, typeof(string).GetTypeInfo().Assembly)
                   && !IsCompilerGenerated(typeInfo);
        }

        private static bool IsCompilerGenerated(TypeInfo typeInfo) =>
            typeInfo.IsDefined(typeof(CompilerGeneratedAttribute), false);
    }

    /// <summary>
    /// A class that is responsible for instantiating and executing an <see cref="ICompositionRoot"/>.
    /// </summary>
    internal class CompositionRootExecutor : ICompositionRootExecutor
    {
        private readonly IServiceRegistry serviceRegistry;
        private readonly Func<Type, ICompositionRoot> activator;

        private readonly IList<Type> executedCompositionRoots = new List<Type>();

        private readonly object syncRoot = new object();

        /// <summary>
        /// Initializes a new instance of the <see cref="CompositionRootExecutor"/> class.
        /// </summary>
        /// <param name="serviceRegistry">The <see cref="IServiceRegistry"/> to be configured by the <see cref="ICompositionRoot"/>.</param>
        /// <param name="activator">The function delegate that is responsible for creating an instance of the <see cref="ICompositionRoot"/>.</param>
        public CompositionRootExecutor(IServiceRegistry serviceRegistry, Func<Type, ICompositionRoot> activator)
        {
            this.serviceRegistry = serviceRegistry;
            this.activator = activator;
        }

        /// <inheritdoc/>
        public void Execute(Type compositionRootType)
        {
            if (!executedCompositionRoots.Contains(compositionRootType))
            {
                lock (syncRoot)
                {
                    if (!executedCompositionRoots.Contains(compositionRootType))
                    {
                        executedCompositionRoots.Add(compositionRootType);
                        var compositionRoot = activator(compositionRootType);
                        compositionRoot.Compose(serviceRegistry);
                    }
                }
            }
        }

        /// <inheritdoc/>
        public void Execute<TCompositionRoot>(TCompositionRoot compositionRoot)
            where TCompositionRoot : ICompositionRoot
        {
            if (!executedCompositionRoots.Contains(typeof(TCompositionRoot)))
            {
                lock (syncRoot)
                {
                    if (!executedCompositionRoots.Contains(typeof(TCompositionRoot)))
                    {
                        executedCompositionRoots.Add(typeof(TCompositionRoot));
                        compositionRoot.Compose(serviceRegistry);
                    }
                }
            }
        }
    }

    /// <summary>
    /// A class that maps the generic arguments/parameters from a generic servicetype
    /// to a open generic implementing type.
    /// </summary>
    internal class GenericArgumentMapper : IGenericArgumentMapper
    {
        /// <summary>
        /// Maps the generic arguments/parameters from the <paramref name="genericServiceType"/>
        /// to the generic arguments/parameters in the <paramref name="openGenericImplementingType"/>.
        /// </summary>
        /// <param name="genericServiceType">The generic type containing the arguments/parameters to be mapped to the generic arguments/parameters of the <paramref name="openGenericImplementingType"/>.</param>
        /// <param name="openGenericImplementingType">The open generic implementing type.</param>
        /// <returns>A <see cref="GenericMappingResult"/>.</returns>
        public GenericMappingResult Map(Type genericServiceType, Type openGenericImplementingType)
        {
            string[] genericParameterNames =
                openGenericImplementingType.GetTypeInfo().GenericTypeParameters.Select(t => t.Name).ToArray();

            var genericArgumentMap = CreateMap(genericServiceType, openGenericImplementingType, genericParameterNames);

            return new GenericMappingResult(genericParameterNames, genericArgumentMap, genericServiceType, openGenericImplementingType);
        }

        /// <inheritdoc/>
        public Type TryMakeGenericType(Type genericServiceType, Type openGenericImplementingType)
        {
            var mappingResult = Map(genericServiceType, openGenericImplementingType);
            if (!mappingResult.IsValid)
            {
                return null;
            }
            else
            {
                try
                {
                    return openGenericImplementingType.MakeGenericType(mappingResult.GetMappedArguments());
                }
                catch (Exception)
                {
                    return null;
                }
            }
        }

        private static Dictionary<string, Type> CreateMap(Type genericServiceType, Type openGenericImplementingType, string[] genericParameterNames)
        {
            var genericArgumentMap = new Dictionary<string, Type>(genericParameterNames.Length);

            var genericArguments = GetGenericArgumentsOrParameters(genericServiceType);

            if (genericArguments.Length > 0)
            {
                genericServiceType = genericServiceType.GetTypeInfo().GetGenericTypeDefinition();
            }
            else
            {
                return genericArgumentMap;
            }

            Type baseTypeImplementingOpenGenericServiceType = GetBaseTypeImplementingGenericTypeDefinition(
                openGenericImplementingType,
                genericServiceType);

            Type[] baseTypeGenericArguments = GetGenericArgumentsOrParameters(baseTypeImplementingOpenGenericServiceType);

            MapGenericArguments(genericArguments, baseTypeGenericArguments, genericArgumentMap);
            return genericArgumentMap;
        }

        private static Type[] GetGenericArgumentsOrParameters(Type type)
        {
            TypeInfo typeInfo = type.GetTypeInfo();
            if (typeInfo.IsGenericTypeDefinition)
            {
                return typeInfo.GenericTypeParameters;
            }

            return typeInfo.GenericTypeArguments;
        }

        private static void MapGenericArguments(Type[] serviceTypeGenericArguments, Type[] baseTypeGenericArguments, IDictionary<string, Type> map)
        {
            for (int index = 0; index < baseTypeGenericArguments.Length; index++)
            {
                var baseTypeGenericArgument = baseTypeGenericArguments[index];
                var serviceTypeGenericArgument = serviceTypeGenericArguments[index];
                if (baseTypeGenericArgument.GetTypeInfo().IsGenericParameter)
                {
                    map[baseTypeGenericArgument.Name] = serviceTypeGenericArgument;
                }
                else if (baseTypeGenericArgument.GetTypeInfo().IsGenericType)
                {
                    if (serviceTypeGenericArgument.GetTypeInfo().IsGenericType)
                    {
                        MapGenericArguments(serviceTypeGenericArgument.GetTypeInfo().GenericTypeArguments, baseTypeGenericArgument.GetTypeInfo().GenericTypeArguments, map);
                    }
                    else
                    {
                        MapGenericArguments(serviceTypeGenericArguments, baseTypeGenericArgument.GetTypeInfo().GenericTypeArguments, map);
                    }
                }
            }
        }

        private static Type GetBaseTypeImplementingGenericTypeDefinition(Type implementingType, Type genericTypeDefinition)
        {
            Type baseTypeImplementingGenericTypeDefinition = null;

            if (genericTypeDefinition.GetTypeInfo().IsInterface)
            {
                baseTypeImplementingGenericTypeDefinition = implementingType
                    .GetTypeInfo().ImplementedInterfaces
                    .FirstOrDefault(i => i.GetTypeInfo().IsGenericType && i.GetTypeInfo().GetGenericTypeDefinition() == genericTypeDefinition);
            }
            else
            {
                Type baseType = implementingType;
                while (!ImplementsOpenGenericTypeDefinition(genericTypeDefinition, baseType) && baseType != typeof(object))
                {
                    baseType = baseType.GetTypeInfo().BaseType;
                }

                if (baseType != typeof(object))
                {
                    baseTypeImplementingGenericTypeDefinition = baseType;
                }
            }

            if (baseTypeImplementingGenericTypeDefinition == null)
            {
                throw new InvalidOperationException($"The generic type definition {genericTypeDefinition.FullName} not implemented by implementing type {implementingType.FullName}");
            }

            return baseTypeImplementingGenericTypeDefinition;
        }

        private static bool ImplementsOpenGenericTypeDefinition(Type genericTypeDefinition, Type baseType)
        {
            return baseType.GetTypeInfo().IsGenericType && baseType.GetTypeInfo().GetGenericTypeDefinition() == genericTypeDefinition;
        }
    }

    /// <summary>
    /// An assembly scanner that registers services based on the types contained within an <see cref="Assembly"/>.
    /// </summary>
    internal class AssemblyScanner : IAssemblyScanner
    {
        private readonly ITypeExtractor concreteTypeExtractor;
        private readonly ITypeExtractor compositionRootTypeExtractor;
        private readonly ICompositionRootExecutor compositionRootExecutor;
        private readonly IGenericArgumentMapper genericArgumentMapper;
        private Assembly currentAssembly;

        /// <summary>
        /// Initializes a new instance of the <see cref="AssemblyScanner"/> class.
        /// </summary>
        /// <param name="concreteTypeExtractor">The <see cref="ITypeExtractor"/> that is responsible for
        /// extracting concrete types from the assembly being scanned.</param>
        /// <param name="compositionRootTypeExtractor">The <see cref="ITypeExtractor"/> that is responsible for
        /// extracting <see cref="ICompositionRoot"/> implementations from the assembly being scanned.</param>
        /// <param name="compositionRootExecutor">The <see cref="ICompositionRootExecutor"/> that is
        /// responsible for creating and executing an <see cref="ICompositionRoot"/>.</param>
        /// <param name="genericArgumentMapper">The <see cref="IGenericArgumentMapper"/> that is responsible
        /// for determining if an open generic type can be created from the information provided by a given abstraction.</param>
        public AssemblyScanner(ITypeExtractor concreteTypeExtractor, ITypeExtractor compositionRootTypeExtractor, ICompositionRootExecutor compositionRootExecutor, IGenericArgumentMapper genericArgumentMapper)
        {
            this.concreteTypeExtractor = concreteTypeExtractor;
            this.compositionRootTypeExtractor = compositionRootTypeExtractor;
            this.compositionRootExecutor = compositionRootExecutor;
            this.genericArgumentMapper = genericArgumentMapper;
        }

        /// <inheritdoc/>
        public void Scan(Assembly assembly, IServiceRegistry serviceRegistry, Func<ILifetime> lifetimeFactory, Func<Type, Type, bool> shouldRegister, Func<Type, Type, string> serviceNameProvider)
        {
            Type[] concreteTypes = GetConcreteTypes(assembly);
            foreach (Type type in concreteTypes)
            {
                BuildImplementationMap(type, serviceRegistry, lifetimeFactory, shouldRegister, serviceNameProvider);
            }
        }

        /// <inheritdoc/>
        public void Scan(Assembly assembly, IServiceRegistry serviceRegistry)
        {
            Type[] compositionRootTypes = GetCompositionRootTypes(assembly);
            if (compositionRootTypes.Length > 0 && !Equals(currentAssembly, assembly))
            {
                currentAssembly = assembly;
                ExecuteCompositionRoots(compositionRootTypes);
            }
        }

        private static IEnumerable<Type> GetBaseTypes(Type concreteType)
        {
            Type baseType = concreteType;
            while (baseType != typeof(object) && baseType != null)
            {
                yield return baseType;
                baseType = baseType.GetTypeInfo().BaseType;
            }
        }

        private void ExecuteCompositionRoots(IEnumerable<Type> compositionRoots)
        {
            foreach (var compositionRoot in compositionRoots)
            {
                compositionRootExecutor.Execute(compositionRoot);
            }
        }

        private Type[] GetConcreteTypes(Assembly assembly)
        {
            return concreteTypeExtractor.Execute(assembly);
        }

        private Type[] GetCompositionRootTypes(Assembly assembly)
        {
            return compositionRootTypeExtractor.Execute(assembly);
        }

        private void BuildImplementationMap(Type implementingType, IServiceRegistry serviceRegistry, Func<ILifetime> lifetimeFactory, Func<Type, Type, bool> shouldRegister, Func<Type, Type, string> serviceNameProvider)
        {
            Type[] interfaces = implementingType.GetTypeInfo().ImplementedInterfaces.ToArray();
            foreach (Type interfaceType in interfaces)
            {
                if (shouldRegister(interfaceType, implementingType))
                {
                    RegisterInternal(interfaceType, implementingType, serviceRegistry, lifetimeFactory(), serviceNameProvider);
                }
            }

            foreach (Type baseType in GetBaseTypes(implementingType))
            {
                if (shouldRegister(baseType, implementingType))
                {
                    RegisterInternal(baseType, implementingType, serviceRegistry, lifetimeFactory(), serviceNameProvider);
                }
            }
        }

        private void RegisterInternal(Type serviceType, Type implementingType, IServiceRegistry serviceRegistry, ILifetime lifetime, Func<Type, Type, string> serviceNameProvider)
        {
            var serviceTypeInfo = serviceType.GetTypeInfo();
            if (implementingType.GetTypeInfo().ContainsGenericParameters)
            {
                if (!genericArgumentMapper.Map(serviceType, implementingType).IsValid)
                {
                    return;
                }
            }

            if (serviceTypeInfo.IsGenericType && serviceTypeInfo.ContainsGenericParameters)
            {
                serviceType = serviceTypeInfo.GetGenericTypeDefinition();
            }

            serviceRegistry.Register(serviceType, implementingType, serviceNameProvider(serviceType, implementingType), lifetime);
        }
    }

    /// <summary>
    /// A class that is capable of providing a service name
    /// to be used when a service is registered during assembly scanning.
    /// </summary>
    internal class ServiceNameProvider : IServiceNameProvider
    {
        /// <inheritdoc/>
        public string GetServiceName(Type serviceType, Type implementingType)
        {
            string implementingTypeName = implementingType.FullName;
            string serviceTypeName = serviceType.FullName;
            if (implementingType.GetTypeInfo().IsGenericTypeDefinition)
            {
                var regex = new Regex("((?:[a-z][a-z.]+))", RegexOptions.IgnoreCase);
                implementingTypeName = regex.Match(implementingTypeName).Groups[1].Value;
                serviceTypeName = regex.Match(serviceTypeName).Groups[1].Value;
            }

            if (serviceTypeName.Split('.').Last().Substring(1) == implementingTypeName.Split('.').Last())
            {
                implementingTypeName = string.Empty;
            }

            return implementingTypeName;
        }
    }

    /// <summary>
    /// Selects the properties that represents a dependency to the target <see cref="Type"/>.
    /// </summary>
    internal class PropertySelector : IPropertySelector
    {
        /// <summary>
        /// Selects properties that represents a dependency from the given <paramref name="type"/>.
        /// </summary>
        /// <param name="type">The <see cref="Type"/> for which to select the properties.</param>
        /// <returns>A list of properties that represents a dependency to the target <paramref name="type"/>.</returns>
        public IEnumerable<PropertyInfo> Execute(Type type)
        {
            return type.GetRuntimeProperties().Where(IsInjectable).ToList();
        }

        /// <summary>
        /// Determines if the <paramref name="propertyInfo"/> represents an injectable property.
        /// </summary>
        /// <param name="propertyInfo">The <see cref="PropertyInfo"/> that describes the target property.</param>
        /// <returns><b>true</b> if the property is injectable, otherwise <b>false</b>.</returns>
        protected virtual bool IsInjectable(PropertyInfo propertyInfo)
        {
            return !IsReadOnly(propertyInfo);
        }

        private static bool IsReadOnly(PropertyInfo propertyInfo)
        {
            return propertyInfo.SetMethod == null || propertyInfo.SetMethod.IsStatic || propertyInfo.SetMethod.IsPrivate || propertyInfo.GetIndexParameters().Length > 0;
        }
    }
#if NET452 || NET46 || NETCOREAPP2_0

    /// <summary>
    /// Loads all assemblies from the application base directory that matches the given search pattern.
    /// </summary>
    internal class AssemblyLoader : IAssemblyLoader
    {
        /// <inheritdoc/>
        public IEnumerable<Assembly> Load(string searchPattern)
        {
            string directory = Path.GetDirectoryName(new Uri(GetAssemblyCodeBasePath()).LocalPath);

            string[] searchPatterns = searchPattern.Split('|');
            foreach (string file in searchPatterns.SelectMany(sp => Directory.GetFiles(directory, sp)).Where(CanLoad))
            {
                yield return LoadAssembly(file);
            }
        }

        /// <summary>
        /// Indicates if the current <paramref name="fileName"/> represent a file that can be loaded.
        /// </summary>
        /// <param name="fileName">The name of the target file.</param>
        /// <returns><b>true</b> if the file can be loaded, otherwise <b>false</b>.</returns>
        protected virtual bool CanLoad(string fileName)
        {
            return true;
        }

        /// <summary>
        /// Loads <see cref="Assembly"/> for the file located in <paramref name="filename"/>.
        /// </summary>
        /// <param name="filename">Full path to the file.</param>
        /// <returns><see cref="Assembly"/> of the file.</returns>
        protected virtual Assembly LoadAssembly(string filename)
        {
            return Assembly.LoadFrom(filename);
        }

        /// <summary>
        /// Gets the path where the LightInject assembly is located.
        /// </summary>
        /// <returns>The path where the LightInject assembly is located.</returns>
        protected virtual string GetAssemblyCodeBasePath()
        {
            return typeof(ServiceContainer).Assembly.CodeBase;
        }
    }
#endif

#if NETSTANDARD1_6 || NETSTANDARD2_0
    /// <summary>
    /// Loads all assemblies from the application base directory that matches the given search pattern.
    /// </summary>
    internal class AssemblyLoader : IAssemblyLoader
    {
        /// <inheritdoc/>
        public IEnumerable<Assembly> Load(string searchPattern)
        {
            string directory = Path.GetDirectoryName(new Uri(GetAssemblyCodeBasePath()).LocalPath);
            if (directory != null)
            {
                string[] searchPatterns = searchPattern.Split('|');
                foreach (string file in searchPatterns.SelectMany(sp => Directory.GetFiles(directory, sp)).Where(CanLoad))
                {
                    yield return LoadAssembly(file);
                }
            }
        }

        /// <summary>
        /// Indicates if the current <paramref name="fileName"/> represent a file that can be loaded.
        /// </summary>
        /// <param name="fileName">The name of the target file.</param>
        /// <returns><b>true</b> if the file can be loaded, otherwise <b>false</b>.</returns>
        protected virtual bool CanLoad(string fileName)
        {
            return true;
        }

        /// <summary>
        /// Loads <see cref="Assembly"/> for the file located in <paramref name="filename"/>.
        /// </summary>
        /// <param name="filename">Full path to the file.</param>
        /// <returns><see cref="Assembly"/> of the file.</returns>
        protected virtual Assembly LoadAssembly(string filename)
        {
            FileInfo fileInfo = new FileInfo(filename);
            return Assembly.Load(new AssemblyName(fileInfo.Name.Replace(fileInfo.Extension, string.Empty)));
        }

        /// <summary>
        /// Gets the path where the LightInject assembly is located.
        /// </summary>
        /// <returns>The path where the LightInject assembly is located.</returns>
        protected virtual string GetAssemblyCodeBasePath()
        {
            return typeof(ServiceContainer).GetTypeInfo().Assembly.CodeBase;
        }
    }
#endif

    /// <summary>
    /// Defines an immutable representation of a key and a value.
    /// </summary>
    /// <typeparam name="TKey">The type of the key.</typeparam>
    /// <typeparam name="TValue">The type of the value.</typeparam>
    internal sealed class KeyValue<TKey, TValue>
    {
        /// <summary>
        /// The key of this <see cref="KeyValue{TKey,TValue}"/> instance.
        /// </summary>
        public readonly TKey Key;

        /// <summary>
        /// The key of this <see cref="KeyValue{TKey,TValue}"/> instance.
        /// </summary>
        public readonly TValue Value;

        /// <summary>
        /// Initializes a new instance of the <see cref="KeyValue{TKey,TValue}"/> class.
        /// </summary>
        /// <param name="key">The key of this <see cref="KeyValue{TKey,TValue}"/> instance.</param>
        /// <param name="value">The value of this <see cref="KeyValue{TKey,TValue}"/> instance.</param>
        public KeyValue(TKey key, TValue value)
        {
            Key = key;
            Value = value;
        }
    }

    /// <summary>
    /// Represents a simple "add only" immutable list.
    /// </summary>
    /// <typeparam name="T">The type of items contained in the list.</typeparam>
    internal sealed class ImmutableList<T>
    {
        /// <summary>
        /// Represents an empty <see cref="ImmutableList{T}"/>.
        /// </summary>
        public static readonly ImmutableList<T> Empty = new ImmutableList<T>();

        /// <summary>
        /// An array that contains the items in the <see cref="ImmutableList{T}"/>.
        /// </summary>
        public readonly T[] Items;

        /// <summary>
        /// The number of items in the <see cref="ImmutableList{T}"/>.
        /// </summary>
        public readonly int Count;

        /// <summary>
        /// Initializes a new instance of the <see cref="ImmutableList{T}"/> class.
        /// </summary>
        /// <param name="previousList">The list from which the previous items are copied.</param>
        /// <param name="value">The value to be added to the list.</param>
        public ImmutableList(ImmutableList<T> previousList, T value)
        {
            Items = new T[previousList.Items.Length + 1];
            Array.Copy(previousList.Items, Items, previousList.Items.Length);
            Items[Items.Length - 1] = value;
            Count = Items.Length;
        }

        private ImmutableList() => Items = new T[0];

        /// <summary>
        /// Creates a new <see cref="ImmutableList{T}"/> that contains the new <paramref name="value"/>.
        /// </summary>
        /// <param name="value">The value to be added to the new list.</param>
        /// <returns>A new <see cref="ImmutableList{T}"/> that contains the new <paramref name="value"/>.</returns>
        public ImmutableList<T> Add(T value) => new ImmutableList<T>(this, value);
    }

    /// <summary>
    /// A simple immutable add-only hash table.
    /// </summary>
    /// <typeparam name="TKey">The type of the key.</typeparam>
    /// <typeparam name="TValue">The type of the value.</typeparam>
    internal sealed class ImmutableHashTable<TKey, TValue>
    {
        /// <summary>
        /// An empty <see cref="ImmutableHashTree{TKey,TValue}"/>.
        /// </summary>
        public static readonly ImmutableHashTable<TKey, TValue> Empty = new ImmutableHashTable<TKey, TValue>();

        /// <summary>
        /// Gets the number of items stored in the hash table.
        /// </summary>
        public readonly int Count;

        /// <summary>
        /// Gets the hast table buckets.
        /// </summary>
        internal readonly ImmutableHashTree<TKey, TValue>[] Buckets;

        /// <summary>
        /// Gets the divisor used to calculate the bucket index from the hash code of the key.
        /// </summary>
        internal readonly int Divisor;

        /// <summary>
        /// Initializes a new instance of the <see cref="ImmutableHashTable{TKey,TValue}"/> class.
        /// </summary>
        /// <param name="previous">The "previous" hash table that contains already existing values.</param>
        /// <param name="key">The key to be associated with the value.</param>
        /// <param name="value">The value to be added to the tree.</param>
        internal ImmutableHashTable(ImmutableHashTable<TKey, TValue> previous, TKey key, TValue value)
        {
            this.Count = previous.Count + 1;
            if (previous.Count >= previous.Divisor)
            {
                this.Divisor = previous.Divisor * 2;
                this.Buckets = new ImmutableHashTree<TKey, TValue>[this.Divisor];
                InitializeBuckets(0, this.Divisor);
                this.AddExistingValues(previous);
            }
            else
            {
                this.Divisor = previous.Divisor;
                this.Buckets = new ImmutableHashTree<TKey, TValue>[this.Divisor];
                Array.Copy(previous.Buckets, this.Buckets, previous.Divisor);
            }

            var hashCode = key.GetHashCode();
            var bucketIndex = hashCode & (this.Divisor - 1);
            this.Buckets[bucketIndex] = this.Buckets[bucketIndex].Add(key, value);
        }

        /// <summary>
        /// Prevents a default instance of the <see cref="ImmutableHashTable{TKey,TValue}"/> class from being created.
        /// </summary>
        private ImmutableHashTable()
        {
            this.Buckets = new ImmutableHashTree<TKey, TValue>[2];
            this.Divisor = 2;
            InitializeBuckets(0, 2);
        }

        private void AddExistingValues(ImmutableHashTable<TKey, TValue> previous)
        {
            foreach (ImmutableHashTree<TKey, TValue> bucket in previous.Buckets)
            {
                foreach (var keyValue in bucket.InOrder())
                {
                    int hashCode = keyValue.Key.GetHashCode();
                    int bucketIndex = hashCode & (this.Divisor - 1);
                    this.Buckets[bucketIndex] = this.Buckets[bucketIndex].Add(keyValue.Key, keyValue.Value);
                }
            }
        }

        private void InitializeBuckets(int startIndex, int count)
        {
            for (int i = startIndex; i < count; i++)
            {
                this.Buckets[i] = ImmutableHashTree<TKey, TValue>.Empty;
            }
        }
    }

    /// <summary>
    /// A balanced binary search tree implemented as an AVL tree.
    /// </summary>
    /// <typeparam name="TKey">The type of the key.</typeparam>
    /// <typeparam name="TValue">The type of the value.</typeparam>
    internal sealed class ImmutableHashTree<TKey, TValue>
    {
        /// <summary>
        /// An empty <see cref="ImmutableHashTree{TKey,TValue}"/>.
        /// </summary>
        public static readonly ImmutableHashTree<TKey, TValue> Empty = new ImmutableHashTree<TKey, TValue>();

        /// <summary>
        /// The key of this <see cref="ImmutableHashTree{TKey,TValue}"/>.
        /// </summary>
        public readonly TKey Key;

        /// <summary>
        /// The value of this <see cref="ImmutableHashTree{TKey,TValue}"/>.
        /// </summary>
        public readonly TValue Value;

        /// <summary>
        /// The list of <see cref="KeyValue{TKey,TValue}"/> instances where the
        /// <see cref="KeyValue{TKey,TValue}.Key"/> has the same hash code as this <see cref="ImmutableHashTree{TKey,TValue}"/>.
        /// </summary>
        public readonly ImmutableList<KeyValue<TKey, TValue>> Duplicates;

        /// <summary>
        /// The hash code retrieved from the <see cref="Key"/>.
        /// </summary>
        public readonly int HashCode;

        /// <summary>
        /// The left node of this <see cref="ImmutableHashTree{TKey,TValue}"/>.
        /// </summary>
        public readonly ImmutableHashTree<TKey, TValue> Left;

        /// <summary>
        /// The right node of this <see cref="ImmutableHashTree{TKey,TValue}"/>.
        /// </summary>
        public readonly ImmutableHashTree<TKey, TValue> Right;

        /// <summary>
        /// The height of this node.
        /// </summary>
        /// <remarks>
        /// An empty node has a height of 0 and a node without children has a height of 1.
        /// </remarks>
        public readonly int Height;

        /// <summary>
        /// Indicates that this <see cref="ImmutableHashTree{TKey,TValue}"/> is empty.
        /// </summary>
        public readonly bool IsEmpty;

        /// <summary>
        /// Initializes a new instance of the <see cref="ImmutableHashTree{TKey,TValue}"/> class
        /// and adds a new entry in the <see cref="Duplicates"/> list.
        /// </summary>
        /// <param name="key">The key for this node.</param>
        /// <param name="value">The value for this node.</param>
        /// <param name="hashTree">The <see cref="ImmutableHashTree{TKey,TValue}"/> that contains existing duplicates.</param>
        public ImmutableHashTree(TKey key, TValue value, ImmutableHashTree<TKey, TValue> hashTree)
        {
            Duplicates = hashTree.Duplicates.Add(new KeyValue<TKey, TValue>(key, value));
            Key = hashTree.Key;
            Value = hashTree.Value;
            Height = hashTree.Height;
            HashCode = hashTree.HashCode;
            Left = hashTree.Left;
            Right = hashTree.Right;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ImmutableHashTree{TKey,TValue}"/> class.
        /// </summary>
        /// <param name="key">The key for this node.</param>
        /// <param name="value">The value for this node.</param>
        /// <param name="left">The left node.</param>
        /// <param name="right">The right node.</param>
        public ImmutableHashTree(TKey key, TValue value, ImmutableHashTree<TKey, TValue> left, ImmutableHashTree<TKey, TValue> right)
        {
            var balance = left.Height - right.Height;

            if (balance == -2)
            {
                if (right.IsLeftHeavy())
                {
                    right = RotateRight(right);
                }

                // Rotate left
                Key = right.Key;
                Value = right.Value;
                Left = new ImmutableHashTree<TKey, TValue>(key, value, left, right.Left);
                Right = right.Right;
            }
            else if (balance == 2)
            {
                if (left.IsRightHeavy())
                {
                    left = RotateLeft(left);
                }

                // Rotate right
                Key = left.Key;
                Value = left.Value;
                Right = new ImmutableHashTree<TKey, TValue>(key, value, left.Right, right);
                Left = left.Left;
            }
            else
            {
                Key = key;
                Value = value;
                Left = left;
                Right = right;
            }

            Height = 1 + Math.Max(Left.Height, Right.Height);

            Duplicates = ImmutableList<KeyValue<TKey, TValue>>.Empty;

            HashCode = Key.GetHashCode();
        }

        private ImmutableHashTree()
        {
            IsEmpty = true;
            Duplicates = ImmutableList<KeyValue<TKey, TValue>>.Empty;
        }

        private static ImmutableHashTree<TKey, TValue> RotateLeft(ImmutableHashTree<TKey, TValue> node)
        {
            return new ImmutableHashTree<TKey, TValue>(
                node.Right.Key,
                node.Right.Value,
                new ImmutableHashTree<TKey, TValue>(node.Key, node.Value, node.Left, node.Right.Left),
                node.Right.Right);
        }

        private static ImmutableHashTree<TKey, TValue> RotateRight(ImmutableHashTree<TKey, TValue> node)
        {
            return new ImmutableHashTree<TKey, TValue>(
                node.Left.Key,
                node.Left.Value,
                node.Left.Left,
                new ImmutableHashTree<TKey, TValue>(node.Key, node.Value, node.Left.Right, node.Right));
        }

        private bool IsLeftHeavy() => Left.Height > Right.Height;

        private bool IsRightHeavy() => Right.Height > Left.Height;
    }

    /// <summary>
    /// A balanced binary search tree implemented as an AVL tree
    /// where the key is an integer which means we don't need GetHashCode.
    /// </summary>
    /// <typeparam name="TValue">The type of the value.</typeparam>
    internal sealed class ImmutableMapTree<TValue>
    {
        /// <summary>
        /// An empty <see cref="ImmutableMapTree{TValue}"/>.
        /// </summary>
        public static readonly ImmutableMapTree<TValue> Empty = new ImmutableMapTree<TValue>();

        /// <summary>
        /// The key of this <see cref="ImmutableMapTree{TValue}"/>.
        /// </summary>
        public readonly int Key;

        /// <summary>
        /// The value of this <see cref="ImmutableMapTree{TValue}"/>.
        /// </summary>
        public readonly TValue Value;

        /// <summary>
        /// The left node of this <see cref="ImmutableMapTree{TValue}"/>.
        /// </summary>
        public readonly ImmutableMapTree<TValue> Left;

        /// <summary>
        /// The right node of this <see cref="ImmutableMapTree{TValue}"/>.
        /// </summary>
        public readonly ImmutableMapTree<TValue> Right;

        /// <summary>
        /// The height of this node.
        /// </summary>
        /// <remarks>
        /// An empty node has a height of 0 and a node without children has a height of 1.
        /// </remarks>
        public readonly int Height;

        /// <summary>
        /// Indicates that this <see cref="ImmutableMapTree{TValue}"/> is empty.
        /// </summary>
        public readonly bool IsEmpty;

        /// <summary>
        /// Initializes a new instance of the <see cref="ImmutableMapTree{TValue}"/> class.
        /// </summary>
        /// <param name="key">The key for this node.</param>
        /// <param name="value">The value for this node.</param>
        /// <param name="hashTree">The <see cref="ImmutableMapTree{TValue}"/> that contains existing duplicates.</param>
        public ImmutableMapTree(int key, TValue value, ImmutableMapTree<TValue> hashTree)
        {
            Key = key;
            Value = value;
            Height = hashTree.Height;
            Left = hashTree.Left;
            Right = hashTree.Right;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ImmutableMapTree{TValue}"/> class.
        /// </summary>
        /// <param name="key">The key for this node.</param>
        /// <param name="value">The value for this node.</param>
        /// <param name="left">The left node.</param>
        /// <param name="right">The right node.</param>
        public ImmutableMapTree(int key, TValue value, ImmutableMapTree<TValue> left, ImmutableMapTree<TValue> right)
        {
            var balance = left.Height - right.Height;

            if (balance == -2)
            {
                if (right.IsLeftHeavy())
                {
                    right = RotateRight(right);
                }

                // Rotate left
                Key = right.Key;
                Value = right.Value;
                Left = new ImmutableMapTree<TValue>(key, value, left, right.Left);
                Right = right.Right;
            }
            else if (balance == 2)
            {
                if (left.IsRightHeavy())
                {
                    left = RotateLeft(left);
                }

                // Rotate right
                Key = left.Key;
                Value = left.Value;
                Right = new ImmutableMapTree<TValue>(key, value, left.Right, right);
                Left = left.Left;
            }
            else
            {
                Key = key;
                Value = value;
                Left = left;
                Right = right;
            }

            Height = 1 + Math.Max(Left.Height, Right.Height);
        }

        private ImmutableMapTree()
        {
            IsEmpty = true;
        }

        private static ImmutableMapTree<TValue> RotateLeft(ImmutableMapTree<TValue> node)
        {
            return new ImmutableMapTree<TValue>(
                node.Right.Key,
                node.Right.Value,
                new ImmutableMapTree<TValue>(node.Key, node.Value, node.Left, node.Right.Left),
                node.Right.Right);
        }

        private static ImmutableMapTree<TValue> RotateRight(ImmutableMapTree<TValue> node)
        {
            return new ImmutableMapTree<TValue>(
                node.Left.Key,
                node.Left.Value,
                node.Left.Left,
                new ImmutableMapTree<TValue>(node.Key, node.Value, node.Left.Right, node.Right));
        }

        private bool IsLeftHeavy() => Left.Height > Right.Height;

        private bool IsRightHeavy() => Right.Height > Left.Height;
    }

    /// <summary>
    /// Represents an MSIL instruction to be emitted into a dynamic method.
    /// </summary>
    internal class Instruction
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Instruction"/> class.
        /// </summary>
        /// <param name="code">The <see cref="OpCode"/> to be emitted.</param>
        /// <param name="emitAction">The action to be performed against an <see cref="ILGenerator"/>
        /// when this <see cref="Instruction"/> is emitted.</param>
        public Instruction(OpCode code, Action<ILGenerator> emitAction)
        {
            Code = code;
            Emit = emitAction;
        }

        /// <summary>
        /// Gets the <see cref="OpCode"/> to be emitted.
        /// </summary>
        public OpCode Code { get; private set; }

        /// <summary>
        /// Gets the action to be performed against an <see cref="ILGenerator"/>
        /// when this <see cref="Instruction"/> is emitted.
        /// </summary>
        public Action<ILGenerator> Emit { get; private set; }

        /// <summary>
        /// Returns the string representation of an <see cref="Instruction"/>.
        /// </summary>
        /// <returns>The string representation of an <see cref="Instruction"/>.</returns>
        public override string ToString() => Code.ToString();
    }

    /// <summary>
    /// Represents an MSIL instruction to be emitted into a dynamic method.
    /// </summary>
    /// <typeparam name="T">The type of argument used in this instruction.</typeparam>
    internal class Instruction<T> : Instruction
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Instruction{T}"/> class.
        /// </summary>
        /// <param name="code">The <see cref="OpCode"/> to be emitted.</param>
        /// <param name="argument">The argument be passed along with the given <paramref name="code"/>.</param>
        /// <param name="emitAction">The action to be performed against an <see cref="ILGenerator"/>
        /// when this <see cref="Instruction"/> is emitted.</param>
        public Instruction(OpCode code, T argument, Action<ILGenerator> emitAction)
            : base(code, emitAction)
        {
            Argument = argument;
        }

        /// <summary>
        /// Gets the argument be passed along with the given <see cref="Instruction.Code"/>.
        /// </summary>
        public T Argument { get; private set; }

        /// <summary>
        /// Returns the string representation of an <see cref="Instruction{T}"/>.
        /// </summary>
        /// <returns>The string representation of an <see cref="Instruction{T}"/>.</returns>
        public override string ToString() => $"{base.ToString()} {Argument}";
    }

    /// <summary>
    /// An abstraction of the <see cref="ILGenerator"/> class that provides information
    /// about the <see cref="Type"/> currently on the stack.
    /// </summary>
    internal class Emitter : IEmitter
    {
        private readonly ILGenerator generator;

        private readonly Type[] parameterTypes;

        private readonly Stack<Type> stack = new Stack<Type>();

        private readonly List<LocalBuilder> variables = new List<LocalBuilder>();

        private readonly List<Instruction> instructions = new List<Instruction>();

        /// <summary>
        /// Initializes a new instance of the <see cref="Emitter"/> class.
        /// </summary>
        /// <param name="generator">The <see cref="ILGenerator"/> used to emit MSIL instructions.</param>
        /// <param name="parameterTypes">The list of parameter types used by the current dynamic method.</param>
        public Emitter(ILGenerator generator, Type[] parameterTypes)
        {
            this.generator = generator;
            this.parameterTypes = parameterTypes;
        }

        /// <inheritdoc/>
        public Type StackType => stack.Count == 0 ? null : stack.Peek();

        /// <inheritdoc/>
        public List<Instruction> Instructions => instructions;

        /// <inheritdoc/>
        public void Emit(OpCode code)
        {
            if (code == OpCodes.Ldarg_0)
            {
                stack.Push(parameterTypes[0]);
            }
            else if (code == OpCodes.Ldarg_1)
            {
                stack.Push(parameterTypes[1]);
            }
            else if (code == OpCodes.Ldarg_2)
            {
                stack.Push(parameterTypes[2]);
            }
            else if (code == OpCodes.Ldarg_3)
            {
                stack.Push(parameterTypes[3]);
            }
            else if (code == OpCodes.Ldloc_0)
            {
                stack.Push(variables[0].LocalType);
            }
            else if (code == OpCodes.Ldloc_1)
            {
                stack.Push(variables[1].LocalType);
            }
            else if (code == OpCodes.Ldloc_2)
            {
                stack.Push(variables[2].LocalType);
            }
            else if (code == OpCodes.Ldloc_3)
            {
                stack.Push(variables[3].LocalType);
            }
            else if (code == OpCodes.Stloc_0)
            {
                stack.Pop();
            }
            else if (code == OpCodes.Stloc_1)
            {
                stack.Pop();
            }
            else if (code == OpCodes.Stloc_2)
            {
                stack.Pop();
            }
            else if (code == OpCodes.Stloc_3)
            {
                stack.Pop();
            }
            else if (code == OpCodes.Ldelem_Ref)
            {
                stack.Pop();
                Type arrayType = stack.Pop();
                stack.Push(arrayType.GetElementType());
            }
            else if (code == OpCodes.Ldlen)
            {
                stack.Pop();
                stack.Push(typeof(int));
            }
            else if (code == OpCodes.Conv_I4)
            {
                stack.Pop();
                stack.Push(typeof(int));
            }
            else if (code == OpCodes.Ldc_I4_0)
            {
                stack.Push(typeof(int));
            }
            else if (code == OpCodes.Ldc_I4_1)
            {
                stack.Push(typeof(int));
            }
            else if (code == OpCodes.Ldc_I4_2)
            {
                stack.Push(typeof(int));
            }
            else if (code == OpCodes.Ldc_I4_3)
            {
                stack.Push(typeof(int));
            }
            else if (code == OpCodes.Ldc_I4_4)
            {
                stack.Push(typeof(int));
            }
            else if (code == OpCodes.Ldc_I4_5)
            {
                stack.Push(typeof(int));
            }
            else if (code == OpCodes.Ldc_I4_6)
            {
                stack.Push(typeof(int));
            }
            else if (code == OpCodes.Ldc_I4_7)
            {
                stack.Push(typeof(int));
            }
            else if (code == OpCodes.Ldc_I4_8)
            {
                stack.Push(typeof(int));
            }
            else if (code == OpCodes.Sub)
            {
                stack.Pop();
                stack.Pop();
                stack.Push(typeof(int));
            }
            else if (code == OpCodes.Ret)
            {
            }
            else if (code == OpCodes.Ldnull)
            {
                stack.Push(null);
            }
            else
            {
                throw new NotSupportedException(code.ToString());
            }

            instructions.Add(new Instruction(code, il => il.Emit(code)));
            if (code == OpCodes.Ret)
            {
                foreach (var instruction in instructions)
                {
                    instruction.Emit(generator);
                }
            }
        }

        /// <inheritdoc/>
        public void Emit(OpCode code, int arg)
        {
            if (code == OpCodes.Ldc_I4)
            {
                stack.Push(typeof(int));
            }
            else if (code == OpCodes.Ldarg)
            {
                stack.Push(parameterTypes[arg]);
            }
            else if (code == OpCodes.Ldloc)
            {
                stack.Push(variables[arg].LocalType);
            }
            else if (code == OpCodes.Ldloca)
            {
                stack.Push(variables[arg].LocalType.MakePointerType());
            }
            else if (code == OpCodes.Stloc)
            {
                stack.Pop();
            }
            else
            {
                throw new NotSupportedException(code.ToString());
            }

            instructions.Add(new Instruction<int>(code, arg, il => il.Emit(code, arg)));
        }

        /// <inheritdoc/>
        public void Emit(OpCode code, long arg)
        {
            if (code == OpCodes.Ldc_I8)
            {
                stack.Push(typeof(long));
            }
            else
            {
                throw new NotSupportedException(code.ToString());
            }

            instructions.Add(new Instruction<long>(code, arg, il => il.Emit(code, arg)));
        }

        /// <inheritdoc/>
        public void Emit(OpCode code, sbyte arg)
        {
            if (code == OpCodes.Ldc_I4_S)
            {
                stack.Push(typeof(int));
            }
            else
            {
                throw new NotSupportedException(code.ToString());
            }

            instructions.Add(new Instruction<int>(code, arg, il => il.Emit(code, arg)));
        }

        /// <inheritdoc/>
        public void Emit(OpCode code, byte arg)
        {
            if (code == OpCodes.Ldloc_S)
            {
                stack.Push(variables[arg].LocalType);
            }
            else if (code == OpCodes.Ldarg_S)
            {
                stack.Push(parameterTypes[arg]);
            }
            else if (code == OpCodes.Stloc_S)
            {
                stack.Pop();
            }
            else
            {
                throw new NotSupportedException(code.ToString());
            }

            instructions.Add(new Instruction<byte>(code, arg, il => il.Emit(code, arg)));
        }

        /// <inheritdoc/>
        public void Emit(OpCode code, Type type)
        {
            if (code == OpCodes.Newarr)
            {
                stack.Pop();
                stack.Push(type.MakeArrayType());
            }
            else if (code == OpCodes.Stelem)
            {
                stack.Pop();
                stack.Pop();
                stack.Pop();
            }
            else if (code == OpCodes.Castclass)
            {
                stack.Pop();
                stack.Push(type);
            }
            else if (code == OpCodes.Box)
            {
                stack.Pop();
                stack.Push(typeof(object));
            }
            else if (code == OpCodes.Unbox_Any)
            {
                stack.Pop();
                stack.Push(type);
            }
            else if (code == OpCodes.Initobj)
            {
                stack.Pop();
            }
            else
            {
                throw new NotSupportedException(code.ToString());
            }

            instructions.Add(new Instruction<Type>(code, type, il => il.Emit(code, type)));
        }

        /// <inheritdoc/>
        public void Emit(OpCode code, ConstructorInfo constructor)
        {
            if (code == OpCodes.Newobj)
            {
                var parameterCount = constructor.GetParameters().Length;
                for (int i = 0; i < parameterCount; i++)
                {
                    stack.Pop();
                }

                stack.Push(constructor.DeclaringType);
            }
            else
            {
                throw new NotSupportedException(code.ToString());
            }

            instructions.Add(new Instruction<ConstructorInfo>(code, constructor, il => il.Emit(code, constructor)));
        }

        /// <inheritdoc/>
        public void Emit(OpCode code, LocalBuilder localBuilder)
        {
            if (code == OpCodes.Stloc)
            {
                stack.Pop();
            }
            else if (code == OpCodes.Ldloc)
            {
                stack.Push(localBuilder.LocalType);
            }
            else
            {
                throw new NotSupportedException(code.ToString());
            }

            instructions.Add(new Instruction<LocalBuilder>(code, localBuilder, il => il.Emit(code, localBuilder)));
        }

        /// <inheritdoc/>
        public void Emit(OpCode code, MethodInfo methodInfo)
        {
            if (code == OpCodes.Callvirt || code == OpCodes.Call)
            {
                var parameterCount = methodInfo.GetParameters().Length;
                for (int i = 0; i < parameterCount; i++)
                {
                    stack.Pop();
                }

                if (!methodInfo.IsStatic)
                {
                    stack.Pop();
                }

                if (methodInfo.ReturnType != typeof(void))
                {
                    stack.Push(methodInfo.ReturnType);
                }
            }
            else
            {
                throw new NotSupportedException(code.ToString());
            }

            instructions.Add(new Instruction<MethodInfo>(code, methodInfo, il => il.Emit(code, methodInfo)));
        }

        /// <inheritdoc/>
        public LocalBuilder DeclareLocal(Type type)
        {
            var localBuilder = generator.DeclareLocal(type);
            variables.Add(localBuilder);
            return localBuilder;
        }

        /// <inheritdoc/>
        public void Emit(OpCode code, string arg)
        {
            if (code == OpCodes.Ldstr)
            {
                stack.Push(typeof(string));
            }
            else
            {
                throw new NotSupportedException(code.ToString());
            }

            instructions.Add(new Instruction<string>(code, arg, il => il.Emit(code, arg)));
        }

#if NETSTANDARD1_1 || NETSTANDARD1_3 || NETSTANDARD1_6 || NETSTANDARD2_0
        /// <inheritdoc/>
        public void PushConstantValue(object arg, Type type)
        {
            stack.Push(type);
            instructions.Add(new Instruction<object>(OpCodes.Nop, arg, il => il.PushConstantValue(arg, type)));
        }

#endif
    }

#if NET452

    /// <summary>
    /// Provides storage per logical thread of execution.
    /// </summary>
    /// <typeparam name="T">The type of the value contained in this <see cref="LogicalThreadStorage{T}"/>.</typeparam>
    internal class LogicalThreadStorage<T>
    {
        private readonly string key = Guid.NewGuid().ToString();

        /// <summary>
        /// Gets or sets the value for the current logical thread of execution.
        /// </summary>
        /// <value>
        /// The value for the current logical thread of execution.
        /// </value>
        public T Value
        {
            get
            {
                var logicalThreadValue = (LogicalThreadValue)CallContext.LogicalGetData(key);
                return logicalThreadValue != null ? logicalThreadValue.Value : default(T);
            }

            set
            {
                LogicalThreadValue logicalThreadValue = null;
                if (value != null)
                {
                    logicalThreadValue = new LogicalThreadValue { Value = value };
                }

                CallContext.LogicalSetData(key, logicalThreadValue);
            }
        }

        [Serializable]
        private class LogicalThreadValue : MarshalByRefObject
        {
            [NonSerialized]
            private T value;

            public T Value
            {
                get
                {
                    return value;
                }

                set
                {
                    this.value = value;
                }
            }
        }
    }
#endif

#if NETSTANDARD1_1 || NETSTANDARD1_3 || NETSTANDARD1_6 || NETSTANDARD2_0
    /// <summary>
    /// An attribute shim since we don't have this attribute in netstandard.
    /// </summary>
    internal class ExcludeFromCodeCoverageAttribute : Attribute
    {
    }
#endif

#if NETSTANDARD1_3 || NETSTANDARD1_6 || NETSTANDARD2_0 || NET46 || NETCOREAPP2_0
    /// <summary>
    /// Provides storage per logical thread of execution.
    /// </summary>
    /// <typeparam name="T">The type of the value contained in this <see cref="LogicalThreadStorage{T}"/>.</typeparam>
    internal class LogicalThreadStorage<T>
    {
        private readonly AsyncLocal<T> asyncLocal = new AsyncLocal<T>();

        /// <summary>
        /// Gets or sets the value for the current logical thread of execution.
        /// </summary>
        /// <value>
        /// The value for the current logical thread of execution.
        /// </value>
        public T Value
        {
            get { return asyncLocal.Value; }
            set { asyncLocal.Value = value; }
        }
    }
#endif

    internal static class LifetimeHelper
    {
        public static readonly MethodInfo GetInstanceMethod;

        public static readonly MethodInfo GetCurrentScopeMethod;

        private static readonly ThreadSafeDictionary<Type, MethodInfo> NonClosingGetInstanceMethods
            = new ThreadSafeDictionary<Type, MethodInfo>();

        static LifetimeHelper()
        {
            GetInstanceMethod = typeof(ILifetime).GetTypeInfo().GetDeclaredMethod("GetInstance");
            GetCurrentScopeMethod = typeof(IScopeManager).GetTypeInfo().GetDeclaredProperty("CurrentScope").GetMethod;
        }

        public static MethodInfo GetNonClosingGetInstanceMethod(Type lifetimeType)
            => NonClosingGetInstanceMethods.GetOrAdd(lifetimeType, ResolveNonClosingGetInstanceMethod);

        private static MethodInfo ResolveNonClosingGetInstanceMethod(Type lifetimeType)
        {
            Type[] parameterTypes = { typeof(GetInstanceDelegate), typeof(Scope), typeof(object[]) };
            return lifetimeType.GetTypeInfo().DeclaredMethods.SingleOrDefault(m => m.GetParameters().Select(p => p.ParameterType).SequenceEqual(parameterTypes));
        }
    }

    internal static class ScopeLoader
    {
        public static readonly MethodInfo GetThisOrCurrentScopeMethod;

        public static readonly MethodInfo GetScopedInstanceMethod;

        public static readonly MethodInfo ValidateScopeMethod;

        public static readonly MethodInfo ValidateTrackedTransientMethod;

        static ScopeLoader()
        {
            GetThisOrCurrentScopeMethod = typeof(ScopeLoader).GetTypeInfo().GetDeclaredMethod("GetThisOrCurrentScope");
            GetScopedInstanceMethod = typeof(Scope).GetTypeInfo().GetDeclaredMethod("GetScopedInstance");
            ValidateScopeMethod = typeof(ScopeLoader).GetTypeInfo().GetDeclaredMethod("ValidateScope");
            ValidateTrackedTransientMethod = typeof(ScopeLoader).GetTypeInfo().GetDeclaredMethod("ValidateTrackedTransient");
        }

        public static object ValidateTrackedTransient(object instance, Scope scope)
        {
            if (instance is IDisposable disposable)
            {
                if (scope == null)
                {
                    string message = $@"The disposable instance ({instance.GetType()}) was created outside a scope. If 'ContainerOptions.EnableCurrentScope=false',
the service must be requested directly from the scope. If `ContainerOptions.EnableCurrentScope=true`, the service can be requested from the container,
but either way the scope has to be started with 'container.BeginScope()'";
                    throw new InvalidOperationException(message);
                }

                scope.TrackInstance(disposable);
            }

            return instance;
        }

        public static Scope ValidateScope<TService>(Scope scope)
        {
            if (scope == null)
            {
                string message = $@"Attempt to create a scoped instance ({typeof(TService)}) outside a scope. If 'ContainerOptions.EnableCurrentScope=false',
the service must be requested directly from the scope. If `ContainerOptions.EnableCurrentScope=true`, the service can be requested from the container,
but either way the scope has to be started with 'container.BeginScope()'";
                throw new InvalidOperationException(message);
            }

            return scope;
        }

        public static Scope GetThisOrCurrentScope(Scope scope, IScopeManager scopemanager)
        {
            if (scope != null)
            {
                return scope;
            }

            return scopemanager.CurrentScope;
        }
    }

    internal static class FuncHelper
    {
        public static readonly MethodInfo CreateScopedFuncMethod;

        public static readonly MethodInfo CreateScopedGenericFuncMethod;

        public static readonly MethodInfo CreateScopedGenericNamedFuncMethod;

        static FuncHelper()
        {
            CreateScopedFuncMethod = typeof(FuncHelper).GetTypeInfo().GetDeclaredMethod("CreateScopedFunc");
            CreateScopedGenericFuncMethod = typeof(FuncHelper).GetTypeInfo().GetDeclaredMethod("CreateScopedGenericFunc");
            CreateScopedGenericNamedFuncMethod = typeof(FuncHelper).GetTypeInfo().GetDeclaredMethod("CreateScopedGenericNamedFunc");
        }

        public static Func<object> CreateScopedFunc(GetInstanceDelegate getInstanceDelegate, object[] constants, Scope scope)
            => () => getInstanceDelegate(constants, scope);

        public static Func<T> CreateScopedGenericFunc<T>(ServiceContainer serviceContainer, Scope scope)
            => () => (T)serviceContainer.GetInstance(typeof(T), scope);

        public static Func<string, T> CreateScopedGenericNamedFunc<T>(ServiceContainer serviceContainer, Scope scope)
            => (serviceName) => (T)serviceContainer.GetInstance(typeof(T), scope, serviceName);
    }

    internal static class ServiceFactoryLoader
    {
        public static readonly MethodInfo LoadServiceFactoryMethod;

        static ServiceFactoryLoader()
            => LoadServiceFactoryMethod = typeof(ServiceFactoryLoader).GetTypeInfo().GetDeclaredMethod("LoadServiceFactory");

        public static IServiceFactory LoadServiceFactory(IServiceFactory serviceFactory, IScopeManager scopeManager, Scope scope)
        {
            if (scope != null)
            {
                return scope;
            }

            var currentScope = scopeManager.CurrentScope;
            if (currentScope != null)
            {
                return currentScope;
            }

            return serviceFactory;
        }
    }

    internal static class ServiceRequestHelper
    {
        public static readonly MethodInfo CreateServiceRequestMethod;

        static ServiceRequestHelper()
            => CreateServiceRequestMethod = typeof(ServiceRequestHelper).GetTypeInfo().GetDeclaredMethod("CreateServiceRequest");

        public static ServiceRequest CreateServiceRequest<TService>(string serviceName, IServiceFactory serviceFactory)
            => new ServiceRequest(typeof(TService), serviceName, serviceFactory);
    }

    internal static class LazyHelper
    {
        public static readonly MethodInfo CreateScopedLazyMethod;

        public static readonly MethodInfo CreateScopedLazyFromDelegateMethod;

        static LazyHelper()
        {
            CreateScopedLazyMethod = typeof(LazyHelper).GetTypeInfo().GetDeclaredMethod("CreateScopedLazy");
            CreateScopedLazyFromDelegateMethod = typeof(LazyHelper).GetTypeInfo().GetDeclaredMethod("CreateScopedLazyFromDelegate");
        }

        public static Lazy<T> CreateScopedLazy<T>(ServiceContainer serviceContainer, Scope scope)
            => new Lazy<T>(() => (T)serviceContainer.GetInstance(typeof(T), scope));

        public static Lazy<T> CreateScopedLazyFromDelegate<T>(GetInstanceDelegate getInstanceDelegate, object[] constants, Scope scope)
            => new Lazy<T>(() => (T)getInstanceDelegate(constants, scope));
    }

    internal static class ReflectionHelper
    {
        private static readonly Lazy<ThreadSafeDictionary<Type, MethodInfo>> GetInstanceWithParametersMethods;

        static ReflectionHelper() => GetInstanceWithParametersMethods = CreateLazyGetInstanceWithParametersMethods();

        public static MethodInfo GetGetInstanceWithParametersMethod(Type serviceType)
            => GetInstanceWithParametersMethods.Value.GetOrAdd(serviceType, CreateGetInstanceWithParametersMethod);

        public static Delegate CreateGetNamedInstanceWithParametersDelegate(IServiceFactory factory, Type delegateType, string serviceName)
        {
            Type[] genericTypeArguments = delegateType.GetTypeInfo().GenericTypeArguments;
            var openGenericMethod =
                typeof(ReflectionHelper).GetTypeInfo().DeclaredMethods
                    .Single(
                        m =>
                            m.GetGenericArguments().Length == genericTypeArguments.Length
                            && m.Name == "CreateGenericGetNamedParameterizedInstanceDelegate");
            var closedGenericMethod = openGenericMethod.MakeGenericMethod(genericTypeArguments);
            return (Delegate)closedGenericMethod.Invoke(null, new object[] { factory, serviceName });
        }

        private static Lazy<ThreadSafeDictionary<Type, MethodInfo>> CreateLazyGetInstanceWithParametersMethods()
            => new Lazy<ThreadSafeDictionary<Type, MethodInfo>>(() => new ThreadSafeDictionary<Type, MethodInfo>());

        private static MethodInfo CreateGetInstanceWithParametersMethod(Type serviceType)
        {
            Type[] genericTypeArguments = serviceType.GetTypeInfo().GenericTypeArguments;
            MethodInfo openGenericMethod =
                typeof(ServiceFactoryExtensions).GetTypeInfo().DeclaredMethods.Single(m => m.Name == "GetInstance"
                                                                                           && m.GetGenericArguments().Length == genericTypeArguments.Length && m.GetParameters().All(p => p.Name != "serviceName"));

            MethodInfo closedGenericMethod = openGenericMethod.MakeGenericMethod(genericTypeArguments);

            return closedGenericMethod;
        }
#pragma warning disable IDE0051

        private static Func<TArg, TService> CreateGenericGetNamedParameterizedInstanceDelegate<TArg, TService>(IServiceFactory factory, string serviceName)
            => arg => factory.GetInstance<TArg, TService>(arg, serviceName);

        private static Func<TArg1, TArg2, TService> CreateGenericGetNamedParameterizedInstanceDelegate<TArg1, TArg2, TService>(IServiceFactory factory, string serviceName)
            => (arg1, arg2) => factory.GetInstance<TArg1, TArg2, TService>(arg1, arg2, serviceName);

        private static Func<TArg1, TArg2, TArg3, TService> CreateGenericGetNamedParameterizedInstanceDelegate<TArg1, TArg2, TArg3, TService>(IServiceFactory factory, string serviceName)
            => (arg1, arg2, arg3) => factory.GetInstance<TArg1, TArg2, TArg3, TService>(arg1, arg2, arg3, serviceName);

        private static Func<TArg1, TArg2, TArg3, TArg4, TService> CreateGenericGetNamedParameterizedInstanceDelegate<TArg1, TArg2, TArg3, TArg4, TService>(IServiceFactory factory, string serviceName)
            => (arg1, arg2, arg3, arg4) => factory.GetInstance<TArg1, TArg2, TArg3, TArg4, TService>(arg1, arg2, arg3, arg4, serviceName);
    }

#pragma warning restore IDE0051

    /// <summary>
    /// Contains a set of extension method that represents
    /// a compability layer for reflection methods.
    /// </summary>
    internal static class TypeHelper
    {
        /// <summary>
        /// Attempts to create a generic type from the <paramref name="openGenericType"/> using <paramref name="closedGenericArguments"/>.
        /// </summary>
        /// <param name="openGenericType">The open generic type for which to create a generic type.</param>
        /// <param name="closedGenericArguments">The generic arguments used to create the generic type.</param>
        /// <returns>The closed generic type if successful, otherwise null.</returns>
        public static Type TryMakeGenericType(Type openGenericType, Type[] closedGenericArguments)
        {
            try
            {
                return openGenericType.MakeGenericType(closedGenericArguments);
            }
            catch (Exception)
            {
                return null;
            }
        }
#if NET452 || NET46 || NETCOREAPP2_0

        /// <summary>
        /// Gets the method represented by the delegate.
        /// </summary>
        /// <param name="del">The target <see cref="Delegate"/>.</param>
        /// <returns>The method represented by the delegate.</returns>
        public static MethodInfo GetMethodInfo(this Delegate del) => del.Method;

        /// <summary>
        /// Gets the custom attributes for this <paramref name="assembly"/>.
        /// </summary>
        /// <param name="assembly">The target <see cref="Assembly"/>.</param>
        /// <param name="attributeType">The type of <see cref="Attribute"/> objects to return.</param>
        /// <returns>The custom attributes for this <paramref name="assembly"/>.</returns>
        public static IEnumerable<Attribute> GetCustomAttributes(this Assembly assembly, Type attributeType)
            => assembly.GetCustomAttributes(attributeType, false).Cast<Attribute>();
#endif

        /// <summary>
        /// Gets a value indicating whether the <see cref="Type"/> is an <see cref="IEnumerable{T}"/> type.
        /// </summary>
        /// <param name="type">The target <see cref="Type"/>.</param>
        /// <returns>true if the <see cref="Type"/> is an <see cref="IEnumerable{T}"/>; otherwise, false.</returns>
        public static bool IsEnumerableOfT(this Type type)
        {
            var typeInfo = type.GetTypeInfo();
            return typeInfo.IsGenericType && typeInfo.GetGenericTypeDefinition() == typeof(IEnumerable<>);
        }

        /// <summary>
        /// Gets a value indicating whether the <see cref="Type"/> is an <see cref="IList{T}"/> type.
        /// </summary>
        /// <param name="type">The target <see cref="Type"/>.</param>
        /// <returns>true if the <see cref="Type"/> is an <see cref="IList{T}"/>; otherwise, false.</returns>
        public static bool IsListOfT(this Type type)
        {
            var typeInfo = type.GetTypeInfo();
            return typeInfo.IsGenericType && typeInfo.GetGenericTypeDefinition() == typeof(IList<>);
        }

        /// <summary>
        /// Gets a value indicating whether the <see cref="Type"/> is an <see cref="ICollection{T}"/> type.
        /// </summary>
        /// <param name="type">The target <see cref="Type"/>.</param>
        /// <returns>true if the <see cref="Type"/> is an <see cref="ICollection{T}"/>; otherwise, false.</returns>
        public static bool IsCollectionOfT(this Type type)
        {
            var typeInfo = type.GetTypeInfo();
            return typeInfo.IsGenericType && typeInfo.GetGenericTypeDefinition() == typeof(ICollection<>);
        }

        /// <summary>
        /// Gets a value indicating whether the <see cref="Type"/> is an <see cref="IReadOnlyCollection{T}"/> type.
        /// </summary>
        /// <param name="type">The target <see cref="Type"/>.</param>
        /// <returns>true if the <see cref="Type"/> is an <see cref="IReadOnlyCollection{T}"/>; otherwise, false.</returns>
        public static bool IsReadOnlyCollectionOfT(this Type type)
        {
            var typeInfo = type.GetTypeInfo();
            return typeInfo.IsGenericType && typeInfo.GetGenericTypeDefinition() == typeof(IReadOnlyCollection<>);
        }

        /// <summary>
        /// Gets a value indicating whether the <see cref="Type"/> is an <see cref="IReadOnlyList{T}"/> type.
        /// </summary>
        /// <param name="type">The target <see cref="Type"/>.</param>
        /// <returns>true if the <see cref="Type"/> is an <see cref="IReadOnlyList{T}"/>; otherwise, false.</returns>
        public static bool IsReadOnlyListOfT(this Type type)
        {
            var typeInfo = type.GetTypeInfo();
            return typeInfo.IsGenericType && typeInfo.GetGenericTypeDefinition() == typeof(IReadOnlyList<>);
        }

        /// <summary>
        /// Gets a value indicating whether the <see cref="Type"/> is an <see cref="Lazy{T}"/> type.
        /// </summary>
        /// <param name="type">The target <see cref="Type"/>.</param>
        /// <returns>true if the <see cref="Type"/> is an <see cref="Lazy{T}"/>; otherwise, false.</returns>
        public static bool IsLazy(this Type type)
        {
            var typeInfo = type.GetTypeInfo();
            return typeInfo.IsGenericType && typeInfo.GetGenericTypeDefinition() == typeof(Lazy<>);
        }

        /// <summary>
        /// Gets a value indicating whether the <see cref="Type"/> is an <see cref="Func{T1}"/> type.
        /// </summary>
        /// <param name="type">The target <see cref="Type"/>.</param>
        /// <returns>true if the <see cref="Type"/> is an <see cref="Func{T1}"/>; otherwise, false.</returns>
        public static bool IsFuncRepresentingService(this Type type)
        {
            var typeInfo = type.GetTypeInfo();
            return typeInfo.IsGenericType && typeInfo.GetGenericTypeDefinition() == typeof(Func<>);
        }

        /// <summary>
        /// Gets a value indicating whether the <see cref="Type"/> is an <see cref="Func{T1, TResult}"/>,
        /// <see cref="Func{T1,T2,TResult}"/>, <see cref="Func{T1,T2,T3, TResult}"/> or an <see cref="Func{T1,T2,T3,T4 ,TResult}"/>.
        /// </summary>
        /// <param name="type">The target <see cref="Type"/>.</param>
        /// <returns>true if the <see cref="Type"/> is an <see cref="Func{T1, TResult}"/>, <see cref="Func{T1,T2,TResult}"/>, <see cref="Func{T1,T2,T3, TResult}"/> or an <see cref="Func{T1,T2,T3,T4 ,TResult}"/>; otherwise, false.</returns>
        public static bool IsFuncWithParameters(this Type type)
        {
            var typeInfo = type.GetTypeInfo();
            if (!typeInfo.IsGenericType)
            {
                return false;
            }

            if (type.IsFuncRepresentingNamedService())
            {
                return false;
            }

            Type genericTypeDefinition = typeInfo.GetGenericTypeDefinition();

            return genericTypeDefinition == typeof(Func<,>) || genericTypeDefinition == typeof(Func<,,>)
                   || genericTypeDefinition == typeof(Func<,,,>) || genericTypeDefinition == typeof(Func<,,,,>);
        }

        public static bool IsFuncRepresentingNamedService(this Type type)
        {
            var typeInfo = type.GetTypeInfo();
            if (!typeInfo.IsGenericType)
            {
                return false;
            }

            Type genericTypeDefinition = typeInfo.GetGenericTypeDefinition();

            return genericTypeDefinition == typeof(Func<,>) && typeInfo.GenericTypeArguments[0] == typeof(string);
        }

        /// <summary>
        /// Gets a value indicating whether the <see cref="Type"/> is a closed generic type.
        /// </summary>
        /// <param name="type">The target <see cref="Type"/>.</param>
        /// <returns>true if the <see cref="Type"/> is a closed generic type; otherwise, false.</returns>
        public static bool IsClosedGeneric(this Type type)
        {
            var typeInfo = type.GetTypeInfo();
            return typeInfo.IsGenericType && !typeInfo.IsGenericTypeDefinition;
        }

        /// <summary>
        /// Returns the <see cref="Type"/> of the object encompassed or referred to by the current array, pointer or reference type.
        /// </summary>
        /// <param name="type">The target <see cref="Type"/>.</param>
        /// <returns>The <see cref="Type"/> of the object encompassed or referred to by the current array, pointer, or reference type,
        /// or null if the current Type is not an array or a pointer, or is not passed by reference,
        /// or represents a generic type or a type parameter in the definition of a generic type or generic method.</returns>
        public static Type GetElementType(Type type)
        {
            var typeInfo = type.GetTypeInfo();
            var genericTypeArguments = typeInfo.GenericTypeArguments;
            if (typeInfo.IsGenericType && genericTypeArguments.Length == 1)
            {
                return genericTypeArguments[0];
            }

            return type.GetElementType();
        }

#if NETSTANDARD1_1 || NETSTANDARD1_3 || NETSTANDARD1_6 || NETSTANDARD2_0
        public static object GetDefaultValue(Type type)
        {
            var openGenericGetDefaultValueInternalMethod = typeof(TypeHelper).GetTypeInfo().GetDeclaredMethod(nameof(GetDefaultValueInternal));
            var closedGenerictDefaultValueInternalMethod = openGenericGetDefaultValueInternalMethod.MakeGenericMethod(type);
            return closedGenerictDefaultValueInternalMethod.Invoke(null, new object[] { });
        }

        private static T GetDefaultValueInternal<T>()
        {
            return default(T);
        }
#endif
    }

    internal static class EnumerableTypeExtensions
    {
        private static readonly ThreadSafeDictionary<Type, Type> EnumerableTypes = new ThreadSafeDictionary<Type, Type>();

        public static Type GetEnumerableType(this Type returnType) =>
            EnumerableTypes.GetOrAdd(returnType, CreateEnumerableType);

        private static Type CreateEnumerableType(Type type) =>
            typeof(IEnumerable<>).MakeGenericType(type);
    }
}
