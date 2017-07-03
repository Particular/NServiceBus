/*
The MIT License (MIT)

Copyright (c) 2013-2017 Maksim Volkau

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in
all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
THE SOFTWARE.
*/

namespace DryIoc
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Reflection;
    using System.Text;
    using System.Threading;
    using System.Diagnostics.CodeAnalysis;  // for SupressMessage
    using System.Diagnostics;               // for StackTrace
    using ImTools;

    /// <summary>IoC Container. Documentation is available at https://bitbucket.org/dadhi/dryioc. </summary>
    [Janitor.SkipWeaving]
    internal sealed partial class Container : IContainer, IScopeAccess
    {
        /// <summary>Creates new container with default rules <see cref="DryIoc.Rules.Default"/>.</summary>
        public Container() : this(Rules.Default, Ref.Of(Registry.Default), new SingletonScope())
        { }

        /// <summary>Creates new container, optionally providing <see cref="Rules"/> to modify default container behavior.</summary>
        /// <param name="rules">(optional) Rules to modify container default resolution behavior.
        /// If not specified, then <see cref="DryIoc.Rules.Default"/> will be used.</param>
        /// <param name="scopeContext">(optional) Scope context to use for <see cref="Reuse.InCurrentScope"/>, default is <see cref="ThreadScopeContext"/>.</param>
        public Container(Rules rules = null, IScopeContext scopeContext = null)
            : this(rules ?? Rules.Default, Ref.Of(Registry.Default), new SingletonScope(), scopeContext)
        { }

        /// <summary>Creates new container with configured rules.</summary>
        /// <param name="configure">Delegate gets <see cref="DryIoc.Rules.Default"/> as input and may return configured rules.</param>
        /// <param name="scopeContext">(optional) Scope context to use for <see cref="Reuse.InCurrentScope"/>, default is <see cref="ThreadScopeContext"/>.</param>
        public Container(Func<Rules, Rules> configure, IScopeContext scopeContext = null)
            : this(configure.ThrowIfNull()(Rules.Default) ?? Rules.Default, scopeContext)
        { }

        /// <summary>Outputs scope info for open scope.</summary> <returns>Info about scoped container</returns>
        public override string ToString()
        {
            var scope = ((IScopeAccess)this).GetCurrentScope();
            var scopeStr
                = scope == null ? "container"
                    : _scopeContext != null
                        ? "ambient scoped container with scope " + scope
                        : "scoped container with scope " + scope;
            if (IsDisposed)
            {
                scopeStr = "disposed " + scopeStr + Environment.NewLine;
                if (_disposeStackTrace != null)
                    scopeStr += "Dispose stack-trace " + _disposeStackTrace;
                else
                    scopeStr += "You may include Dispose stack-trace into the message via:" + Environment.NewLine
                                + "container.With(rules => rules.WithCaptureContainerDisposeStackTrace())";
            }
            return scopeStr;
        }

        /// <summary>Shares all of container state except Cache and specifies new rules.</summary>
        /// <param name="configure">(optional) Configure rules, if not specified then uses Rules from current container.</param>
        /// <param name="scopeContext">(optional) New scope context, if not specified then uses context from current container.</param>
        /// <returns>New container.</returns>
        public IContainer With(Func<Rules, Rules> configure = null, IScopeContext scopeContext = null)
        {
            ThrowIfContainerDisposed();
            var rules = configure == null ? Rules : configure(Rules);
            scopeContext = scopeContext ?? _scopeContext;
            var registryWithoutCache = Ref.Of(_registry.Value.WithoutCache());
            return new Container(rules, registryWithoutCache,
                _singletonScope, scopeContext, _openedScope,
                _disposed, _disposeStackTrace, _resolverContext._rootContainer);
        }

        /// <summary>Produces new container which prevents any further registrations.</summary>
        /// <param name="ignoreInsteadOfThrow">(optional) Controls what to do with the next registration: ignore or throw exception.
        /// Throws exception by default.</param>
        /// <returns>New container preserving all current container state but disallowing registrations.</returns>
        public IContainer WithNoMoreRegistrationAllowed(bool ignoreInsteadOfThrow = false)
        {
            var readonlyRegistry = Ref.Of(_registry.Value.WithNoMoreRegistrationAllowed(ignoreInsteadOfThrow));
            return new Container(Rules, readonlyRegistry,
                _singletonScope, _scopeContext, _openedScope,
                _disposed, _disposeStackTrace, _resolverContext._rootContainer);
        }

        /// <summary>Returns new container with all expression, delegate, items cache removed/reset.
        /// It will preserve resolved services in Singleton/Current scope.</summary>
        /// <returns>New container with empty cache.</returns>
        public IContainer WithoutCache()
        {
            ThrowIfContainerDisposed();
            var registryWithoutCache = Ref.Of(_registry.Value.WithoutCache());
            return new Container(Rules, registryWithoutCache,
                _singletonScope, _scopeContext, _openedScope,
                _disposed, _disposeStackTrace, _resolverContext._rootContainer);
        }

        /// <summary>Creates new container with state shared with original except singletons and cache.
        /// Dropping cache is required because singletons are cached in resolution state.</summary>
        /// <returns>New container with empty Singleton Scope.</returns>
        public IContainer WithoutSingletonsAndCache()
        {
            ThrowIfContainerDisposed();
            var registryWithoutCache = Ref.Of(_registry.Value.WithoutCache());
            var newSingletons = new SingletonScope();
            return new Container(Rules, registryWithoutCache,
                newSingletons, _scopeContext, _openedScope,
                _disposed, _disposeStackTrace, _resolverContext._rootContainer);
        }

        /// <summary>Shares all parts with original container But copies registration, so the new registration
        /// won't be visible in original. Registrations include decorators and wrappers as well.</summary>
        /// <param name="preserveCache">(optional) If set preserves cache if you know what to do.</param>
        /// <returns>New container with copy of all registrations.</returns>
        public IContainer WithRegistrationsCopy(bool preserveCache = false)
        {
            ThrowIfContainerDisposed();
            var newRegistry = preserveCache ? _registry.NewRef() : Ref.Of(_registry.Value.WithoutCache());
            return new Container(Rules, newRegistry,
                _singletonScope, _scopeContext, _openedScope,
                _disposed, _disposeStackTrace, _resolverContext._rootContainer);
        }

        /// <summary>Returns ambient scope context associated with container.</summary>
        public IScopeContext ScopeContext { get { return _scopeContext; } }

        /// <summary>Creates new container with new opened scope, with shared registrations, singletons and resolutions cache.
        /// If container uses ambient scope context, then this method sets new opened scope as current scope in the context.
        /// In case of previous open scope, new open scope references old one as a parent.
        /// </summary>
        /// <param name="name">(optional) Name for opened scope to allow reuse to identify the scope.</param>
        /// <param name="configure">(optional) Configure rules, if not specified then uses Rules from current container.</param>
        /// <returns>New container with different current scope.</returns>
        /// <example><code lang="cs"><![CDATA[
        /// using (var scoped = container.OpenScope())
        /// {
        ///     var handler = scoped.Resolve<IHandler>();
        ///     handler.Handle(data);
        /// }
        /// ]]></code></example>
        public IContainer OpenScope(object name = null, Func<Rules, Rules> configure = null)
        {
            ThrowIfContainerDisposed();

            if (name == null)
                name = _openedScope != null ? null
                    : _scopeContext != null ? _scopeContext.RootScopeName
                        : NonAmbientRootScopeName;

            var nestedOpenedScope = new Scope(_openedScope, name);

            // Replacing current context scope with new nested only if current is the same as nested parent, otherwise throw.
            if (_scopeContext != null)
                _scopeContext.SetCurrent(scope =>
                    nestedOpenedScope.ThrowIf(scope != _openedScope, Error.NotDirectScopeParent, _openedScope, scope));

            var rules = configure == null ? Rules : configure(Rules);

            return new Container(rules, _registry,
                _singletonScope, _scopeContext, nestedOpenedScope,
                _disposed, _disposeStackTrace, _resolverContext._rootContainer ?? this);
        }

        // todo: v3: Review do we need this name at all, probably not and it is decreasing performance.
        /// <summary>The default name of root scope without ambient context.</summary>
        public static readonly object NonAmbientRootScopeName = "NonAmbientRootScope";

        /// <summary>Creates container (facade) that fallbacks to this container for unresolved services.
        /// Facade is the new empty container, with the same rules and scope context as current container.
        /// It could be used for instance to create Test facade over original container with replacing some services with test ones.</summary>
        /// <remarks>Singletons from container are not reused by facade - 
        /// when you resolve singleton directly from parent and then ask for it from child, it will return another object.
        /// To achieve that you may use <see cref="IContainer.OpenScope"/> with <see cref="Reuse.InCurrentScope"/>.</remarks>
        /// <returns>New facade container.</returns>
        public IContainer CreateFacade()
        {
            ThrowIfContainerDisposed();
            return new Container(Rules.WithFallbackContainer(this), _scopeContext);
        }

        /// <summary>Clears cache for specified service(s). 
        /// But does not clear instances of already resolved/created singletons and scoped services!</summary>
        /// <param name="serviceType">Target service type.</param>
        /// <param name="factoryType">(optional) If not specified, clears cache for all <see cref="FactoryType"/>.</param>
        /// <param name="serviceKey">(optional) If omitted, the cache will be cleared for all registrations of <paramref name="serviceType"/>.</param>
        /// <returns>True if target service was found, false - otherwise.</returns>
        public bool ClearCache(Type serviceType, FactoryType? factoryType = null, object serviceKey = null)
        {
            if (factoryType != null)
                return _registry.Value.ClearCache(serviceType, serviceKey, factoryType.Value);

            var registry = _registry.Value;

            var clearedServices = registry.ClearCache(serviceType, serviceKey, FactoryType.Service);
            var clearedWrapper = registry.ClearCache(serviceType, serviceKey, FactoryType.Wrapper);
            var clearedDecorator = registry.ClearCache(serviceType, serviceKey, FactoryType.Decorator);

            return clearedServices || clearedWrapper || clearedDecorator;
        }

        /// <summary>Dispose either open scope, or container with singletons, if no scope opened.</summary>
        public void Dispose()
        {
            // todo: Add disposed stack info an display in ThrowIfContainerDisposed exception
            if (Interlocked.CompareExchange(ref _disposed, 1, 0) != 0)
                return;

            // nice to have, but we can leave without it if something goes wrong
            if (Rules.CaptureContainerDisposeStackTrace)
                try { _disposeStackTrace = new StackTrace(); } catch { }

            // for container created with OpenScope, 
            // But not WithImplicitOpenedRootScope rules
            var isScopedContainer = _resolverContext._rootContainer != null;

            // remove reference to context container(s) to prevent memory leak
            _resolverContext = null;

            if (isScopedContainer)
            {
                if (_scopeContext != null)
                    _scopeContext.SetCurrent(scope =>
                    {
                        if (scope == _openedScope)
                            return scope.Parent;
                        return scope;
                    });
            }
            else // whole Container with singletons.
            {
                _defaultFactoryDelegateCache = Ref.Of(FactoryDelegateCache.Empty);
                _registry.Swap(Registry.Empty);
                Rules = Rules.Default;

                _singletonScope.Dispose();

                if (_scopeContext != null)
                    _scopeContext.Dispose();
            }

            if (_openedScope != null)
                _openedScope.Dispose();
        }

        /// <summary>Scope containing container singletons.</summary>
        public IScope SingletonScope { get { return _singletonScope; } }

        #region Static state

        // todo: v3: remove
        /// <summary>State parameter expression in FactoryDelegate.</summary>
        public static readonly ParameterExpression StateParamExpr =
            Expression.Parameter(typeof(object[]), "state");

        /// <summary>Resolver context parameter expression in FactoryDelegate.</summary>
        public static readonly ParameterExpression ResolverContextParamExpr =
            Expression.Parameter(typeof(IResolverContext), "r");

        /// <summary>Resolver parameter expression in FactoryDelegate.</summary>
        public static readonly Expression ResolverExpr =
            Expression.Property(ResolverContextParamExpr, "Resolver");

        /// <summary>Resolver parameter expression in FactoryDelegate.</summary>
        public static readonly Expression RootResolverExpr =
            Expression.Call(typeof(ResolverContext), "RootResolver", ArrayTools.Empty<Type>(), ResolverContextParamExpr);

        /// <summary>Returns <see cref="ResolverExpr"/> or <see cref="RootResolverExpr"/> based on request.</summary>
        public static Expression GetResolverExpr(Request request)
        {
            return request.IsSingletonOrDependencyOfSingleton ? RootResolverExpr : ResolverExpr;
        }

        /// <summary>Resolver parameter expression in FactoryDelegate.</summary>
        public static readonly Expression SingletonScopeExpr =
            Expression.Call(typeof(ResolverContext), "SingletonScope", ArrayTools.Empty<Type>(), ResolverContextParamExpr);

        /// <summary>Access to scopes in FactoryDelegate.</summary>
        public static readonly Expression ScopesExpr =
            Expression.Property(ResolverContextParamExpr, "Scopes");

        /// <summary>Resolver parameter expression in FactoryDelegate.</summary>
        public static readonly Expression RootScopesExpr =
            Expression.Call(typeof(ResolverContext), "RootScopes", ArrayTools.Empty<Type>(), ResolverContextParamExpr);

        /// <summary>Returns <see cref="ScopesExpr"/> or <see cref="RootResolverExpr"/> based on request.</summary>
        public static Expression GetScopesExpr(Request request)
        {
            return request.IsSingletonOrDependencyOfSingleton ? RootScopesExpr : ScopesExpr;
        }

        /// <summary>Resolution scope parameter expression in FactoryDelegate.</summary>
        public static readonly ParameterExpression ResolutionScopeParamExpr =
            Expression.Parameter(typeof(IScope), "scope");

        internal static Expression GetResolutionScopeExpression(Request request)
        {
            if (request.Scope != null)
                return ResolutionScopeParamExpr;

            // the only situation when we could be here: the root resolution call
            // and scope was not created on the boundary of Resolve call.
            var parent = request.Enumerate().Last();

            var container = request.Container;
            var registeredServiceType = container.GetWrappedType(parent.ServiceType, parent.RequiredServiceType);
            var parentServiceTypeExpr = Expression.Constant(registeredServiceType, typeof(Type));
            var parentServiceKeyExpr = container.GetOrAddStateItemExpression(parent.ServiceKey, typeof(object));

            // if assign in expression is supported then use it.
            var scopesExpr = GetScopesExpr(request);
            if (_expressionAssignMethod != null)
            {
                var getOrNewScopeExpr = Expression.Call(scopesExpr, "GetOrNewResolutionScope",
                    ArrayTools.Empty<Type>(), ResolutionScopeParamExpr, parentServiceTypeExpr, parentServiceKeyExpr);
                var parameters = new object[] { ResolutionScopeParamExpr, getOrNewScopeExpr };
                return (Expression)_expressionAssignMethod.Value.Invoke(null, parameters);
            }

            return Expression.Call(scopesExpr, "GetOrCreateResolutionScope",
                ArrayTools.Empty<Type>(), ResolutionScopeParamExpr, parentServiceTypeExpr, parentServiceKeyExpr);
        }

        private static readonly Lazy<MethodInfo> _expressionAssignMethod = new Lazy<MethodInfo>(() =>
            typeof(Expression).GetMethodOrNull("Assign", typeof(Expression), typeof(Expression)));

        private static readonly Type[] _factoryDelegateParamTypes =
            { typeof(object[]), typeof(IResolverContext), typeof(IScope) };

        private static readonly ParameterExpression[] _factoryDelegateParamExprs =
            { StateParamExpr, ResolverContextParamExpr, ResolutionScopeParamExpr };

        /// <summary>Wraps service creation expression (body) into <see cref="FactoryDelegate"/> and returns result lambda expression.</summary>
        /// <param name="expression">Service expression (body) to wrap.</param> <returns>Created lambda expression.</returns>
        public static Expression<FactoryDelegate> WrapInFactoryExpression(Expression expression)
        {
            return Expression.Lambda<FactoryDelegate>(OptimizeExpression(expression), _factoryDelegateParamExprs);
        }

        /// <summary>First wraps the input service expression into lambda expression and
        /// then compiles lambda expression to actual <see cref="FactoryDelegate"/> used for service resolution.</summary>
        /// <param name="expression">Service creation expression.</param>
        /// <returns>Compiled factory delegate to use for service resolution.</returns>
        public static FactoryDelegate CompileToDelegate(Expression expression)
        {
            expression = OptimizeExpression(expression);

            // Optimize: just extract singleton from expression without compiling
            if (expression.NodeType == ExpressionType.Constant)
            {
                var value = ((ConstantExpression)expression).Value;
                return (state, context, scope) => value;
            }

            var factoryDelegate = FastExpressionCompiler.ExpressionCompiler.DoCompile<FactoryDelegate>(
                expression, _factoryDelegateParamExprs, _factoryDelegateParamTypes, typeof(object));
            if (factoryDelegate != null)
                return factoryDelegate;

            return Expression.Lambda<FactoryDelegate>(expression, _factoryDelegateParamExprs).Compile();
        }

        /// <summary>Strips the unnecessary or adds the necessary cast to expression return result.</summary>
        /// <param name="expression">Expression to process.</param> <returns>Processed expression.</returns>
        public static Expression OptimizeExpression(Expression expression)
        {
            if (expression.NodeType == ExpressionType.Convert)
                expression = ((UnaryExpression)expression).Operand;
            else if (expression.Type.IsValueType())
                expression = Expression.Convert(expression, typeof(object));
            return expression;
        }

        #endregion

        #region IRegistrator

        /// <summary>Returns all registered service factories with their Type and optional Key.</summary>
        /// <returns>Existing registrations.</returns>
        /// <remarks>Decorator and Wrapper types are not included.</remarks>
        public IEnumerable<ServiceRegistrationInfo> GetServiceRegistrations()
        {
            return _registry.Value.GetServiceRegistrations();
        }

        /// <summary>Stores factory into container using <paramref name="serviceType"/> and <paramref name="serviceKey"/> as key
        /// for later lookup.</summary>
        /// <param name="factory">Any subtypes of <see cref="Factory"/>.</param>
        /// <param name="serviceType">Type of service to resolve later.</param>
        /// <param name="serviceKey">(optional) Service key of any type with <see cref="object.GetHashCode"/> and <see cref="object.Equals(object)"/>
        /// implemented.</param>
        /// <param name="ifAlreadyRegistered">(optional) Says how to handle existing registration with the same
        /// <paramref name="serviceType"/> and <paramref name="serviceKey"/>.</param>
        /// <param name="isStaticallyChecked">Confirms that service and implementation types are statically checked by compiler.</param>
        /// <returns>True if factory was added to registry, false otherwise.
        /// False may be in case of <see cref="IfAlreadyRegistered.Keep"/> setting and already existing factory.</returns>
        public void Register(Factory factory, Type serviceType, object serviceKey, IfAlreadyRegistered ifAlreadyRegistered, bool isStaticallyChecked)
        {
            ThrowIfContainerDisposed();
            factory.ThrowIfNull().ThrowIfInvalidRegistration(serviceType, serviceKey, isStaticallyChecked, Rules);

            if (ifAlreadyRegistered == IfAlreadyRegistered.AppendNotKeyed)
                ifAlreadyRegistered = Rules.DefaultIfAlreadyRegistered;

            // Improves performance a bit by attempt to swapping registry while it is still unchanged,
            // if attempt fails then fallback to normal Swap with retry.
            var registry = _registry.Value;
            if (!_registry.TrySwapIfStillCurrent(registry, registry.Register(factory, serviceType, ifAlreadyRegistered, serviceKey)))
                _registry.Swap(r => r.Register(factory, serviceType, ifAlreadyRegistered, serviceKey));

            _defaultFactoryDelegateCache = _registry.Value.DefaultFactoryDelegateCache;
        }

        /// <summary>Returns true if there is registered factory with the service type and key.
        /// To check if only default factory is registered specify <see cref="DefaultKey.Value"/> as <paramref name="serviceKey"/>.
        /// Otherwise, if no <paramref name="serviceKey"/> specified then True will returned for any registered factories, even keyed.
        /// Additionally you can specify <paramref name="condition"/> to be applied to registered factories.</summary>
        /// <param name="serviceType">Service type to look for.</param>
        /// <param name="serviceKey">Service key to look for.</param>
        /// <param name="factoryType">Expected registered factory type.</param>
        /// <param name="condition">Expected factory condition.</param>
        /// <returns>True if factory is registered, false if not.</returns>
        public bool IsRegistered(Type serviceType, object serviceKey, FactoryType factoryType, Func<Factory, bool> condition)
        {
            ThrowIfContainerDisposed();
            var factories = _registry.Value.GetRegisteredFactories(serviceType.ThrowIfNull(), serviceKey, factoryType, condition);
            return !factories.IsNullOrEmpty();
        }

        /// <summary>Removes specified factory from registry.
        /// Factory is removed only from registry, if there is relevant cache, it will be kept.
        /// Use <see cref="WithoutCache"/> to remove all the cache.</summary>
        /// <param name="serviceType">Service type to look for.</param>
        /// <param name="serviceKey">Service key to look for.</param>
        /// <param name="factoryType">Expected factory type.</param>
        /// <param name="condition">Expected factory condition.</param>
        public void Unregister(Type serviceType, object serviceKey, FactoryType factoryType, Func<Factory, bool> condition)
        {
            ThrowIfContainerDisposed();
            _registry.Swap(r => r.Unregister(factoryType, serviceType, serviceKey, condition));
            _defaultFactoryDelegateCache = _registry.Value.DefaultFactoryDelegateCache;
        }

        #endregion

        #region IResolver

        object IResolver.Resolve(Type serviceType, bool ifUnresolvedReturnDefault)
        {
            var factoryDelegate = _defaultFactoryDelegateCache.Value.GetValueOrDefault(serviceType);
            if (factoryDelegate != null)
                return factoryDelegate(null, _resolverContext, null);
            return ResolveAndCacheDefaultDelegate(serviceType, ifUnresolvedReturnDefault);
        }

        object IResolver.Resolve(Type serviceType, object serviceKey, bool ifUnresolvedReturnDefault, Type requiredServiceType, RequestInfo preResolveParent,
            IScope scope)
        {
            preResolveParent = preResolveParent ?? RequestInfo.Empty;

            var cacheEntryKey = serviceKey == null
                ? (object)serviceType
                : new KV<object, object>(serviceType, serviceKey);

            object cacheContextKey = requiredServiceType;
            if (!preResolveParent.IsEmpty)
                cacheContextKey = cacheContextKey == null
                    ? (object)preResolveParent
                    : KV.Of(cacheContextKey, preResolveParent);

            // Check cache first:
            var registry = _registry.Value;
            var cacheRef = registry.KeyedFactoryDelegateCache;
            var cacheEntry = cacheRef.Value.GetValueOrDefault(cacheEntryKey);
            if (cacheEntry != null)
            {
                var cachedFactoryDelegate = cacheContextKey == null
                    ? cacheEntry.Key
                    : (cacheEntry.Value ?? ImTreeMap<object, FactoryDelegate>.Empty).GetValueOrDefault(cacheContextKey);

                if (cachedFactoryDelegate != null)
                    return cachedFactoryDelegate(null, _resolverContext, scope);
            }

            // Cache is missed, so get the factory and put it into cache:
            ThrowIfContainerDisposed();
            var ifUnresolved = ifUnresolvedReturnDefault ? IfUnresolved.ReturnDefault : IfUnresolved.Throw;
            var request = Request.Create(this, serviceType, serviceKey, ifUnresolved, requiredServiceType, scope,
                preResolveParent);

            var factory = ((IContainer)this).ResolveFactory(request);

            // Hack: may mutate (set) not null request service key.
            if (serviceKey == null && request.ServiceKey != null)
                cacheEntryKey = new KV<object, object>(serviceType, request.ServiceKey);

            if (factory == null)
                return null;

            var factoryDelegate = factory.GetDelegateOrDefault(request);
            if (factoryDelegate == null)
                return null;

            var service = factoryDelegate(null, _resolverContext, scope);

            if (registry.Services.IsEmpty)
                return service;

            // Cache factory only when we successfully called the factory delegate, to prevent failing delegates to be cached.
            // Additionally disable caching when:
            // no services registered, so the service probably empty collection wrapper or alike.
            var cachedContextFactories =
                (cacheEntry == null ? null : cacheEntry.Value) ??
                ImTreeMap<object, FactoryDelegate>.Empty;

            if (cacheContextKey == null)
                cacheEntry = KV.Of(factoryDelegate, cachedContextFactories);
            else
                cacheEntry = KV.Of(cacheEntry == null ? null : cacheEntry.Key,
                    cachedContextFactories.AddOrUpdate(cacheContextKey, factoryDelegate));

            var cacheVal = cacheRef.Value;
            if (!cacheRef.TrySwapIfStillCurrent(cacheVal, cacheVal.AddOrUpdate(cacheEntryKey, cacheEntry)))
                cacheRef.Swap(_ => _.AddOrUpdate(cacheEntryKey, cacheEntry));

            return service;
        }

        private object ResolveAndCacheDefaultDelegate(Type serviceType, bool ifUnresolvedReturnDefault)
        {
            ThrowIfContainerDisposed();

            var ifUnresolved = ifUnresolvedReturnDefault ? IfUnresolved.ReturnDefault : IfUnresolved.Throw;
            var request = Request.Create(this, serviceType, ifUnresolved: ifUnresolved);
            var factory = ((IContainer)this).ResolveFactory(request); // HACK: may mutate request, but it should be safe

            // The situation is possible for multiple default services registered.
            if (request.ServiceKey != null)
                return ((IResolver)this).Resolve(serviceType, request.ServiceKey, ifUnresolvedReturnDefault, null, null, null);

            var factoryDelegate = factory == null ? null : factory.GetDelegateOrDefault(request);
            if (factoryDelegate == null)
                return null;

            var registryValue = _registry.Value;
            var service = factoryDelegate(null, _resolverContext, null);

            // Additionally disable caching when:
            // no services registered, so the service probably empty collection wrapper or alike.
            if (!registryValue.Services.IsEmpty)
            {
                var cacheRef = registryValue.DefaultFactoryDelegateCache;
                var cacheVal = cacheRef.Value;
                if (!cacheRef.TrySwapIfStillCurrent(cacheVal, cacheVal.AddOrUpdate(serviceType, factoryDelegate)))
                    cacheRef.Swap(_ => _.AddOrUpdate(serviceType, factoryDelegate));
            }

            return service;
        }

        // todo: v3: remove unused composite key and required type parameters
        // todo: perf: optimize for speed
        IEnumerable<object> IResolver.ResolveMany(
            Type serviceType, object serviceKey, Type requiredServiceType,
            object compositeParentKey, Type compositeParentRequiredType,
            RequestInfo preResolveParent, IScope scope)
        {
            var requiredItemType = requiredServiceType ?? serviceType;

            // Emulating the collection parent so that collection related rules and conditions were applied
            // the same way as if resolving IEnumerable<T>
            if (preResolveParent == null || preResolveParent.IsEmpty)
                preResolveParent = RequestInfo.Empty.Push(
                    typeof(IEnumerable<object>), requiredItemType, serviceKey, IfUnresolved.Throw,
                    0, FactoryType.Wrapper, implementationType: null, reuse: null, flags: RequestFlags.IsServiceCollection);

            var container = (IContainer)this;
            var items = container.GetAllServiceFactories(requiredItemType);

            IEnumerable<ServiceRegistrationInfo> openGenericItems = null;
            if (requiredItemType.IsClosedGeneric())
            {
                var requiredItemOpenGenericType = requiredItemType.GetGenericDefinitionOrNull();
                openGenericItems = container.GetAllServiceFactories(requiredItemOpenGenericType)
                    .Select(x => new ServiceRegistrationInfo(x.Value, requiredItemOpenGenericType, x.Key));
            }

            // Append registered generic types with compatible variance,
            // e.g. for IHandler<in E> - IHandler<A> is compatible with IHandler<B> if B : A.
            IEnumerable<ServiceRegistrationInfo> variantGenericItems = null;
            if (requiredItemType.IsGeneric() && container.Rules.VariantGenericTypesInResolvedCollection)
                variantGenericItems = container.GetServiceRegistrations()
                    .Where(x => x.ServiceType.IsGeneric()
                                && x.ServiceType.GetGenericTypeDefinition() == requiredItemType.GetGenericTypeDefinition()
                                && x.ServiceType != requiredItemType
                                && x.ServiceType.IsAssignableTo(requiredItemType));

            if (serviceKey != null) // include only single item matching key.
            {
                items = items.Where(it => serviceKey.Equals(it.Key));
                if (openGenericItems != null)
                    openGenericItems = openGenericItems.Where(it => serviceKey.Equals(it.OptionalServiceKey));
                if (variantGenericItems != null)
                    variantGenericItems = variantGenericItems.Where(it => serviceKey.Equals(it.OptionalServiceKey));
            }

            var metadataKey = preResolveParent.MetadataKey;
            var metadata = preResolveParent.Metadata;
            if (metadataKey != null || metadata != null)
            {
                items = items.Where(it => it.Value.Setup.MatchesMetadata(metadataKey, metadata));
                if (openGenericItems != null)
                    openGenericItems = openGenericItems.Where(it => it.Factory.Setup.MatchesMetadata(metadataKey, metadata));
                if (variantGenericItems != null)
                    variantGenericItems = variantGenericItems.Where(it => it.Factory.Setup.MatchesMetadata(metadataKey, metadata));
            }

            // Exclude composite parent service from items, skip decorators
            var parent = preResolveParent;
            if (parent.FactoryType != FactoryType.Service)
                parent = parent.Enumerate().FirstOrDefault(p => p.FactoryType == FactoryType.Service) ?? RequestInfo.Empty;

            if (!parent.IsEmpty && parent.GetActualServiceType() == requiredItemType)
            {
                items = items.Where(x => x.Value.FactoryID != parent.FactoryID);

                if (openGenericItems != null)
                    openGenericItems = openGenericItems.Where(x =>
                        !x.Factory.FactoryGenerator.GeneratedFactories.Enumerate().Any(f =>
                            f.Value.FactoryID == parent.FactoryID &&
                            f.Key.Key == parent.ServiceType && f.Key.Value == parent.ServiceKey));

                if (variantGenericItems != null)
                    variantGenericItems = variantGenericItems.Where(x => x.Factory.FactoryID != parent.FactoryID);
            }

            foreach (var item in items)
            {
                var service = container.Resolve(serviceType, item.Key,
                    true, requiredServiceType, preResolveParent, scope);
                if (service != null) // skip unresolved items
                    yield return service;
            }

            if (openGenericItems != null)
                foreach (var item in openGenericItems)
                {
                    var serviceKeyWithOpenGenericRequiredType = new[] { item.ServiceType, item.OptionalServiceKey };
                    var service = container.Resolve(serviceType, serviceKeyWithOpenGenericRequiredType,
                        true, requiredItemType, preResolveParent, scope);
                    if (service != null) // skip unresolved items
                        yield return service;
                }

            if (variantGenericItems != null)
                foreach (var item in variantGenericItems)
                {
                    var service = container.Resolve(serviceType, item.OptionalServiceKey,
                        true, item.ServiceType, preResolveParent, scope);
                    if (service != null) // skip unresolved items
                        yield return service;
                }

            var fallbackContainers = container.Rules.FallbackContainers;
            if (!fallbackContainers.IsNullOrEmpty())
            {
                for (var i = 0; i < fallbackContainers.Length; i++)
                {
                    var fallbackContainer = fallbackContainers[i];
                    var fallbackServices = fallbackContainer.Resolver.ResolveMany(serviceType,
                        serviceKey, requiredServiceType, compositeParentKey, compositeParentRequiredType,
                        preResolveParent, scope);
                    foreach (var fallbackService in fallbackServices)
                        yield return fallbackService;
                }
            }
        }

        private void ThrowIfContainerDisposed()
        {
            if (IsDisposed)
                Throw.It(Error.ContainerIsDisposed, this.ToString());
        }

        #endregion

        #region IScopeAccess

        IScope IScopeAccess.GetCurrentScope()
        {
            return ((IScopeAccess)this).GetCurrentNamedScope(null, false);
        }

        /// <summary>Gets current scope matching the <paramref name="name"/>.
        /// If name is null then current scope is returned, or if there is no current scope then exception thrown.</summary>
        /// <param name="name">May be null</param> <param name="throwIfNotFound">Says to throw if no scope found.</param>
        /// <returns>Found scope or throws exception.</returns>
        /// <exception cref="ContainerException"> with code <see cref="Error.NoMatchedScopeFound"/>.</exception>
        IScope IScopeAccess.GetCurrentNamedScope(object name, bool throwIfNotFound)
        {
            var currentScope = _scopeContext == null ? _openedScope : _scopeContext.GetCurrentOrDefault();
            return currentScope == null
                ? (throwIfNotFound ? Throw.For<IScope>(Error.NoCurrentScope) : null)
                : GetMatchingScopeOrDefault(currentScope, name)
                  ?? (throwIfNotFound ? Throw.For<IScope>(Error.NoMatchedScopeFound, currentScope, name) : null);
        }

        private static IScope GetMatchingScopeOrDefault(IScope scope, object name)
        {
            if (name != null)
                while (scope != null && !name.Equals(scope.Name))
                    scope = scope.Parent;
            return scope;
        }

        // note: The method required for .NET 3.5 which  does not have Expression.Assign, so the need for "ref" parameter (BTW "ref" is not supported in XAMARIN)
        /// <summary>Check if scope is not null, then just returns it, otherwise will create and return it.</summary>
        /// <param name="scope">May be null scope.</param>
        /// <param name="serviceType">Marking scope with resolved service type.</param>
        /// <param name="serviceKey">Marking scope with resolved service key.</param>
        /// <returns>Input <paramref name="scope"/> ensuring it is not null.</returns>
        IScope IScopeAccess.GetOrCreateResolutionScope(ref IScope scope, Type serviceType, object serviceKey)
        {
            return scope ?? (scope = new Scope(null, new KV<Type, object>(serviceType, serviceKey)));
        }

        /// <summary>Check if scope is not null, then just returns it, otherwise will create and return it.</summary>
        /// <param name="scope">May be null scope.</param>
        /// <param name="serviceType">Marking scope with resolved service type.</param>
        /// <param name="serviceKey">Marking scope with resolved service key.</param>
        /// <returns>Input <paramref name="scope"/> ensuring it is not null.</returns>
        public IScope GetOrNewResolutionScope(IScope scope, Type serviceType, object serviceKey)
        {
            return scope ?? new Scope(null, new KV<Type, object>(serviceType, serviceKey));
        }

        /// <summary>If both <paramref name="assignableFromServiceType"/> and <paramref name="serviceKey"/> are null,
        /// then returns input <paramref name="scope"/>.
        /// Otherwise searches scope hierarchy to find first scope with: Type assignable <paramref name="assignableFromServiceType"/> and
        /// Key equal to <paramref name="serviceKey"/>.</summary>
        /// <param name="scope">Scope to start matching with Type and Key specified.</param>
        /// <param name="assignableFromServiceType">Type to match.</param> <param name="serviceKey">Key to match.</param>
        /// <param name="outermost">If true - commands to look for outermost match instead of nearest.</param>
        /// <param name="throwIfNotFound">Says to throw if no scope found.</param>
        /// <returns>Matching scope or throws <see cref="ContainerException"/>.</returns>
        IScope IScopeAccess.GetMatchingResolutionScope(IScope scope, Type assignableFromServiceType, object serviceKey,
            bool outermost, bool throwIfNotFound)
        {
            return FindMatchingResolutionScope(scope, assignableFromServiceType, serviceKey, outermost)
                   ?? (!throwIfNotFound ? null
                       : Throw.For<IScope>(Error.NoMatchedScopeFound, scope, new KV<Type, object>(assignableFromServiceType, serviceKey)));
        }

        private static IScope FindMatchingResolutionScope(IScope scope, Type assignableFromServiceType, object serviceKey,
            bool outermost)
        {
            if (assignableFromServiceType == null && serviceKey == null)
                return scope;

            IScope matchedScope = null;
            while (scope != null)
            {
                var name = scope.Name as KV<Type, object>;
                if (name != null && (
                        assignableFromServiceType == null ||
                        name.Key.IsAssignableTo(assignableFromServiceType) ||
                        assignableFromServiceType.IsOpenGeneric() &&
                        name.Key.GetGenericDefinitionOrNull().IsAssignableTo(assignableFromServiceType)) &&
                    (serviceKey == null || serviceKey.Equals(name.Value)))
                {
                    matchedScope = scope;
                    if (!outermost) // break on first found match.
                        break;
                }
                scope = scope.Parent;
            }

            return matchedScope;
        }

        #endregion

        #region IContainer

        /// <summary>The rules object defines policies per container for registration and resolution.</summary>
        public Rules Rules { get; private set; }

        /// <summary>Indicates that container is disposed.</summary>
        public bool IsDisposed
        {
            get { return _disposed == 1 || _singletonScope.IsDisposed; }
        }

        // todo: v3: remove
        /// <summary>Obsolete: replaced with <see cref="Request.Create"/>/.</summary>
        Request IContainer.EmptyRequest
        {
            get { return Request.CreateEmpty(this); }
        }

        // todo: v3: Rename to ResolverContext
        /// <summary>Self weak reference, with readable message when container is GCed/Disposed.</summary>
        ContainerWeakRef IContainer.ContainerWeakRef
        {
            get { return _resolverContext; }
        }

        Factory IContainer.ResolveFactory(Request request)
        {
            var factory = GetServiceFactoryOrDefault(request, Rules.FactorySelector);
            if (factory == null)
            {
                factory = GetWrapperFactoryOrDefault(request);
                if (factory != null)
                    return factory;

                if (!Rules.FallbackContainers.IsNullOrEmpty())
                    factory = ResolveFromFallbackContainers(Rules.FallbackContainers, request);

                if (factory == null && !Rules.UnknownServiceResolvers.IsNullOrEmpty())
                    for (var i = 0; factory == null && i < Rules.UnknownServiceResolvers.Length; i++)
                        factory = Rules.UnknownServiceResolvers[i](request);
            }

            if (factory != null && factory.FactoryGenerator != null)
                factory = factory.FactoryGenerator.GetGeneratedFactory(request);

            if (factory == null && request.IfUnresolved == IfUnresolved.Throw)
                ThrowUnableToResolve(request);

            return factory;
        }

        private static Factory ResolveFromFallbackContainers(ContainerWeakRef[] fallbackContainers, Request request)
        {
            var container = request.Container;
            for (var i = 0; i < fallbackContainers.Length; i++)
            {
                var containerWeakRef = fallbackContainers[i];
                var containerRequest = request.WithNewContainer(containerWeakRef);

                // Continue to next parent if factory is not found in first parent by
                // updating IfUnresolved policy to ReturnDefault.
                if (containerRequest.IfUnresolved == IfUnresolved.Throw)
                    containerRequest = containerRequest.WithChangedServiceInfo(info => info.WithIfUnresolved(IfUnresolved.ReturnDefault));

                var factory = containerWeakRef.Container.ResolveFactory(containerRequest);
                if (factory != null)
                    return factory;
            }

            return null;
        }

        internal static void ThrowUnableToResolve(Request request)
        {
            var container = request.Container;

            var registrations = container
                .GetAllServiceFactories(request.ServiceType, bothClosedAndOpenGenerics: true)
                .Aggregate(new StringBuilder(), (s, f) =>
                    (f.Value.HasMatchingReuseScope(request)
                        ? s.Append("  ")
                        : s.Append("  without matching scope "))
                    .AppendLine(f.ToString()));

            if (registrations.Length != 0)
                Throw.It(Error.UnableToResolveFromRegisteredServices,
                    request, container, registrations);
            else
                Throw.It(Error.UnableToResolveUnknownService, request,
                    container.Rules.DynamicRegistrationProviders.EmptyIfNull().Length,
                    container.Rules.UnknownServiceResolvers.EmptyIfNull().Length);
        }

        Factory IContainer.GetServiceFactoryOrDefault(Request request)
        {
            return GetServiceFactoryOrDefault(request, Rules.FactorySelector);
        }

        IEnumerable<KV<object, Factory>> IContainer.GetAllServiceFactories(Type serviceType, bool bothClosedAndOpenGenerics)
        {
            var serviceFactories = _registry.Value.Services;

            var entry = serviceFactories.GetValueOrDefault(serviceType);

            var factories = GetRegistryEntryKeyFactoryPairs(entry).ToArrayOrSelf();

            if (bothClosedAndOpenGenerics && serviceType.IsClosedGeneric())
            {
                var openGenericEntry = serviceFactories.GetValueOrDefault(serviceType.GetGenericTypeDefinition());
                if (openGenericEntry != null)
                {
                    var openGenericFactories = GetRegistryEntryKeyFactoryPairs(openGenericEntry).ToArrayOrSelf();
                    factories = factories.Append(openGenericFactories);
                }
            }

            return GetCombinedRegisteredAndDynamicFactories(factories, FactoryType.Service, serviceType, null);
        }

        private static IEnumerable<KV<object, Factory>> GetRegistryEntryKeyFactoryPairs(object entry)
        {
            return entry == null ? Enumerable.Empty<KV<object, Factory>>()
                : entry is Factory ? new[] { new KV<object, Factory>(DefaultKey.Value, (Factory)entry) }
                    : ((FactoriesEntry)entry).Factories.Enumerate();
        }

        Expression IContainer.GetDecoratorExpressionOrDefault(Request request)
        {
            // return early if no decorators registered and no fallback containers to provide them
            if (_registry.Value.Decorators.IsEmpty &&
                request.Rules.DynamicRegistrationProviders.IsNullOrEmpty() &&
                request.Rules.FallbackContainers.IsNullOrEmpty())
                return null;

            var arrayElementType = request.ServiceType.GetArrayElementTypeOrNull();
            if (arrayElementType != null)
                request = request.WithChangedServiceInfo(info => info
                    .With(typeof(IEnumerable<>).MakeGenericType(arrayElementType)));

            // Define the list of ids for the already applied decorators
            int[] appliedDecoratorIDs = null;

            var container = request.Container;

            var serviceType = request.ServiceType;
            var decorators = container.GetDecoratorFactoriesOrDefault(serviceType);

            // Combine with required service type if different from service type
            var requiredServiceType = request.GetActualServiceType();
            if (requiredServiceType != serviceType)
                decorators = decorators.Append(container.GetDecoratorFactoriesOrDefault(requiredServiceType));

            if (!decorators.IsNullOrEmpty())
            {
                appliedDecoratorIDs = GetAppliedDecoratorIDs(request);
                if (!appliedDecoratorIDs.IsNullOrEmpty())
                    decorators = decorators.Match(d => appliedDecoratorIDs.IndexOf(d.FactoryID) == -1);
            }

            // Append open-generic decorators
            var genericDecorators = ArrayTools.Empty<Factory>();
            var openGenServiceType = serviceType.GetGenericDefinitionOrNull();
            if (openGenServiceType != null)
                genericDecorators = container.GetDecoratorFactoriesOrDefault(openGenServiceType);

            // Combine with open-generic required type if they are different from service type
            if (requiredServiceType != serviceType)
            {
                var openGenRequiredType = requiredServiceType.GetGenericDefinitionOrNull();
                if (openGenRequiredType != null && openGenRequiredType != openGenServiceType)
                    genericDecorators = genericDecorators.Append(container.GetDecoratorFactoriesOrDefault(openGenRequiredType));
            }

            // Append generic type argument decorators, registered as Object
            // Note: the condition for type arguments should be checked before generating the closed generic version
            var typeArgDecorators = container.GetDecoratorFactoriesOrDefault(typeof(object));
            if (!typeArgDecorators.IsNullOrEmpty())
                genericDecorators = genericDecorators.Append(
                    typeArgDecorators.Match(d => d.CheckCondition(request)));

            // Filter out already applied generic decorators
            // And combine with rest of decorators
            if (!genericDecorators.IsNullOrEmpty())
            {
                appliedDecoratorIDs = appliedDecoratorIDs ?? GetAppliedDecoratorIDs(request);
                if (!appliedDecoratorIDs.IsNullOrEmpty())
                    genericDecorators = genericDecorators
                        .Match(d => d.FactoryGenerator == null
                            ? appliedDecoratorIDs.IndexOf(d.FactoryID) == -1
                            : d.FactoryGenerator.GeneratedFactories.Enumerate()
                                .All(f => appliedDecoratorIDs.IndexOf(f.Value.FactoryID) == -1));

                // Generate closed-generic versions
                if (!genericDecorators.IsNullOrEmpty())
                {
                    genericDecorators = genericDecorators
                        .Map(d => d.FactoryGenerator == null ? d : d.FactoryGenerator.GetGeneratedFactory(request))
                        .Match(d => d != null);
                    decorators = decorators.Append(genericDecorators);
                }
            }

            // Filter out the recursive decorators:
            // the decorator with the same which was applied before up to the root
            if (!decorators.IsNullOrEmpty())
            {
                var parent = request.ParentOrWrapper;
                if (!parent.IsEmpty)
                {
                    var ids = parent.Enumerate().Map(p => p.FactoryID).ToArrayOrSelf();
                    decorators = decorators.Match(d => ids.IndexOf(d.FactoryID) == -1);
                }
            }

            // Return earlier if no decorators found, or we have filtered out everything
            if (decorators.IsNullOrEmpty())
                return null;

            Factory decorator;
            if (decorators.Length == 1)
            {
                decorator = decorators[0];
                if (!decorator.CheckCondition(request))
                    return null;
            }
            else
            {
                // Within remaining decorators find one with maximum Order
                // or if no Order for all decorators, then the last registered - with maximum FactoryID
                decorator = decorators
                    .OrderByDescending(d => ((Setup.DecoratorSetup)d.Setup).Order)
                    .ThenByDescending(d => d.FactoryID)
                    .FirstOrDefault(d => d.CheckCondition(request));
            }

            if (decorator == null)
                return null;

            var decoratorExpr = decorator.GetExpressionOrDefault(request);
            if (decoratorExpr == null)
                return null;

            // decorator of arrays should be converted back from IEnumerable to array.
            if (arrayElementType != null)
                decoratorExpr = Expression.Call(typeof(Enumerable), "ToArray", new[] { arrayElementType }, decoratorExpr);

            return decoratorExpr;
        }

        private static int[] GetAppliedDecoratorIDs(Request request)
        {
            var parent = request.ParentOrWrapper;
            if (parent.IsEmpty)
                return ArrayTools.Empty<int>();
            return parent.Enumerate()
                .TakeWhile(p => p.FactoryType != FactoryType.Service) // until the normal service
                .Where(p => p.FactoryType == FactoryType.Decorator)
                .Select(d => d.FactoryID)
                .ToArray();
        }

        Factory IContainer.GetWrapperFactoryOrDefault(Type serviceType)
        {
            // searches for open-generic wrapper, otherwise for concrete one
            // note: currently impossible to have both open and closed generic wrapper of the same generic type
            serviceType = serviceType.GetGenericDefinitionOrNull() ?? serviceType;
            return _registry.Value.Wrappers.GetValueOrDefault(serviceType);
        }

        Factory[] IContainer.GetDecoratorFactoriesOrDefault(Type serviceType)
        {
            var decorators = ArrayTools.Empty<Factory>();

            var allDecorators = _registry.Value.Decorators;
            if (!allDecorators.IsEmpty)
                decorators = allDecorators.GetValueOrDefault(serviceType) ?? ArrayTools.Empty<Factory>();

            decorators = GetCombinedRegisteredAndDynamicFactories(
                    decorators.Map(d => new KV<object, Factory>(DefaultKey.Value, d)),
                    FactoryType.Decorator, serviceType, null)
                .Map(it => it.Value);

            if (!Rules.FallbackContainers.IsNullOrEmpty())
            {
                var fallbackDecorators = Rules.FallbackContainers
                    .SelectMany(c => c.Container.GetDecoratorFactoriesOrDefault(serviceType))
                    .ToArrayOrSelf();
                if (!fallbackDecorators.IsNullOrEmpty())
                    decorators = decorators.Append(fallbackDecorators);
            }

            return decorators;
        }

        Type IContainer.GetWrappedType(Type serviceType, Type requiredServiceType)
        {
            if (requiredServiceType != null && requiredServiceType.IsOpenGeneric())
                return ((IContainer)this).GetWrappedType(serviceType, null);

            serviceType = requiredServiceType ?? serviceType;

            var wrappedType = serviceType.GetArrayElementTypeOrNull();
            if (wrappedType == null)
            {
                var factory = ((IContainer)this).GetWrapperFactoryOrDefault(serviceType);
                if (factory != null)
                {
                    wrappedType = ((Setup.WrapperSetup)factory.Setup)
                        .GetWrappedTypeOrNullIfWrapsRequired(serviceType);
                    if (wrappedType == null)
                        return null;
                }
            }

            return wrappedType == null ? serviceType
                : ((IContainer)this).GetWrappedType(wrappedType, null);
        }

        /// <summary>For given instance resolves and sets properties and fields.</summary>
        /// <param name="instance">Service instance with properties to resolve and initialize.</param>
        /// <param name="propertiesAndFields">(optional) Function to select properties and fields, overrides all other rules if specified.
        /// If not specified then method will use container <see cref="DryIoc.Rules.PropertiesAndFields"/>,
        /// or if not specified method fallbacks to <see cref="PropertiesAndFields.Auto"/>.</param>
        /// <returns>Instance with assigned properties and fields.</returns>
        /// <remarks>Different Rules could be combined together using <see cref="PropertiesAndFields.OverrideWith"/> method.</remarks>
        public object InjectPropertiesAndFields(object instance, PropertiesAndFieldsSelector propertiesAndFields)
        {
            propertiesAndFields = propertiesAndFields
                                  ?? Rules.PropertiesAndFields
                                  ?? PropertiesAndFields.Auto;

            var instanceType = instance.ThrowIfNull().GetType();

            var request = Request.Create(this, instanceType)
                .WithResolvedFactory(new UsedInstanceFactory(instanceType));

            var requestInfo = request.RequestInfo;
            var resolver = (IResolver)this;

            foreach (var serviceInfo in propertiesAndFields(request))
                if (serviceInfo != null)
                {
                    var details = serviceInfo.Details;

                    var value = resolver.Resolve(
                        serviceInfo.ServiceType,
                        details.ServiceKey,
                        details.IfUnresolved == IfUnresolved.ReturnDefault,
                        details.RequiredServiceType,
                        preResolveParent: requestInfo,
                        scope: null);

                    if (value != null)
                        serviceInfo.SetValue(instance, value);
                }

            return instance;
        }

        /// <summary>Adds factory expression to cache identified by factory ID (<see cref="Factory.FactoryID"/>).</summary>
        /// <param name="factoryID">Key in cache.</param>
        /// <param name="factoryExpression">Value to cache.</param>
        public void CacheFactoryExpression(int factoryID, Expression factoryExpression)
        {
            var registry = _registry.Value;
            if (!registry.Services.IsEmpty)
            {
                var cacheRef = registry.FactoryExpressionCache;
                var cacheVal = cacheRef.Value;
                if (!cacheRef.TrySwapIfStillCurrent(cacheVal, cacheVal.AddOrUpdate(factoryID, factoryExpression)))
                    cacheRef.Swap(val => val.AddOrUpdate(factoryID, factoryExpression));
            }
        }

        /// <summary>Searches and returns cached factory expression, or null if not found.</summary>
        /// <param name="factoryID">Factory ID to lookup by.</param> <returns>Found expression or null.</returns>
        public Expression GetCachedFactoryExpressionOrDefault(int factoryID)
        {
            return _registry.Value.FactoryExpressionCache.Value.GetValueOrDefault(factoryID) as Expression;
        }

        // todo: v3: remove
        /// <summary>State item objects which may include: singleton instances for fast access, reuses, reuse wrappers, factory delegates, etc..</summary>
        public object[] ResolutionStateCache
        {
            get { return null; }
        }

        /// <summary>Converts known items into custom expression or wraps in <see cref="ConstantExpression"/>.</summary>
        /// <param name="item">Item to convert.</param> 
        /// <param name="itemType">(optional) Type of item, otherwise item <see cref="object.GetType()"/>.</param>
        /// <param name="throwIfStateRequired">(optional) Throws for non-primitive and not-recognized items, 
        /// identifying that result expression require run-time state. For compiled expression it means closure in lambda delegate.</param>
        /// <returns>Returns constant or state access expression for added items.</returns>
        public Expression GetOrAddStateItemExpression(object item, Type itemType = null, bool throwIfStateRequired = false)
        {
            if (item == null)
                return itemType != null
                    ? Expression.Constant(null, itemType)
                    : Expression.Constant(null);

            if (item is DefaultKey)
                return Expression.Call(typeof(DefaultKey), "Of", ArrayTools.Empty<Type>(),
                    Expression.Constant(((DefaultKey)item).RegistrationOrder));

            itemType = itemType ?? item.GetType();
            if (itemType.IsPrimitive() ||
                itemType.IsAssignableTo(typeof(Type)))
                return Expression.Constant(item, itemType);

            if (itemType.IsArray)
            {
                var elemType = itemType.GetElementType().ThrowIfNull();
                var elems = ((IEnumerable)item).Cast<object>().Select(it => GetOrAddStateItemExpression(it, null, throwIfStateRequired));
                var elemExprs = Expression.NewArrayInit(elemType, elems);
                return elemExprs;
            }

            var convertibleToExpression = item as IConvertibleToExpression;
            if (convertibleToExpression != null)
                return convertibleToExpression.ToExpression(it => GetOrAddStateItemExpression(it));

            if (Rules.ItemToExpressionConverter != null)
            {
                var itemExpr = Rules.ItemToExpressionConverter(item, itemType);
                if (itemExpr != null)
                    return itemExpr;
            }

            Throw.If(throwIfStateRequired || Rules.ThrowIfRuntimeStateRequired,
                Error.StateIsRequiredToUseItem, item);

            return Expression.Constant(item, itemType);
        }

        /// <inheritdoc />
        public int GetOrAddStateItem(object item)
        {
            return -1;
        }

        #endregion

        #region Factories Add/Get

        internal sealed class FactoriesEntry
        {
            public readonly DefaultKey LastDefaultKey;
            public readonly ImTreeMap<object, Factory> Factories;

            // lastDefaultKey may be null
            public FactoriesEntry(DefaultKey lastDefaultKey, ImTreeMap<object, Factory> factories)
            {
                LastDefaultKey = lastDefaultKey;
                Factories = factories;
            }

            public static readonly FactoriesEntry Empty =
                new FactoriesEntry(null, ImTreeMap<object, Factory>.Empty);

            public FactoriesEntry With(Factory factory, object serviceKey = null)
            {
                var lastDefaultKey = serviceKey != null
                    ? LastDefaultKey // if service key is specified, the default one remains the same
                    : LastDefaultKey == null
                        ? DefaultKey.Value
                        : LastDefaultKey.Next();

                var factories = Factories.AddOrUpdate(serviceKey ?? lastDefaultKey, factory);

                return new FactoriesEntry(lastDefaultKey, factories);
            }
        }

        private static Type GetRegisteredServiceType(Request request)
        {
            var requiredServiceType = request.RequiredServiceType;
            if (requiredServiceType != null && requiredServiceType.IsOpenGeneric())
                return requiredServiceType;

            // Special case when open-generic required service type is encoded in ServiceKey as array of { ReqOpenGenServType, ServKey }
            // presumes that required service type is closed generic
            var actualServiceType = request.GetActualServiceType();
            if (actualServiceType.IsClosedGeneric())
            {
                var serviceKey = request.ServiceKey;
                var serviceKeyWithOpenGenericRequiredType = serviceKey as object[];
                if (serviceKeyWithOpenGenericRequiredType != null &&
                    serviceKeyWithOpenGenericRequiredType.Length == 2)
                {
                    var openGenericType = serviceKeyWithOpenGenericRequiredType[0] as Type;
                    if (openGenericType != null &&
                        openGenericType == actualServiceType.GetGenericDefinitionOrNull())
                    {
                        actualServiceType = openGenericType;
                        serviceKey = serviceKeyWithOpenGenericRequiredType[1];

                        // note: Mutates the request
                        request.ChangeServiceKey(serviceKey);
                    }
                }
            }

            return actualServiceType;
        }

        private KV<object, Factory>[] GetRegisteredServiceFactoriesOrNull(Type serviceType, object serviceKey)
        {
            var serviceFactories = _registry.Value.Services;
            var entry = serviceFactories.GetValueOrDefault(serviceType);

            // For closed-generic lookup type:
            // When entry is not found
            //   or the key in entry is not found
            // Then go to the open-generic services
            if (serviceType.IsClosedGeneric())
            {
                if (entry == null ||
                    serviceKey != null && (
                        entry is Factory && !serviceKey.Equals(DefaultKey.Value) ||
                        entry is FactoriesEntry && ((FactoriesEntry)entry).Factories.GetValueOrDefault(serviceKey) == null))
                {
                    var lookupOpenGenericType = serviceType.GetGenericTypeDefinition();
                    var openGenericEntry = serviceFactories.GetValueOrDefault(lookupOpenGenericType);
                    if (openGenericEntry != null)
                        entry = openGenericEntry;
                }
            }

            if (entry == null)
                return null;

            var factory = entry as Factory;
            if (factory != null)
                return new[] { new KV<object, Factory>(DefaultKey.Value, factory) };

            return ((FactoriesEntry)entry).Factories.Enumerate().ToArray();
        }

        private KV<object, Factory>[] GetCombinedRegisteredAndDynamicFactories(
            KV<object, Factory>[] registeredFactories, FactoryType factoryType, Type serviceType, object serviceKey)
        {
            var dynamicRegistrationProviders = Rules.DynamicRegistrationProviders;
            if (dynamicRegistrationProviders.IsNullOrEmpty())
                return registeredFactories;

            var factories = registeredFactories;

            // assign unique continuous keys across all of dynamic providers, 
            // to prevent duplicate keys and peeking the wrong factory by collection wrappers
            var dynamicKey = DefaultDynamicKey.Empty;

            for (var i = 0; i < dynamicRegistrationProviders.Length; i++)
            {
                var dynamicRegistrationProvider = dynamicRegistrationProviders[i];
                var dynamicRegistrations = dynamicRegistrationProvider(serviceType, serviceKey);
                if (dynamicRegistrations != null)
                {
                    if (factories.IsNullOrEmpty())
                    {
                        factories = dynamicRegistrations.Match(it =>
                                    it.Factory.FactoryType == factoryType &&
                                    it.Factory.ThrowIfInvalidRegistration(serviceType, serviceKey, false, Rules),
                                it => KV.Of(it.ServiceKey ?? (dynamicKey = dynamicKey.Next()), it.Factory))
                            .ToArrayOrSelf();
                        continue;
                    }

                    var remainingDynamicFactories = dynamicRegistrations
                        .Match(it =>
                            {
                                if (it.Factory.FactoryType != factoryType ||
                                    !it.Factory.ThrowIfInvalidRegistration(serviceType, serviceKey, false, Rules))
                                    return false;

                                if (it.ServiceKey == null) // for the default dynamic factory
                                {
                                    switch (it.IfAlreadyRegistered)
                                    {
                                        // accept the default if result factories don't contain it already
                                        case IfAlreadyRegistered.Keep:
                                        case IfAlreadyRegistered.Throw:
                                            return factories.IndexOf(f => f.Key is DefaultKey || f.Key is DefaultDynamicKey) == -1;

                                        // remove the default from the result factories 
                                        case IfAlreadyRegistered.Replace:
                                            factories = factories.Match(f => !(f.Key is DefaultKey || f.Key is DefaultDynamicKey));
                                            return true;

                                        case IfAlreadyRegistered.AppendNotKeyed:
                                            return true;

                                        case IfAlreadyRegistered.AppendNewImplementation:
                                            // if we cannot access to dynamic implementation type, assume that the type is new implementation
                                            if (!it.Factory.CanAccessImplementationType)
                                                return true;

                                            // keep dynamic factory if there is no result factory with the same implementation type
                                            return factories.IndexOf(f =>
                                                       f.Value.CanAccessImplementationType &&
                                                       f.Value.ImplementationType == it.Factory.ImplementationType) == -1;
                                    }
                                }
                                else // for the keyed dynamic factory 
                                {
                                    switch (it.IfAlreadyRegistered)
                                    {
                                        // remove the result factory with the same key
                                        case IfAlreadyRegistered.Replace:
                                            factories = factories.Match(f => !f.Key.Equals(it.ServiceKey));
                                            return true;

                                        // keep the dynamic factory with the new service key, otherwise skip it
                                        default:
                                            return factories.IndexOf(f => f.Key.Equals(it.ServiceKey)) == -1;
                                    }
                                }

                                return true;
                            },
                            it => KV.Of(it.ServiceKey ?? (dynamicKey = dynamicKey.Next()), it.Factory));

                    factories = factories.Append(remainingDynamicFactories);
                }
            }

            return factories;
        }

        private Factory GetServiceFactoryOrDefault(Request request,
            Rules.FactorySelectorRule factorySelector,
            bool fromDynamicRegistrations = false)
        {
            var serviceType = GetRegisteredServiceType(request);
            var serviceKey = request.ServiceKey;

            var registeredFactories = GetRegisteredServiceFactoriesOrNull(serviceType, serviceKey);

            var factories = GetCombinedRegisteredAndDynamicFactories(
                registeredFactories, FactoryType.Service, serviceType, serviceKey);

            if (factories.IsNullOrEmpty())
                return null;

            if (factorySelector != null)
            {
                var validFactories = factories.Match(
                    f => f.Value.CheckCondition(request),
                    f => new KeyValuePair<object, Factory>(f.Key, f.Value));
                var selectedFactory = factorySelector(request, validFactories);
                return selectedFactory;
            }

            // For requested keyed service just lookup for key and return anyway
            if (serviceKey != null)
            {
                var keyedFactory = factories.FindFirst(f => serviceKey.Equals(f.Key));
                if (keyedFactory != null && keyedFactory.Value.CheckCondition(request))
                    return keyedFactory.Value; // todo: skip further checks, really?
                return null;
            }

            // Filter out non default factories
            var defaultFactories = factories.Match(f => f.Key is DefaultKey || f.Key is DefaultDynamicKey);
            if (defaultFactories.Length == 0)
                return null;

            // Match default factories by condition and reuse
            var matchedFactories = defaultFactories.Match(f => f.Value.CheckCondition(request));
            if (matchedFactories.Length == 0)
                return null;

            // Adds asResolutionCall for the factory to prevent caching of in-lined expression in context with not matching condition
            // issues: #382, #429
            if (matchedFactories.Length != defaultFactories.Length)
            {
                for (var i = 0; i < matchedFactories.Length; i++)
                {
                    var factory = matchedFactories[i].Value;
                    if (factory.Reuse is CurrentScopeReuse && !factory.Setup.AsResolutionCall)
                        factory.Setup = factory.Setup.WithAsResolutionCall();
                }
            }

            // Match by metadata if requested
            var metadataKey = request.MetadataKey;
            var metadata = request.Metadata;
            if (metadataKey != null || metadata != null)
            {
                matchedFactories = matchedFactories.Match(f => f.Value.Setup.MatchesMetadata(metadataKey, metadata));
                if (matchedFactories.Length == 0)
                    return null;
            }

            // todo: May be a bug to match for more than 1 factory. Works with ResolveFactory, but may not work in other call sites.
            // Match open-generic implementation with closed service type. Performance is OK because the generated factories are cached - 
            // so there should not be repeating of the check, and not match of Performance decrease.
            if (matchedFactories.Length > 1)
            {
                matchedFactories = matchedFactories.Match(f =>
                    f.Value.FactoryGenerator == null ||
                    f.Value.FactoryGenerator.GetGeneratedFactory(request, ifErrorReturnDefault: true) != null);
                if (matchedFactories.Length == 0)
                    return null;
            }

            // Hurrah! The result is a single matched factory
            if (matchedFactories.Length == 1)
            {
                var factory = matchedFactories[0];

                // note: For resolution root sets correct default key to be used in delegate cache.
                if (request.IsResolutionCall && defaultFactories.Length > 1)
                    request.ChangeServiceKey(factory.Key);

                return factory.Value;
            }

            if (matchedFactories.Length > 1 && request.IfUnresolved == IfUnresolved.Throw)
                Throw.It(Error.ExpectedSingleDefaultFactory, matchedFactories, request);

            // Return null to allow fallback strategies
            return null;
        }

        private Factory GetWrapperFactoryOrDefault(Request request)
        {
            var serviceType = request.GetActualServiceType();
            // note: wrapper ignores the service key, and propagate service key to wrapped service

            var itemType = serviceType.GetArrayElementTypeOrNull();
            if (itemType != null)
                serviceType = typeof(IEnumerable<>).MakeGenericType(itemType);

            var factory = ((IContainer)this).GetWrapperFactoryOrDefault(serviceType);
            if (factory == null)
                return null;

            // tries to generate the factory to match the request
            if (factory.FactoryGenerator != null)
            {
                factory = factory.FactoryGenerator.GetGeneratedFactory(request);
                if (factory == null)
                    return null;
            }

            var condition = factory.Setup.Condition;
            if (condition != null && !condition(request))
                return null;

            return factory;
        }

        #endregion

        #region Implementation

        private int _disposed;
        private StackTrace _disposeStackTrace;

        private readonly Ref<Registry> _registry;
        private Ref<FactoryDelegateCache> _defaultFactoryDelegateCache;

        private readonly SingletonScope _singletonScope;

        internal readonly IScope _openedScope;
        private readonly IScopeContext _scopeContext;

        private ContainerWeakRef _resolverContext; // mutable in order the reference to be removed on Dispose

        internal void UseInstanceInternal(Type serviceType, object instance,
            IfAlreadyRegistered IfAlreadyRegistered, object serviceKey)
        {
            ThrowIfContainerDisposed();

            var scope = _openedScope ?? _singletonScope;
            var instanceType = instance == null ? typeof(object) : instance.GetType();

            _registry.Swap(r =>
            {
                var entry = r.Services.GetValueOrDefault(serviceType);

                // no entries, first registration, usual/hot path
                if (entry == null)
                {
                    // add new entry with instance factory
                    var instanceFactory = GetInstanceFactory(instance, instanceType, scope);
                    entry = serviceKey == null
                        ? (object)instanceFactory
                        : FactoriesEntry.Empty.With(instanceFactory, serviceKey);
                }
                else
                {
                    // have some registrations of instance, find if we should replace, add, or throw
                    var singleDefaultFactory = entry as Factory;
                    if (singleDefaultFactory != null)
                    {
                        if (serviceKey != null)
                        {
                            // @ifAlreadyRegistered doe no make sense for keyed, because there are no other keyed
                            entry = FactoriesEntry.Empty.With(singleDefaultFactory)
                                .With(GetInstanceFactory(instance, instanceType, scope), serviceKey);
                        }
                        else // for default instance
                        {
                            switch (IfAlreadyRegistered)
                            {
                                case IfAlreadyRegistered.Replace: // the DEFAULT option
                                    // the special case for re-use of existing factory,
                                    // we can just update scope with the new instance
                                    var reusedFactory = singleDefaultFactory as UsedInstanceFactory;
                                    if (reusedFactory != null)
                                        scope.SetOrAdd(scope.GetScopedItemIdOrSelf(reusedFactory.FactoryID), instance);
                                    else
                                        entry = GetInstanceFactory(instance, instanceType, scope);
                                    break;
                                case IfAlreadyRegistered.AppendNotKeyed:
                                    entry = FactoriesEntry.Empty.With(singleDefaultFactory)
                                        .With(GetInstanceFactory(instance, instanceType, scope));
                                    break;
                                case IfAlreadyRegistered.Throw:
                                    Throw.It(Error.UnableToRegisterDuplicateDefault, serviceType, singleDefaultFactory);
                                    break;
                                case IfAlreadyRegistered.AppendNewImplementation: // otherwise Keep the old one
                                    if (singleDefaultFactory.CanAccessImplementationType &&
                                        singleDefaultFactory.ImplementationType != instanceType)
                                        entry = FactoriesEntry.Empty.With(singleDefaultFactory)
                                            .With(GetInstanceFactory(instance, instanceType, scope));
                                    break;
                                default:
                                    break;
                            }
                        }
                    }
                    else // for multiple existing or single keyed factory
                    {
                        var singleKeyedOrManyFactories = (FactoriesEntry)entry;
                        if (serviceKey != null)
                        {
                            var keyedFactory = singleKeyedOrManyFactories.Factories.GetValueOrDefault(serviceKey);
                            if (keyedFactory == null)
                            {
                                entry = singleKeyedOrManyFactories
                                    .With(GetInstanceFactory(instance, instanceType, scope), serviceKey);
                            }
                            else // when keyed instance is found
                            {
                                switch (IfAlreadyRegistered)
                                {
                                    case IfAlreadyRegistered.Replace: // the DEFAULT option
                                        // the special case for re-use of existing factory,
                                        // we can just update scope with the new instance
                                        var reusedFactory = keyedFactory as UsedInstanceFactory;
                                        if (reusedFactory != null)
                                            scope.SetOrAdd(scope.GetScopedItemIdOrSelf(reusedFactory.FactoryID),
                                                instance);
                                        else // note: not possible for the moment
                                            Throw.It(Error.UnableToUseInstanceForExistingNonInstanceFactory,
                                                KV.Of(serviceKey, instance), keyedFactory);
                                        break;
                                    case IfAlreadyRegistered.Keep:
                                        break;
                                    default:
                                        Throw.It(Error.UnableToRegisterDuplicateKey, serviceType, serviceKey, keyedFactory);
                                        break;
                                }
                            }
                        }
                        else // for default instance
                        {
                            var defaultFactories = singleKeyedOrManyFactories.LastDefaultKey == null
                                ? ArrayTools.Empty<Factory>()
                                : singleKeyedOrManyFactories.Factories.Enumerate()
                                    .Match(it => it.Key is DefaultKey, it => it.Value)
                                    .ToArrayOrSelf();

                            if (defaultFactories.Length == 0) // no default factories among the multiple existing keyed factories
                            {
                                entry = singleKeyedOrManyFactories
                                    .With(GetInstanceFactory(instance, instanceType, scope));
                            }
                            else // there are existing default factories
                            {
                                switch (IfAlreadyRegistered)
                                {
                                    case IfAlreadyRegistered.Replace: // the DEFAULT option
                                        // the special case for reusing of existing factory,
                                        // we can just update scope with the new instance
                                        if (defaultFactories.Length == 1 && defaultFactories[0] is UsedInstanceFactory)
                                            scope.SetOrAdd(scope.GetScopedItemIdOrSelf(defaultFactories[0].FactoryID),
                                                instance);
                                        else
                                        {
                                            var keyedFactories = singleKeyedOrManyFactories.Factories.Enumerate()
                                                .Match(it => !(it.Key is DefaultKey)).ToArrayOrSelf();
                                            if (keyedFactories.Length == 0)
                                                entry = GetInstanceFactory(instance, instanceType, scope);
                                            else
                                            {
                                                var factoriesEntry = FactoriesEntry.Empty;
                                                for (int i = 0; i < keyedFactories.Length; i++)
                                                    factoriesEntry = factoriesEntry
                                                        .With(keyedFactories[i].Value, keyedFactories[i].Key);
                                                entry = factoriesEntry.With(GetInstanceFactory(instance, instanceType, scope));
                                            }
                                        }

                                        break;
                                    case IfAlreadyRegistered.AppendNotKeyed:
                                        entry = singleKeyedOrManyFactories
                                            .With(GetInstanceFactory(instance, instanceType, scope));
                                        break;
                                    case IfAlreadyRegistered.Throw:
                                        Throw.It(Error.UnableToRegisterDuplicateDefault, serviceType, defaultFactories);
                                        break;
                                    case IfAlreadyRegistered.AppendNewImplementation: // otherwise Keep the old one
                                        var duplicateImplIndex = defaultFactories.IndexOf(
                                            it => it.CanAccessImplementationType &&
                                                  it.ImplementationType == instanceType);
                                        if (duplicateImplIndex == -1) // add new implementation
                                            entry = singleKeyedOrManyFactories
                                                .With(GetInstanceFactory(instance, instanceType, scope));
                                        // otherwise do nothing - keep the old entry
                                        break;
                                    default: // IfAlreadyRegistered.Keep
                                        break;
                                }
                            }
                        }
                    }
                }

                // add instance entry to service registrations
                return r.WithServices(r.Services.AddOrUpdate(serviceType, entry));
            });
        }

        private static UsedInstanceFactory GetInstanceFactory(object instance, Type instanceType, IScope scope)
        {
            var instanceFactory = new UsedInstanceFactory(instanceType);
            scope.SetOrAdd(scope.GetScopedItemIdOrSelf(instanceFactory.FactoryID), instance);
            return instanceFactory;
        }

        // Just a wrapper, with only goal to provide and expression for instance access bound to FactoryID
        internal sealed class UsedInstanceFactory : Factory
        {
            public override Type ImplementationType { get { return _instanceType; } }
            private readonly Type _instanceType;

            public UsedInstanceFactory(Type instanceType)
            {
                _instanceType = instanceType;
            }

            /// <summary>Called from Resolve method</summary>
            public override FactoryDelegate GetDelegateOrDefault(Request request)
            {
                if (request.IsResolutionRoot)
                {
                    var decoratedExpr = request.Container.GetDecoratorExpressionOrDefault(request.WithResolvedFactory(this));
                    if (decoratedExpr != null)
                        return CompileToDelegate(decoratedExpr);
                }

                return GetInstanceFromScopeChainOrSingletons;
            }

            /// <summary>Called for Injection as dependency.</summary>
            public override Expression GetExpressionOrDefault(Request request)
            {
                return request.Container.GetDecoratorExpressionOrDefault(request.WithResolvedFactory(this))
                       ?? CreateExpressionOrDefault(request);
            }

            public override Expression CreateExpressionOrDefault(Request request)
            {
                return Resolver.CreateResolutionExpression(request, isRuntimeDependency: true);
            }

            #region Implementation

            private object GetInstanceFromScopeChainOrSingletons(object[] _, IResolverContext r, IScope __)
            {
                var scope = r.Scopes.GetCurrentScope();
                while (scope != null)
                {
                    var result = GetAndUnwrapOrDefault(scope, FactoryID);
                    if (result != null)
                        return result;
                    scope = scope.Parent;
                }

                return GetAndUnwrapOrDefault(r.SingletonScope(), FactoryID);
            }

            private static object GetAndUnwrapOrDefault(IScope scope, int factoryId)
            {
                var id = scope.GetScopedItemIdOrSelf(factoryId);
                var value = scope.GetOrAdd(id, () => null);

                if (value == null)
                    return null;

                var weaklyReferenced = value as WeakReference;
                if (weaklyReferenced != null)
                    value = weaklyReferenced.Target.ThrowIfNull(Error.WeakRefReuseWrapperGCed);

                var preventDisposable = value as object[];
                if (preventDisposable != null && preventDisposable.Length == 1)
                    value = preventDisposable[0];

                return value;
            }

            #endregion
        }

        internal sealed class FactoryDelegateCache
        {
            private const int NumberOfTrees = 32;
            private const int HashBitsToTree = NumberOfTrees - 1;  // get last 4 bits, fast (hash % NumberOfTrees)

            public static readonly FactoryDelegateCache Empty = new FactoryDelegateCache(new ImTreeMap<Type, FactoryDelegate>[NumberOfTrees], 0);

            public readonly int Count;

            public FactoryDelegate GetValueOrDefault(Type key)
            {
                var hash = key.GetHashCode();

                var t = _trees[hash & HashBitsToTree];
                if (t == null)
                    return null;

                while (t.Height != 0 && t.Hash != hash)
                    t = hash < t.Hash ? t.Left : t.Right;

                if (t.Height != 0 && key == t.Key)
                    return t.Value;

                return t.GetConflictedValueOrDefault(key, null);
            }

            public FactoryDelegateCache AddOrUpdate(Type key, FactoryDelegate value)
            {
                var hash = key.GetHashCode();

                var treeIndex = hash & HashBitsToTree;

                var trees = _trees;
                var tree = trees[treeIndex];
                if (tree == null)
                    tree = ImTreeMap<Type, FactoryDelegate>.Empty;

                tree = tree.AddOrUpdate(hash, key, value, null, false);

                var newTrees = new ImTreeMap<Type, FactoryDelegate>[NumberOfTrees];
                Array.Copy(trees, 0, newTrees, 0, NumberOfTrees);
                newTrees[treeIndex] = tree;

                return new FactoryDelegateCache(newTrees, Count + 1);
            }

            public FactoryDelegateCache Update(Type key, FactoryDelegate value)
            {
                var hash = key.GetHashCode();

                var trees = _trees;
                var tree = trees[hash & HashBitsToTree];
                if (tree == null)
                    return this;

                var newTree = tree.AddOrUpdate(hash, key, value, null, true);
                if (newTree == tree)
                    return this;

                var newTrees = new ImTreeMap<Type, FactoryDelegate>[NumberOfTrees];
                Array.Copy(trees, 0, newTrees, 0, NumberOfTrees);
                newTrees[hash & HashBitsToTree] = newTree;

                return new FactoryDelegateCache(newTrees, Count);
            }

            private readonly ImTreeMap<Type, FactoryDelegate>[] _trees;

            private FactoryDelegateCache(ImTreeMap<Type, FactoryDelegate>[] newTrees, int count)
            {
                _trees = newTrees;
                Count = count;
            }
        }

        internal sealed class Registry
        {
            public static readonly Registry Empty = new Registry();
            public static readonly Registry Default = new Registry(WrappersSupport.Wrappers);

            // Factories:
            public readonly ImTreeMap<Type, object> Services;
            public readonly ImTreeMap<Type, Factory[]> Decorators;
            public readonly ImTreeMap<Type, Factory> Wrappers;

            // Cache:
            public readonly Ref<ImTreeMapIntToObj> FactoryExpressionCache;

            public readonly Ref<FactoryDelegateCache> DefaultFactoryDelegateCache;

            // key: KV where Key is ServiceType and object is ServiceKey
            // value: FactoryDelegate or/and IntTreeMap<{requiredServicType+preResolvedParent}, FactoryDelegate>
            public readonly Ref<ImTreeMap<object, KV<FactoryDelegate, ImTreeMap<object, FactoryDelegate>>>> KeyedFactoryDelegateCache;

            private enum IsChangePermitted { Permitted, Error, Ignored }
            private readonly IsChangePermitted _isChangePermitted;

            public Registry WithoutCache()
            {
                return new Registry(Services, Decorators, Wrappers,
                    Ref.Of(FactoryDelegateCache.Empty),
                    Ref.Of(ImTreeMap<object, KV<FactoryDelegate, ImTreeMap<object, FactoryDelegate>>>.Empty),
                    Ref.Of(ImTreeMapIntToObj.Empty),
                    _isChangePermitted);
            }

            private Registry(ImTreeMap<Type, Factory> wrapperFactories = null)
                : this(ImTreeMap<Type, object>.Empty,
                    ImTreeMap<Type, Factory[]>.Empty,
                    wrapperFactories ?? ImTreeMap<Type, Factory>.Empty,
                    Ref.Of(FactoryDelegateCache.Empty),
                    Ref.Of(ImTreeMap<object, KV<FactoryDelegate, ImTreeMap<object, FactoryDelegate>>>.Empty),
                    Ref.Of(ImTreeMapIntToObj.Empty),
                    IsChangePermitted.Permitted)
            { }

            private Registry(
                ImTreeMap<Type, object> services,
                ImTreeMap<Type, Factory[]> decorators,
                ImTreeMap<Type, Factory> wrappers,
                Ref<FactoryDelegateCache> defaultFactoryDelegateCache,
                Ref<ImTreeMap<object, KV<FactoryDelegate, ImTreeMap<object, FactoryDelegate>>>> keyedFactoryDelegateCache,
                Ref<ImTreeMapIntToObj> factoryExpressionCache,
                IsChangePermitted isChangePermitted)
            {
                Services = services;
                Decorators = decorators;
                Wrappers = wrappers;
                DefaultFactoryDelegateCache = defaultFactoryDelegateCache;
                KeyedFactoryDelegateCache = keyedFactoryDelegateCache;
                FactoryExpressionCache = factoryExpressionCache;
                _isChangePermitted = isChangePermitted;
            }

            internal Registry WithServices(ImTreeMap<Type, object> services)
            {
                return services == Services ? this :
                    new Registry(services, Decorators, Wrappers,
                        DefaultFactoryDelegateCache.NewRef(), KeyedFactoryDelegateCache.NewRef(),
                        FactoryExpressionCache.NewRef(), _isChangePermitted);
            }

            private Registry WithDecorators(ImTreeMap<Type, Factory[]> decorators)
            {
                return decorators == Decorators ? this :
                    new Registry(Services, decorators, Wrappers,
                        DefaultFactoryDelegateCache.NewRef(), KeyedFactoryDelegateCache.NewRef(),
                        FactoryExpressionCache.NewRef(), _isChangePermitted);
            }

            private Registry WithWrappers(ImTreeMap<Type, Factory> wrappers)
            {
                return wrappers == Wrappers ? this :
                    new Registry(Services, Decorators, wrappers,
                        DefaultFactoryDelegateCache.NewRef(), KeyedFactoryDelegateCache.NewRef(),
                        FactoryExpressionCache.NewRef(), _isChangePermitted);
            }

            public IEnumerable<ServiceRegistrationInfo> GetServiceRegistrations()
            {
                foreach (var entry in Services.Enumerate())
                {
                    var serviceType = entry.Key;
                    var factory = entry.Value as Factory;
                    if (factory != null)
                        yield return new ServiceRegistrationInfo(factory, serviceType, null);
                    else
                    {
                        var factories = ((FactoriesEntry)entry.Value).Factories;
                        foreach (var f in factories.Enumerate())
                            yield return new ServiceRegistrationInfo(f.Value, serviceType, f.Key);
                    }
                }
            }

            public Registry Register(Factory factory, Type serviceType, IfAlreadyRegistered ifAlreadyRegistered, object serviceKey)
            {
                if (_isChangePermitted != IsChangePermitted.Permitted)
                    return _isChangePermitted == IsChangePermitted.Ignored ? this
                        : Throw.For<Registry>(Error.NoMoreRegistrationsAllowed,
                            serviceType, serviceKey != null ? "with key " + serviceKey : string.Empty, factory);

                return factory.FactoryType == FactoryType.Service
                    ? WithService(factory, serviceType, serviceKey, ifAlreadyRegistered)
                    : factory.FactoryType == FactoryType.Decorator
                        ? WithDecorators(
                            Decorators.AddOrUpdate(serviceType, new[] { factory }, ArrayTools.Append))
                        : WithWrappers(
                            Wrappers.AddOrUpdate(serviceType, factory));
            }

            public Factory[] GetRegisteredFactories(Type serviceType, object serviceKey, FactoryType factoryType,
                Func<Factory, bool> condition)
            {
                serviceType = serviceType.ThrowIfNull();
                switch (factoryType)
                {
                    case FactoryType.Wrapper:
                        var arrayElementType = serviceType.GetArrayElementTypeOrNull();
                        if (arrayElementType != null)
                            serviceType = typeof(IEnumerable<>).MakeGenericType(arrayElementType);

                        var wrapper = Wrappers.GetValueOrDefault(serviceType.GetGenericDefinitionOrNull() ?? serviceType);
                        return wrapper != null && (condition == null || condition(wrapper))
                            ? new[] { wrapper }
                            : null;

                    case FactoryType.Decorator:
                        var decorators = Decorators.GetValueOrDefault(serviceType);

                        var openGenServiceType = serviceType.GetGenericDefinitionOrNull();
                        if (openGenServiceType != null)
                            decorators = decorators.Append(Decorators.GetValueOrDefault(openGenServiceType));

                        if (decorators != null && decorators.Length != 0)
                            return condition == null
                                ? decorators
                                : decorators.Match(condition);
                        return null;

                    default:
                        var entry = Services.GetValueOrDefault(serviceType);
                        if (entry == null)
                            return null;

                        var factory = entry as Factory;
                        if (factory != null)
                        {
                            if (serviceKey == null || DefaultKey.Value.Equals(serviceKey))
                                return condition == null || condition(factory)
                                    ? new[] { factory }
                                    : null;
                            return null;
                        }

                        var factories = ((FactoriesEntry)entry).Factories;
                        if (serviceKey == null)
                            return condition == null
                                ? factories.Enumerate().Map(f => f.Value).ToArrayOrSelf()
                                : factories.Enumerate().Match(f => condition(f.Value), f => f.Value).ToArrayOrSelf();

                        factory = factories.GetValueOrDefault(serviceKey);
                        return factory != null && (condition == null || condition(factory))
                            ? new[] { factory }
                            : null;
                }
            }

            public bool ClearCache(Type serviceType, object serviceKey, FactoryType factoryType)
            {
                var factories = GetRegisteredFactories(serviceType, serviceKey, factoryType, null);
                if (factories.IsNullOrEmpty())
                    return false;

                for (var i = 0; i < factories.Length; i++)
                    WithoutFactoryCache(factories[i], serviceType, serviceKey);

                return true;
            }

            private Registry WithService(Factory factory, Type serviceType, object serviceKey, IfAlreadyRegistered ifAlreadyRegistered)
            {
                Factory replacedFactory = null;
                ImTreeMap<object, Factory> replacedFactories = null;
                ImTreeMap<Type, object> services;
                if (serviceKey == null)
                {
                    services = Services.AddOrUpdate(serviceType, factory, (oldEntry, newEntry) =>
                    {
                        if (oldEntry == null)
                            return newEntry;

                        var newFactory = (Factory)newEntry;

                        var oldFactoriesEntry = oldEntry as FactoriesEntry;
                        if (oldFactoriesEntry != null && oldFactoriesEntry.LastDefaultKey == null) // no default registered yet
                            return oldFactoriesEntry.With(newFactory);

                        var oldFactory = oldFactoriesEntry == null ? (Factory)oldEntry : null;
                        switch (ifAlreadyRegistered)
                        {
                            case IfAlreadyRegistered.Throw:
                                oldFactory = oldFactory ?? oldFactoriesEntry.Factories.GetValueOrDefault(oldFactoriesEntry.LastDefaultKey);
                                return Throw.For<object>(Error.UnableToRegisterDuplicateDefault, serviceType, oldFactory);

                            case IfAlreadyRegistered.Keep:
                                return oldEntry;

                            case IfAlreadyRegistered.Replace:
                                if (oldFactoriesEntry != null)
                                {
                                    var newFactories = oldFactoriesEntry.Factories;
                                    if (oldFactoriesEntry.LastDefaultKey != null)
                                    {
                                        newFactories = ImTreeMap<object, Factory>.Empty;
                                        var removedFactories = ImTreeMap<object, Factory>.Empty;
                                        foreach (var f in newFactories.Enumerate())
                                            if (f.Key is DefaultKey)
                                                removedFactories = removedFactories.AddOrUpdate(f.Key, f.Value);
                                            else
                                                newFactories = newFactories.AddOrUpdate(f.Key, f.Value);

                                        replacedFactories = removedFactories;
                                    }

                                    return new FactoriesEntry(DefaultKey.Value,
                                        newFactories.AddOrUpdate(DefaultKey.Value, newFactory));
                                }

                                replacedFactory = oldFactory;
                                return newEntry;

                            case IfAlreadyRegistered.AppendNewImplementation:
                                var implementationType = newFactory.ImplementationType;
                                if (implementationType == null ||
                                    oldFactory != null && oldFactory.ImplementationType != implementationType ||
                                    oldFactoriesEntry != null && oldFactoriesEntry.Factories.Enumerate()
                                        .All(f => f.Value.ImplementationType != implementationType))
                                {
                                    return (oldFactoriesEntry ?? FactoriesEntry.Empty.With(oldFactory)).With(newFactory);
                                }

                                return oldEntry;

                            default:
                                return (oldFactoriesEntry ?? FactoriesEntry.Empty.With(oldFactory)).With(newFactory);
                        }
                    });
                }
                else // serviceKey != null
                {
                    var factories = FactoriesEntry.Empty.With(factory, serviceKey);
                    services = Services.AddOrUpdate(serviceType, factories, (oldEntry, newEntry) =>
                    {
                        if (oldEntry == null)
                            return newEntry;

                        if (oldEntry is Factory) // if registered is default, just add it to new entry
                            return ((FactoriesEntry)newEntry).With((Factory)oldEntry);

                        var oldFactories = (FactoriesEntry)oldEntry;
                        return new FactoriesEntry(oldFactories.LastDefaultKey,
                            oldFactories.Factories.AddOrUpdate(serviceKey, factory, (oldFactory, newFactory) =>
                            {
                                if (oldFactory == null)
                                    return factory;

                                switch (ifAlreadyRegistered)
                                {
                                    case IfAlreadyRegistered.Keep:
                                        return oldFactory;

                                    case IfAlreadyRegistered.Replace:
                                        replacedFactory = oldFactory;
                                        return newFactory;

                                    default:
                                        return Throw.For<Factory>(Error.UnableToRegisterDuplicateKey, serviceType, serviceKey, oldFactory);
                                }
                            }));
                    });
                }

                // Note: We are reusing replaced factory (with same setup and reuse) by using the ID.
                // It is possible because cache depends only on ID.
                var reuseReplacedInstanceFactory = false;
                if (replacedFactory != null)
                {
                    var replacedInstanceFactory = replacedFactory as InstanceFactory;
                    reuseReplacedInstanceFactory =
                        replacedInstanceFactory != null && factory is InstanceFactory &&
                        replacedInstanceFactory.Reuse == factory.Reuse &&
                        replacedInstanceFactory.Setup == factory.Setup;

                    if (reuseReplacedInstanceFactory)
                        ((InstanceFactory)factory).FactoryID = replacedInstanceFactory.FactoryID;
                }

                var registry = this;
                if (registry.Services != services)
                {
                    registry = new Registry(services, Decorators, Wrappers,
                        DefaultFactoryDelegateCache.NewRef(), KeyedFactoryDelegateCache.NewRef(),
                        FactoryExpressionCache.NewRef(), _isChangePermitted);

                    if ((replacedFactories != null || replacedFactory != null) &&
                        !reuseReplacedInstanceFactory)
                    {
                        if (replacedFactory != null)
                            registry = registry.WithoutFactoryCache(replacedFactory, serviceType, serviceKey);
                        else if (replacedFactories != null)
                            foreach (var f in replacedFactories.Enumerate())
                                registry = registry.WithoutFactoryCache(f.Value, serviceType, serviceKey);
                    }
                }

                return registry;
            }

            public Registry Unregister(FactoryType factoryType, Type serviceType, object serviceKey, Func<Factory, bool> condition)
            {
                if (_isChangePermitted != IsChangePermitted.Permitted)
                    return _isChangePermitted == IsChangePermitted.Ignored ? this
                        : Throw.For<Registry>(Error.NoMoreUnregistrationsAllowed,
                            serviceType, serviceKey != null ? "with key " + serviceKey : string.Empty, factoryType);

                switch (factoryType)
                {
                    case FactoryType.Wrapper:
                        Factory removedWrapper = null;
                        var registry = WithWrappers(Wrappers.Update(serviceType, null, (factory, _null) =>
                        {
                            if (factory != null && condition != null && !condition(factory))
                                return factory;
                            removedWrapper = factory;
                            return null;
                        }));

                        return removedWrapper == null ? this : registry.WithoutFactoryCache(removedWrapper, serviceType);

                    case FactoryType.Decorator:
                        Factory[] removedDecorators = null;
                        registry = WithDecorators(Decorators.Update(serviceType, null, (factories, _null) =>
                        {
                            var remaining = condition == null ? null : factories.Match(f => !condition(f));
                            removedDecorators = remaining == null || remaining.Length == 0 ? factories : factories.Except(remaining).ToArray();
                            return remaining;
                        }));

                        if (removedDecorators.IsNullOrEmpty())
                            return this;

                        foreach (var removedDecorator in removedDecorators)
                            registry = registry.WithoutFactoryCache(removedDecorator, serviceType);

                        return registry;

                    default:
                        return UnregisterServiceFactory(serviceType, serviceKey, condition);
                }
            }

            private Registry UnregisterServiceFactory(Type serviceType, object serviceKey = null, Func<Factory, bool> condition = null)
            {
                object removed = null; // Factory or FactoriesEntry or Factory[]
                ImTreeMap<Type, object> services;

                if (serviceKey == null && condition == null) // simplest case with simplest handling
                    services = Services.Update(serviceType, null, (entry, _null) =>
                    {
                        removed = entry;
                        return null;
                    });
                else
                    services = Services.Update(serviceType, null, (entry, _null) =>
                    {
                        if (entry == null)
                            return null;

                        if (entry is Factory)
                        {
                            if ((serviceKey != null && !DefaultKey.Value.Equals(serviceKey)) ||
                                (condition != null && !condition((Factory)entry)))
                                return entry; // keep entry
                            removed = entry; // otherwise remove it (the only case if serviceKey == DefaultKey.Value)
                            return null;
                        }

                        var factoriesEntry = (FactoriesEntry)entry;
                        var oldFactories = factoriesEntry.Factories;
                        var remainingFactories = ImTreeMap<object, Factory>.Empty;
                        if (serviceKey == null) // automatically means condition != null
                        {
                            // keep factories for which condition is true
                            foreach (var factory in oldFactories.Enumerate())
                                if (condition != null && !condition(factory.Value))
                                    remainingFactories = remainingFactories.AddOrUpdate(factory.Key, factory.Value);
                        }
                        else // serviceKey is not default, which automatically means condition == null
                        {
                            // set to null factory with specified key if its found
                            remainingFactories = oldFactories;
                            var factory = oldFactories.GetValueOrDefault(serviceKey);
                            if (factory != null)
                                remainingFactories = oldFactories.Height > 1
                                    ? oldFactories.Update(serviceKey, null)
                                    : ImTreeMap<object, Factory>.Empty;
                        }

                        if (remainingFactories.IsEmpty)
                        {
                            // if no more remaining factories, then delete the whole entry
                            removed = entry;
                            return null;
                        }

                        removed =
                            oldFactories.Enumerate()
                                .Except(remainingFactories.Enumerate())
                                .Select(f => f.Value)
                                .ToArray();

                        if (remainingFactories.Height == 1 && DefaultKey.Value.Equals(remainingFactories.Key))
                            return remainingFactories.Value; // replace entry with single remaining default factory

                        // update last default key if current default key was removed
                        var newDefaultKey = factoriesEntry.LastDefaultKey;
                        if (newDefaultKey != null && remainingFactories.GetValueOrDefault(newDefaultKey) == null)
                            newDefaultKey = remainingFactories.Enumerate().Select(x => x.Key)
                                .OfType<DefaultKey>().OrderByDescending(key => key.RegistrationOrder).FirstOrDefault();
                        return new FactoriesEntry(newDefaultKey, remainingFactories);
                    });

                if (removed == null)
                    return this;

                var registry = WithServices(services);

                if (removed is Factory)
                    return registry.WithoutFactoryCache((Factory)removed, serviceType, serviceKey);

                var removedFactories = removed as Factory[]
                                       ?? ((FactoriesEntry)removed).Factories.Enumerate().Select(f => f.Value).ToArray();

                foreach (var removedFactory in removedFactories)
                    registry = registry.WithoutFactoryCache(removedFactory, serviceType, serviceKey);

                return registry;
            }

            // Does not change registry, returns Registry just for convenience of method chaining
            private Registry WithoutFactoryCache(Factory factory, Type serviceType, object serviceKey = null)
            {
                if (factory.FactoryGenerator != null)
                {
                    foreach (var f in factory.FactoryGenerator.GeneratedFactories.Enumerate())
                        WithoutFactoryCache(f.Value, f.Key.Key, f.Key.Value);
                }
                else
                {
                    // clean expression cache using FactoryID as key
                    FactoryExpressionCache.Swap(_ => _.Update(factory.FactoryID, null));

                    // clean default factory cache
                    DefaultFactoryDelegateCache.Swap(_ => _.Update(serviceType, null));

                    // clean keyed/context cache from keyed and context based resolutions
                    var keyedCacheKey = serviceKey == null ? (object)serviceType : new KV<object, object>(serviceType, serviceKey);
                    KeyedFactoryDelegateCache.Swap(_ => _.Update(keyedCacheKey, null));
                }

                return this;
            }

            public Registry WithNoMoreRegistrationAllowed(bool ignoreInsteadOfThrow)
            {
                var isChangePermitted = ignoreInsteadOfThrow ? IsChangePermitted.Ignored : IsChangePermitted.Error;
                return new Registry(Services, Decorators, Wrappers,
                    DefaultFactoryDelegateCache, KeyedFactoryDelegateCache, FactoryExpressionCache,
                    isChangePermitted);
            }
        }

        private Container(Rules rules, Ref<Registry> registry, SingletonScope singletonScope,
            IScopeContext scopeContext = null, IScope openedScope = null,
            int disposed = 0, StackTrace disposeStackTrace = null,
            Container rootContainer = null)
        {
            _disposed = disposed;
            _disposeStackTrace = disposeStackTrace;

            Rules = rules;

            _registry = registry;
            _defaultFactoryDelegateCache = registry.Value.DefaultFactoryDelegateCache;

            _singletonScope = singletonScope;

            _scopeContext = scopeContext;

            if (openedScope != null)
                _openedScope = openedScope;

            // creating scope in a root container (its own root is null) is valid only for non-ambient scopes
            else if (rules.ImplicitOpenedRootScope && rootContainer == null && scopeContext == null)
                _openedScope = new Scope(null, NonAmbientRootScopeName);

            _resolverContext = new ContainerWeakRef(this, rootContainer);
        }

        #endregion
    }

    /// <summary>Container extended features.</summary>
    internal static class ContainerTools
    {
        /// <summary>For given instance resolves and sets properties and fields.
        /// It respects <see cref="DryIoc.Rules.PropertiesAndFields"/> rules set per container,
        /// or if rules are not set it uses <see cref="PropertiesAndFields.Auto"/>,
        /// or you can specify your own rules with <paramref name="propertiesAndFields"/> parameter.</summary>
        /// <typeparam name="TService">Input and returned instance type.</typeparam>Service (wrapped)
        /// <param name="container">Usually a container instance, cause <see cref="Container"/> implements <see cref="IResolver"/></param>
        /// <param name="instance">Service instance with properties to resolve and initialize.</param>
        /// <param name="propertiesAndFields">(optional) Function to select properties and fields, overrides all other rules if specified.</param>
        /// <returns>Input instance with resolved dependencies, to enable fluent method composition.</returns>
        /// <remarks>Different Rules could be combined together using <see cref="PropertiesAndFields.OverrideWith"/> method.</remarks>
        public static TService InjectPropertiesAndFields<TService>(this IContainer container,
            TService instance, PropertiesAndFieldsSelector propertiesAndFields = null)
        {
            return (TService)container.InjectPropertiesAndFields(instance, propertiesAndFields);
        }

        /// <summary>Creates service using container for injecting parameters without registering anything in <paramref name="container"/>.</summary>
        /// <param name="container">Container to use for type creation and injecting its dependencies.</param>
        /// <param name="concreteType">Type to instantiate. Wrappers (Func, Lazy, etc.) is also supported.</param>
        /// <param name="made">(optional) Injection rules to select constructor/factory method, inject parameters, properties and fields.</param>
        /// <returns>Object instantiated by constructor or object returned by factory method.</returns>
        public static object New(this IContainer container, Type concreteType, Made made = null)
        {
            // Creates independent registry
            var facade = container.WithRegistrationsCopy();

            var implType = facade.GetWrappedType(concreteType, null);
            facade.Register(implType, made: made);

            // No need to Dispose facade because it shares singleton/open scopes with source container, and disposing source container does the job.
            return facade.Resolve(concreteType);
        }

        /// <summary>Creates service using container for injecting parameters without registering anything in <paramref name="container"/>.</summary>
        /// <typeparam name="T">Type to instantiate.</typeparam>
        /// <param name="container">Container to use for type creation and injecting its dependencies.</param>
        /// <param name="made">(optional) Injection rules to select constructor/factory method, inject parameters, properties and fields.</param>
        /// <returns>Object instantiated by constructor or object returned by factory method.</returns>
        public static T New<T>(this IContainer container, Made made = null)
        {
            return (T)container.New(typeof(T), made);
        }

        /// <summary>Creates service given strongly-typed creation expression.
        /// Can be used to invoke arbitrary method returning some value with injecting its parameters from container.</summary>
        /// <typeparam name="T">Method or constructor result type.</typeparam>
        /// <param name="container">Container to use for injecting dependencies.</param>
        /// <param name="made">Creation expression.</param>
        /// <returns>Created result.</returns>
        public static T New<T>(this IContainer container, Made.TypedMade<T> made)
        {
            return (T)container.New(typeof(T), made);
        }

        /// <summary>Registers new service type with factory for registered service type.
        /// Throw if no such registered service type in container.</summary>
        /// <param name="container">Container</param> <param name="serviceType">New service type.</param>
        /// <param name="registeredServiceType">Existing registered service type.</param>
        /// <param name="serviceKey">(optional)</param> <param name="registeredServiceKey">(optional)</param>
        /// <remarks>Does nothing if registration is already exists.</remarks>
        public static void RegisterMapping(this IContainer container, Type serviceType, Type registeredServiceType,
            object serviceKey = null, object registeredServiceKey = null)
        {
            var request = Request.Create(container, registeredServiceType, registeredServiceKey);
            var factory = container.GetServiceFactoryOrDefault(request);
            factory.ThrowIfNull(Error.RegisterMappingNotFoundRegisteredService,
                registeredServiceType, registeredServiceKey);
            container.Register(factory, serviceType, serviceKey, IfAlreadyRegistered.Keep, false);
        }

        /// <summary>Registers new service type with factory for registered service type.
        /// Throw if no such registered service type in container.</summary>
        /// <param name="container">Container</param>
        /// <typeparam name="TService">New service type.</typeparam>
        /// <typeparam name="TRegisteredService">Existing registered service type.</typeparam>
        /// <param name="serviceKey">(optional)</param> <param name="registeredServiceKey">(optional)</param>
        /// <remarks>Does nothing if registration is already exists.</remarks>
        public static void RegisterMapping<TService, TRegisteredService>(this IContainer container,
            object serviceKey = null, object registeredServiceKey = null)
        {
            container.RegisterMapping(typeof(TService), typeof(TRegisteredService), serviceKey, registeredServiceKey);
        }

        // todo: v3: Mark with ObsoleteAttribute
        /// <summary>Obsolete: please use WithAutoFallbackDynamicRegistration</summary>
        public static IContainer WithAutoFallbackResolution(this IContainer container,
            IEnumerable<Type> implTypes,
            Func<IReuse, Request, IReuse> changeDefaultReuse = null,
            Func<Request, bool> condition = null)
        {
            return container.ThrowIfNull().With(rules =>
                rules.WithUnknownServiceResolvers(
                    Rules.AutoRegisterUnknownServiceRule(implTypes, changeDefaultReuse, condition)));
        }

        // todo: v3: Mark with ObsoleteAttribute
        /// <summary>Obsolete: please use WithAutoFallbackDynamicRegistration</summary>
        public static IContainer WithAutoFallbackResolution(this IContainer container,
            IEnumerable<Assembly> implTypeAssemblies,
            Func<IReuse, Request, IReuse> changeDefaultReuse = null,
            Func<Request, bool> condition = null)
        {
            var types = implTypeAssemblies.ThrowIfNull()
                .SelectMany(assembly => assembly.GetLoadedTypes())
                .Where(Registrator.IsImplementationType)
                .ToArray();
            return container.WithAutoFallbackResolution(types, changeDefaultReuse, condition);
        }

        /// <summary>Provides automatic fallback resolution mechanism for not normally registered 
        /// services. Underneath uses <see cref="Rules.WithDynamicRegistrations"/>.</summary>
        /// <param name="container">Container to use.</param>
        /// <param name="getImplTypes">Implementation type provider.</param>
        /// <param name="factory">(optional) Handler to customize the factory, e.g.
        /// specify reuse or setup. Handler should not return <c>null</c>.</param>
        /// <returns>New container with corresponding rule set.</returns>
        public static IContainer WithAutoFallbackDynamicRegistrations(this IContainer container,
            Func<Type, object, IEnumerable<Type>> getImplTypes,
            Func<Type, Factory> factory = null)
        {
            return container.ThrowIfNull()
                .With(rules => rules.WithDynamicRegistrations(
                    Rules.AutoFallbackDynamicRegistrations(getImplTypes, factory)));
        }

        /// <summary>Provides automatic fallback resolution mechanism for not normally registered 
        /// services. Underneath uses <see cref="Rules.WithDynamicRegistrations"/>.</summary>
        /// <param name="container">Container to use.</param>
        /// <param name="implTypes">Implementation types.</param>
        /// <returns>New container with corresponding rule set.</returns>
        public static IContainer WithAutoFallbackDynamicRegistrations(this IContainer container, params Type[] implTypes)
        {
            return container.WithAutoFallbackDynamicRegistrations((ignoredServiceType, ignoredServiceKey) => implTypes);
        }

        /// <summary>Provides automatic fallback resolution mechanism for not normally registered 
        /// services. Underneath uses <see cref="Rules.WithDynamicRegistrations"/>.</summary>
        /// <param name="container">Container to use.</param>
        /// <param name="reuse">The implementation reuse.</param>
        /// <param name="implTypes">Implementation types.</param>
        /// <returns>New container with corresponding rule set.</returns>
        public static IContainer WithAutoFallbackDynamicRegistrations(this IContainer container, IReuse reuse, params Type[] implTypes)
        {
            return container.WithAutoFallbackDynamicRegistrations(
                (ignoredServiceType, ignoredServiceKey) => implTypes,
                implType => new ReflectionFactory(implType, reuse));
        }

        /// <summary>Provides automatic fallback resolution mechanism for not normally registered 
        /// services. Underneath uses <see cref="Rules.WithDynamicRegistrations"/>.</summary>
        /// <param name="container">Container to use.</param>
        /// <param name="reuse">The implementation reuse</param>
        /// <param name="setup">The implementation setup, including condition</param>
        /// <param name="implTypes">Type to get implementations from.</param>
        /// <returns>New container with corresponding rule set.</returns>
        public static IContainer WithAutoFallbackDynamicRegistrations(this IContainer container, IReuse reuse, Setup setup, params Type[] implTypes)
        {
            return container.WithAutoFallbackDynamicRegistrations(
                (ignoredServiceType, ignoredServiceKey) => implTypes,
                implType => new ReflectionFactory(implType, reuse, setup: setup));
        }

        /// <summary>Provides automatic fallback resolution mechanism for not normally registered 
        /// services. Underneath uses <see cref="Rules.WithDynamicRegistrations"/>.</summary>
        /// <param name="container">Container to use.</param>
        /// <param name="getImplTypeAssemblies">Provides assemblies with implementation types.</param>
        /// <param name="factory">(optional) Handler to customize the factory, e.g.
        /// specify reuse or setup. Handler should not return <c>null</c>.</param>
        /// <returns>New container with corresponding rule set.</returns>
        public static IContainer WithAutoFallbackDynamicRegistrations(this IContainer container,
            Func<Type, object, IEnumerable<Assembly>> getImplTypeAssemblies,
            Func<Type, Factory> factory = null)
        {
            return container.ThrowIfNull().With(rules => rules.WithDynamicRegistrations(
                Rules.AutoFallbackDynamicRegistrations(
                    (serviceType, serviceKey) =>
                    {
                        var assemblies = getImplTypeAssemblies(serviceType, serviceKey);
                        if (assemblies == null)
                            return ArrayTools.Empty<Type>();
                        return assemblies
                            .SelectMany(ReflectionTools.GetLoadedTypes)
                            .Where(Registrator.IsImplementationType)
                            .ToArray();
                    },
                    factory)));
        }

        /// <summary>Provides automatic fallback resolution mechanism for not normally registered 
        /// services. Underneath uses <see cref="Rules.WithDynamicRegistrations"/>.</summary>
        /// <param name="container">Container to use.</param>
        /// <param name="implTypeAssemblies">Assemblies with implementation types.</param>
        /// <returns>New container with corresponding rule set.</returns>
        public static IContainer WithAutoFallbackDynamicRegistrations(this IContainer container, params Assembly[] implTypeAssemblies)
        {
            return container.WithAutoFallbackDynamicRegistrations((ignoredServiceType, ignoredServiceKey) => implTypeAssemblies);
        }

        /// <summary>Creates new container with provided parameters and properties
        /// to pass the custom dependency values for injection. The old parameters and properties are overridden,
        /// but not replaced.</summary>
        /// <param name="container">Container to work with.</param>
        /// <param name="parameters">(optional) Parameters specification, can be used to proved custom values.</param>
        /// <param name="propertiesAndFields">(optional) Properties and fields specification, can be used to proved custom values.</param>
        /// <returns>New container with adjusted rules.</returns>
        /// <example><code lang="cs"><![CDATA[
        ///     var c = container.WithDependencies(Parameters.Of.Type<string>(_ => "Nya!"));
        ///     var a = c.Resolve<A>(); // where A accepts string parameter in constructor
        ///     Assert.AreEqual("Nya!", a.Message)
        /// ]]></code></example>
        public static IContainer WithDependencies(this IContainer container,
            ParameterSelector parameters = null, PropertiesAndFieldsSelector propertiesAndFields = null)
        {
            return container.With(rules => rules.With(Made.Of(
                    parameters: rules.Parameters.OverrideWith(parameters),
                    propertiesAndFields: rules.PropertiesAndFields.OverrideWith(propertiesAndFields)),
                overrideRegistrationMade: true));
        }

        /// <summary>Pre-defined what-registrations predicate for <seealso cref="GenerateResolutionExpressions"/>.</summary>
        public static Func<ServiceRegistrationInfo, bool> SetupAsResolutionRoots = r => r.Factory.Setup.AsResolutionRoot;

        /// <summary>Generates all resolution root and calls expressions.</summary>
        /// <param name="container">For container</param>
        /// <param name="resolutions">Result resolution factory expressions. They could be compiled and used for actual service resolution.</param>
        /// <param name="resolutionCallDependencies">Resolution call dependencies (implemented via Resolve call): e.g. dependencies wrapped in Lazy{T}.</param>
        /// <param name="whatRegistrations">(optional) Allow to filter what registration to resolve. By default applies to all registrations.
        /// You may use <see cref="SetupAsResolutionRoots"/> to generate only for registrations with <see cref="Setup.AsResolutionRoot"/>.</param>
        /// <returns>Errors happened when resolving corresponding registrations.</returns>
        public static KeyValuePair<ServiceRegistrationInfo, ContainerException>[] GenerateResolutionExpressions(this IContainer container,
            out KeyValuePair<ServiceRegistrationInfo, Expression<FactoryDelegate>>[] resolutions,
            out KeyValuePair<RequestInfo, Expression>[] resolutionCallDependencies,
            Func<ServiceRegistrationInfo, bool> whatRegistrations = null)
        {
            var generatingContainer = container.With(rules => rules
                .WithoutEagerCachingSingletonForFasterAccess()
                .WithoutImplicitCheckForReuseMatchingScope()
                .WithDependencyResolutionCallExpressions());

            var registrations = generatingContainer.GetServiceRegistrations()
                // ignore open-generic registrations because their may be resolved only when closed.
                .Where(r => !r.ServiceType.IsOpenGeneric());

            if (whatRegistrations != null)
                registrations = registrations.Where(whatRegistrations);

            var resolutionExprList = new List<KeyValuePair<ServiceRegistrationInfo, Expression<FactoryDelegate>>>();
            var resolutionErrorList = new List<KeyValuePair<ServiceRegistrationInfo, ContainerException>>();
            foreach (var r in registrations)
            {
                try
                {
                    var request = Request.Create(generatingContainer, r.ServiceType, r.OptionalServiceKey);
                    var factoryExpr = Container.WrapInFactoryExpression(r.Factory.GetExpressionOrDefault(request));
                    resolutionExprList.Add(new KeyValuePair<ServiceRegistrationInfo, Expression<FactoryDelegate>>(r, factoryExpr));
                }
                catch (ContainerException ex)
                {
                    resolutionErrorList.Add(new KeyValuePair<ServiceRegistrationInfo, ContainerException>(r, ex));
                }
            }

            resolutions = resolutionExprList.ToArray();

            resolutionCallDependencies = generatingContainer.Rules.DependencyResolutionCallExpressions.Value.Enumerate()
                .Select(r => new KeyValuePair<RequestInfo, Expression>(r.Key, Container.OptimizeExpression(r.Value))).ToArray();

            return resolutionErrorList.ToArray();
        }

        /// <summary>Used to find potential problems when resolving the passed services <paramref name="resolutionRoots"/>.
        /// Method will collect the exceptions when resolving or injecting the specific registration.
        /// Does not create any actual service objects.</summary>
        /// <param name="container">for container</param>
        /// <param name="resolutionRoots">(optional) Examined resolved services. If empty will try to resolve every service in container.</param>
        /// <returns>Exceptions happened for corresponding registrations.</returns>
        public static KeyValuePair<ServiceRegistrationInfo, ContainerException>[] Validate(
            this IContainer container, params Type[] resolutionRoots)
        {
            return container.VerifyResolutions(resolutionRoots.IsNullOrEmpty()
                ? (Func<ServiceRegistrationInfo, bool>)null
                : registration => resolutionRoots.IndexOf(registration.ServiceType) != -1);
        }

        // todo: v3: rename to Validate, rename parameters too.
        /// <summary>Used to find potential problems in service registration setup.
        /// Method tries to get expressions for Roots/All registrations, collects happened exceptions, and
        /// returns them to user. Does not create any actual service objects.</summary>
        /// <param name="container">for container</param>
        /// <param name="whatRegistrations">(optional) Allow to filter what registration to resolve. By default applies to all registrations.</param>
        /// <returns>Exceptions happened for corresponding registrations.</returns>
        public static KeyValuePair<ServiceRegistrationInfo, ContainerException>[] VerifyResolutions(this IContainer container,
            Func<ServiceRegistrationInfo, bool> whatRegistrations = null)
        {
            KeyValuePair<ServiceRegistrationInfo, Expression<FactoryDelegate>>[] ignoredRoots;
            KeyValuePair<RequestInfo, Expression>[] ignoredDeps;
            return container.GenerateResolutionExpressions(out ignoredRoots, out ignoredDeps, whatRegistrations);
        }

        // todo: v3: remove
        // previously was checking for primitive value.
        /// <summary>Checks if custom value of the <paramref name="customValueType"/> is supported by DryIoc injection mechanism.</summary>
        /// <param name="customValueType">Type to check</param> <returns>True if supported, false otherwise.c</returns>
        public static bool IsSupportedInjectedCustomValueType(Type customValueType)
        {
            return true;
        }

        /// <summary>Represents construction of whole request info stack as expression.</summary>
        /// <param name="container">Required to access container facilities for expression conversion.</param>
        /// <param name="request">Request info to convert to expression.</param>
        /// <returns>Returns result expression.</returns>
        public static Expression RequestInfoToExpression(this IContainer container, RequestInfo request)
        {
            if (request.IsEmpty)
                return _emptyRequestInfoExpr.Value;

            // recursively ask for parent expression until it is empty
            var parentRequestInfoExpr = container.RequestInfoToExpression(request.ParentOrWrapper);

            var serviceType = request.ServiceType;
            var factoryID = request.FactoryID;
            var implementationType = request.ImplementationType;
            var requiredServiceType = request.RequiredServiceType;
            var serviceKey = request.ServiceKey;
            var metadataKey = request.MetadataKey;
            var metadata = request.Metadata;
            var factoryType = request.FactoryType;
            var ifUnresolved = request.IfUnresolved;
            var flags = request.Flags;

            var serviceTypeExpr = Expression.Constant(serviceType, typeof(Type));
            var factoryIdExpr = Expression.Constant(factoryID, typeof(int));
            var implTypeExpr = Expression.Constant(implementationType, typeof(Type));
            var reuseExpr = request.Reuse == null
                ? Expression.Constant(null, typeof(IReuse))
                : ((IReuseV3)request.Reuse).ToExpression(it => container.GetOrAddStateItemExpression(it));

            if (ifUnresolved == IfUnresolved.Throw &&
                requiredServiceType == null && serviceKey == null && metadataKey == null && metadata == null &&
                factoryType == FactoryType.Service && flags == default(RequestFlags))
                return Expression.Call(parentRequestInfoExpr, "Push", ArrayTools.Empty<Type>(),
                    serviceTypeExpr, factoryIdExpr, implTypeExpr, reuseExpr);

            var requiredServiceTypeExpr = Expression.Constant(requiredServiceType, typeof(Type));
            var servicekeyExpr = Expression.Convert(container.GetOrAddStateItemExpression(serviceKey), typeof(object));
            var factoryTypeExpr = Expression.Constant(factoryType, typeof(FactoryType));
            var flagsExpr = Expression.Constant(flags, typeof(RequestFlags));

            if (ifUnresolved == IfUnresolved.Throw &&
                metadataKey == null && metadata == null)
                return Expression.Call(parentRequestInfoExpr, "Push", ArrayTools.Empty<Type>(),
                    serviceTypeExpr, requiredServiceTypeExpr, servicekeyExpr,
                    factoryIdExpr, factoryTypeExpr, implTypeExpr, reuseExpr, flagsExpr);

            var ifUnresolvedExpr = Expression.Constant(ifUnresolved, typeof(IfUnresolved));

            if (metadataKey == null && metadata == null)
                return Expression.Call(parentRequestInfoExpr, "Push", ArrayTools.Empty<Type>(),
                    serviceTypeExpr, requiredServiceTypeExpr, servicekeyExpr, ifUnresolvedExpr,
                    factoryIdExpr, factoryTypeExpr, implTypeExpr, reuseExpr, flagsExpr);

            var metadataKeyExpr = Expression.Constant(metadataKey, typeof(string));
            var metadataExpr = Expression.Convert(container.GetOrAddStateItemExpression(metadata), typeof(object));

            return Expression.Call(parentRequestInfoExpr, "Push", ArrayTools.Empty<Type>(),
                serviceTypeExpr, requiredServiceTypeExpr, servicekeyExpr, metadataKeyExpr, metadataExpr, ifUnresolvedExpr,
                factoryIdExpr, factoryTypeExpr, implTypeExpr, reuseExpr, flagsExpr);
        }

        private static readonly Lazy<Expression> _emptyRequestInfoExpr = new Lazy<Expression>(() =>
            Expression.Field(null, typeof(RequestInfo).GetFieldOrNull("Empty")));

        // todo: v3: replace with more direct access
        /// <summary>Returns the current scope, or null if not opened and <paramref name="throwIfNotFound"/> is not set.</summary>
        /// <param name="container">Container with scope to check.</param>
        /// <param name="name">(optional) Name of scope to search in current scope or its parents.</param>
        /// <param name="throwIfNotFound">(optional) Dictates to throw exception if scope if not found.</param>
        /// <returns>Scope if found, or null otherwise (if <paramref name="throwIfNotFound"/> is not set).</returns>
        public static IScope GetCurrentScope(this IContainer container, object name = null, bool throwIfNotFound = false)
        {
            return container.ContainerWeakRef.Scopes.GetCurrentNamedScope(name, throwIfNotFound);
        }

        /// <summary>Clears delegate and expression cache for specified <typeparamref name="T"/>.
        /// But does not clear instances of already resolved/created singletons and scoped services!</summary>
        /// <typeparam name="T">Target service or wrapper type.</typeparam>
        /// <param name="container">Container to operate.</param>
        /// <param name="factoryType">(optional) If not specified, clears cache for all <see cref="FactoryType"/>.</param>
        /// <param name="serviceKey">(optional) If omitted, the cache will be cleared for all registrations of <typeparamref name="T"/>.</param>
        /// <returns>True if type is found in the cache and cleared - false otherwise.</returns>
        public static bool ClearCache<T>(this IContainer container, FactoryType? factoryType = null, object serviceKey = null)
        {
            return container.ClearCache(typeof(T), factoryType, serviceKey);
        }

        /// <summary>Clears delegate and expression cache for specified service.
        /// But does not clear instances of already resolved/created singletons and scoped services!</summary>
        /// <param name="container">Container to operate.</param>
        /// <param name="serviceType">Target service type.</param>
        /// <param name="factoryType">(optional) If not specified, clears cache for all <see cref="FactoryType"/>.</param>
        /// <param name="serviceKey">(optional) If omitted, the cache will be cleared for all registrations of <paramref name="serviceType"/>.</param>
        /// <returns>True if type is found in the cache and cleared - false otherwise.</returns>
        public static bool ClearCache(this IContainer container, Type serviceType,
            FactoryType? factoryType = null, object serviceKey = null)
        {
            // todo: v3: remove cast. Move to IContainer.
            return ((Container)container).ClearCache(serviceType, factoryType, serviceKey);
        }
    }

    /// <summary>Interface used to convert reuse instance to expression.</summary>
    internal interface IConvertibleToExpression
    {
        /// <summary>Returns expression representation without closure.</summary>
        /// <param name="fallbackConverter">Delegate converting of sub-items, constants to container.</param>
        /// <returns>Expression representation.</returns>
        Expression ToExpression(Func<object, Expression> fallbackConverter);
    }

    /// <summary>Used to represent multiple default service keys.
    /// Exposes <see cref="RegistrationOrder"/> to determine order of service added.</summary>
    internal sealed class DefaultKey
    {
        /// <summary>Default value.</summary>
        public static readonly DefaultKey Value = new DefaultKey(0);

        /// <summary>Allows to determine service registration order.</summary>
        public readonly int RegistrationOrder;

        /// <summary>Create new default key with specified registration order.</summary>
        /// <param name="registrationOrder"></param> <returns>New default key.</returns>
        public static DefaultKey Of(int registrationOrder)
        {
            return registrationOrder == 0 ? Value : new DefaultKey(registrationOrder);
        }

        /// <summary>Returns next default key with increased <see cref="RegistrationOrder"/>.</summary>
        /// <returns>New key.</returns>
        public DefaultKey Next()
        {
            return Of(RegistrationOrder + 1);
        }

        /// <summary>Compares keys based on registration order.</summary>
        /// <param name="key">Key to compare with.</param>
        /// <returns>True if keys have the same order.</returns>
        public override bool Equals(object key)
        {
            if (key == null) // to enable comparison with null (unspecified)
                return true;
            var defaultKey = key as DefaultKey;
            return defaultKey != null && defaultKey.RegistrationOrder == RegistrationOrder;
        }

        /// <summary>Returns registration order as hash.</summary> <returns>Hash code.</returns>
        public override int GetHashCode()
        {
            return RegistrationOrder;
        }

        /// <summary>Prints registration order to string.</summary> <returns>Printed string.</returns>
        public override string ToString()
        {
            return "DefaultKey(" + RegistrationOrder + ")";
        }

        private DefaultKey(int registrationOrder)
        {
            RegistrationOrder = registrationOrder;
        }
    }

    /// <summary>Represents default key for dynamic registrations</summary>
    internal sealed class DefaultDynamicKey : IConvertibleToExpression
    {
        /// <summary>Default value.</summary>
        public static readonly DefaultDynamicKey Empty = new DefaultDynamicKey(0);

        /// <summary>Associated ID.</summary>
        public readonly int ID;

        /// <summary>Returns dynamic key with specified ID. The key itself may be non unique, and requested from pool.</summary>
        /// <param name="id">Associated ID.</param> <returns>The key.</returns>
        public static DefaultDynamicKey Of(int id)
        {
            return id == 0 ? Empty : new DefaultDynamicKey(id);
        }

        /// <summary>Returns next dynamic key with increased <see cref="ID"/>.</summary> <returns>Next key.</returns>
        public DefaultDynamicKey Next()
        {
            return Of(ID + 1);
        }

        /// <summary>Compares key's IDs.</summary> <param name="key">Key to compare with.</param>
        /// <returns>True if keys have the same IDs.</returns>
        public override bool Equals(object key)
        {
            if (key == null)
                return true;
            var other = key as DefaultDynamicKey;
            return other != null && other.ID == ID;
        }

        /// <summary>Returns key index as hash.</summary> <returns>Hash code.</returns>
        public override int GetHashCode()
        {
            return ID;
        }

        /// <summary>Prints registration order to string.</summary> <returns>Printed string.</returns>
        public override string ToString()
        {
            return "DefaultDynamicKey(" + ID + ")";
        }

        /// <inheritdoc />
        public Expression ToExpression(Func<object, Expression> fallbackConverter)
        {
            return Expression.Call(typeof(DefaultDynamicKey), "Of", ArrayTools.Empty<Type>(), Expression.Constant(ID));
        }

        private DefaultDynamicKey(int id)
        {
            ID = id;
        }
    }

    /// <summary>Provides access to Container for compiled factory delegate.</summary>
    internal interface IResolverContext
    {
        /// <summary>Provides access to current / scoped resolver.</summary>
        IResolver Resolver { get; }

        /// <summary>Access to the singleton and current scopes.</summary>
        IScopeAccess Scopes { get; }
    }

    // todo: v3: Remove dependency on  ContainerWeakRef impl. of IResolver context.
    /// <summary>Provides the shortcuts to <see cref="IResolverContext"/></summary>
    internal static class ResolverContext
    {
        /// <summary>Returns subj.</summary>
        /// <param name="ctx"></param> <returns></returns>
        public static IResolver RootResolver(this IResolverContext ctx)
        {
            return ((ContainerWeakRef)ctx).RootContainerOrSelf();
        }

        /// <summary>Returns subj.</summary>
        /// <param name="ctx"></param> <returns></returns>
        public static IScopeAccess RootScopes(this IResolverContext ctx)
        {
            return ((ContainerWeakRef)ctx).RootContainerOrSelf();
        }

        /// <summary>Returns subj.</summary>
        /// <param name="ctx"></param> <returns></returns>
        public static IScope SingletonScope(this IResolverContext ctx)
        {
            return ((ContainerWeakRef)ctx).RootContainerOrSelf().SingletonScope;
        }
    }

    /// <summary>Provides access to Container from compiled factory delegate.</summary>
    internal sealed class ContainerWeakRef : IResolverContext
    {
        /// <summary>Provides access to current / scoped resolver.</summary>
        public IResolver Resolver { get { return EnsureNotDisposed(_container); } }

        /// <summary>Access to the singleton and current scopes.</summary>
        public IScopeAccess Scopes { get { return EnsureNotDisposed(_container); } }

        /// <summary>Container access.</summary>
        public IContainer Container { get { return EnsureNotDisposed(_container); } }

        /// <summary>Returns root container (the same as container if its already the root). 
        /// Throws exception if root is disposed.</summary> 
        /// <returns>Container</returns>
        public Container RootContainerOrSelf()
        {
            return EnsureNotDisposed(_rootContainer ?? _container);
        }

        /// <summary>Creates weak reference wrapper over passed container object.</summary> 
        /// <param name="container">Container to reference.</param>
        /// <param name="rootContainer">(optional) Root container - the same as <paramref name="container"/> for root container.</param>
        public ContainerWeakRef(Container container, Container rootContainer)
        {
            _container = container;
            _rootContainer = rootContainer;
        }

        internal readonly Container _container;
        internal readonly Container _rootContainer;

        internal Container EnsureNotDisposed(Container container)
        {
            return !container.IsDisposed ? container
                : Throw.For<Container>(Error.ContainerIsDisposed, container);
        }
    }

    /// <summary>The delegate type which is actually used to create service instance by container.
    /// Delegate instance required to be static with all information supplied by <paramref name="state"/> and <paramref name="scope"/>
    /// parameters. The requirement is due to enable compilation to DynamicMethod in DynamicAssembly, and also to simplify
    /// state management and minimizes memory leaks.</summary>
    /// <param name="state">All the state items available in resolution root.</param>
    /// <param name="r">Provides access to <see cref="IResolver"/> implementation to enable nested/dynamic resolve inside:
    /// registered delegate factory, <see cref="Lazy{T}"/>, and <see cref="LazyEnumerable{TService}"/>.</param>
    /// <param name="scope">Resolution root scope: initially passed value will be null, but then the actual will be created on demand.</param>
    /// <returns>Created service object.</returns>
    internal delegate object FactoryDelegate(object[] state, IResolverContext r, IScope scope);

    /// <summary>Adds to Container support for:
    /// <list type="bullet">
    /// <item>Open-generic services</item>
    /// <item>Service generics wrappers and arrays using <see cref="Rules.UnknownServiceResolvers"/> extension point.
    /// Supported wrappers include: Func of <see cref="FuncTypes"/>, Lazy, Many, IEnumerable, arrays, Meta, KeyValuePair, DebugExpression.
    /// All wrapper factories are added into collection of <see cref="Wrappers"/>.
    /// unregistered resolution rule.</item>
    /// </list></summary>
    internal static class WrappersSupport
    {
        /// <summary>Supported Func types.</summary>
        public static readonly Type[] FuncTypes =
        {
            typeof(Func<>), typeof(Func<,>), typeof(Func<,,>), typeof(Func<,,,>), typeof(Func<,,,,>),
            typeof(Func<,,,,,>), typeof(Func<,,,,,,>), typeof(Func<,,,,,,,>)
        };

        /// <summary>Supported Action types. Yeah, action I can resolve or inject void returning method as action.</summary>
        public static readonly Type[] ActionTypes =
        {
            typeof(Action), typeof(Action<>), typeof(Action<,>), typeof(Action<,,>), typeof(Action<,,,>),
            typeof(Action<,,,,>), typeof(Action<,,,,,>), typeof(Action<,,,,,,>)
        };

        /// <summary>Supported open-generic collection types.</summary>
        public static readonly Type[] ArrayInterfaces =
            typeof(object[]).GetImplementedInterfaces()
                .Match(t => t.IsGeneric(), t => t.GetGenericTypeDefinition());

        /// <summary>Checks if passed type represents supported collection types.</summary>
        /// <param name="type">Type to examine.</param> <returns>Check result.</returns>
        public static bool IsSupportedCollectionType(this Type type)
        {
            if (type.IsArray) return true;
            var genericDefinition = type.GetGenericDefinitionOrNull();
            return genericDefinition != null && (
                       genericDefinition == typeof(LazyEnumerable<>) ||
                       ArrayInterfaces.Contains(genericDefinition));
        }

        /// <summary>Returns true if type is supported <see cref="FuncTypes"/>, and false otherwise.</summary>
        /// <param name="type">Type to check.</param><returns>True for func type, false otherwise.</returns>
        public static bool IsFunc(this Type type)
        {
            var genericDefinition = type.GetGenericDefinitionOrNull();
            return genericDefinition != null && FuncTypes.IndexOf(genericDefinition) != -1;
        }

        // todo: v3: remove as not used
        /// <summary>Returns true if type is Func with 1 or more input arguments.</summary>
        /// <param name="type">Type to check.</param><returns>True for Func type, false otherwise.</returns>
        public static bool IsFuncWithArgs(this Type type)
        {
            return type.IsFunc() && type.GetGenericTypeDefinition() != typeof(Func<>);
        }

        /// <summary>Registered wrappers by their concrete or generic definition service type.</summary>
        public static readonly ImTreeMap<Type, Factory> Wrappers = BuildSupportedWrappers();

        private static ImTreeMap<Type, Factory> BuildSupportedWrappers()
        {
            var wrappers = ImTreeMap<Type, Factory>.Empty;

            var arrayExpr = new ExpressionFactory(GetArrayExpression, setup: Setup.Wrapper);

            var arrayInterfaces = ArrayInterfaces;
            for (var i = 0; i < arrayInterfaces.Length; i++)
                wrappers = wrappers.AddOrUpdate(arrayInterfaces[i], arrayExpr);

            wrappers = wrappers.AddOrUpdate(typeof(LazyEnumerable<>),
                new ExpressionFactory(GetLazyEnumerableExpressionOrDefault, setup: Setup.Wrapper));

            wrappers = wrappers.AddOrUpdate(typeof(Lazy<>),
                new ExpressionFactory(r => GetLazyExpressionOrDefault(r), setup: Setup.Wrapper));

            wrappers = wrappers.AddOrUpdate(typeof(KeyValuePair<,>),
                new ExpressionFactory(GetKeyValuePairExpressionOrDefault, setup: Setup.WrapperWith(1)));

            wrappers = wrappers.AddOrUpdate(typeof(Meta<,>),
                new ExpressionFactory(GetMetaExpressionOrDefault, setup: Setup.WrapperWith(0)));

            wrappers = wrappers.AddOrUpdate(typeof(Tuple<,>),
                new ExpressionFactory(GetMetaExpressionOrDefault, setup: Setup.WrapperWith(0)));

            wrappers = wrappers.AddOrUpdate(typeof(LambdaExpression),
                new ExpressionFactory(GetLambdaExpressionExpressionOrDefault, setup: Setup.Wrapper));

            wrappers = wrappers.AddOrUpdate(typeof(Func<>),
                new ExpressionFactory(GetFuncOrActionExpressionOrDefault, setup: Setup.Wrapper));

            for (var i = 0; i < FuncTypes.Length; i++)
                wrappers = wrappers.AddOrUpdate(FuncTypes[i],
                    new ExpressionFactory(GetFuncOrActionExpressionOrDefault, setup: Setup.WrapperWith(i)));

            for (var i = 0; i < ActionTypes.Length; i++)
                wrappers = wrappers.AddOrUpdate(ActionTypes[i],
                    new ExpressionFactory(GetFuncOrActionExpressionOrDefault,
                        setup: Setup.WrapperWith(unwrap: _ => typeof(void))));

            wrappers = AddContainerInterfacesAndDisposableScope(wrappers);

            return wrappers;
        }

        private static ImTreeMap<Type, Factory> AddContainerInterfacesAndDisposableScope(ImTreeMap<Type, Factory> wrappers)
        {
            // Using @preventDisposal to not apply tracking disposable transient
            var asContainerWrapper = Setup.WrapperWith(preventDisposal: true);

            wrappers = wrappers.AddOrUpdate(typeof(IResolver),
                new ExpressionFactory(Container.GetResolverExpr,
                    Reuse.Transient,
                    setup: asContainerWrapper));

            // todo: replace convert with exposed Container property on ResolverContext.
            var containerFactory = new ExpressionFactory(r =>
                    Expression.Convert(Container.GetResolverExpr(r), r.ServiceType),
                Reuse.Transient,
                setup: asContainerWrapper);

            wrappers = wrappers
                .AddOrUpdate(typeof(IRegistrator), containerFactory)
                .AddOrUpdate(typeof(IContainer), containerFactory);

            wrappers = wrappers.AddOrUpdate(typeof(IDisposable),
                new ExpressionFactory(r => r.IsResolutionRoot ? null : Container.GetResolutionScopeExpression(r),
                    setup: Setup.Wrapper));

            return wrappers;
        }

        private static Expression GetArrayExpression(Request request)
        {
            var collectionType = request.GetActualServiceType();
            var container = request.Container;
            var rules = container.Rules;

            var itemType = collectionType.GetArrayElementTypeOrNull() ?? collectionType.GetGenericParamsAndArgs()[0];

            if (rules.ResolveIEnumerableAsLazyEnumerable)
            {
                var lazyEnumerableExpr = GetLazyEnumerableExpressionOrDefault(request);
                if (collectionType.GetGenericDefinitionOrNull() != typeof(IEnumerable<>))
                    return Expression.Call(typeof(Enumerable), "ToArray", new[] { itemType }, lazyEnumerableExpr);
                return lazyEnumerableExpr;
            }

            var requiredItemType = container.GetWrappedType(itemType, request.RequiredServiceType);

            var items = container.GetAllServiceFactories(requiredItemType)
                .Map(kv => new ServiceRegistrationInfo(kv.Value, requiredItemType, kv.Key))
                .ToArrayOrSelf();

            if (requiredItemType.IsClosedGeneric())
            {
                var requiredItemOpenGenericType = requiredItemType.GetGenericDefinitionOrNull();
                var openGenericItems = container.GetAllServiceFactories(requiredItemOpenGenericType)
                    .Map(f => new ServiceRegistrationInfo(f.Value,
                        requiredItemType,
                        // NOTE: Special service key with info about open-generic factory service type
                        new[] { requiredItemOpenGenericType, f.Key }))
                    .ToArrayOrSelf();
                items = items.Append(openGenericItems);
            }

            // Append registered generic types with compatible variance,
            // e.g. for IHandler<in E> - IHandler<A> is compatible with IHandler<B> if B : A.
            var includeVariantGenericItems = requiredItemType.IsGeneric() && rules.VariantGenericTypesInResolvedCollection;
            if (includeVariantGenericItems)
            {
                var variantGenericItems = container.GetServiceRegistrations()
                    .Match(x =>
                        x.ServiceType.IsGeneric() &&
                        x.ServiceType.GetGenericTypeDefinition() == requiredItemType.GetGenericTypeDefinition() &&
                        x.ServiceType != requiredItemType &&
                        x.ServiceType.IsAssignableTo(requiredItemType))
                    .ToArrayOrSelf();
                items = items.Append(variantGenericItems);
            }

            // Composite pattern support: filter out composite parent service skip wrappers and decorators
            var parent = request.Parent;
            if (parent.FactoryType != FactoryType.Service)
                parent = parent.Enumerate().FirstOrDefault(p => p.FactoryType == FactoryType.Service) ?? RequestInfo.Empty;

            if (!parent.IsEmpty && parent.GetActualServiceType() == requiredItemType) // check fast for the parent of the same type
                items = items.Match(x => x.Factory.FactoryID != parent.FactoryID &&
                                         (x.Factory.FactoryGenerator == null || !x.Factory.FactoryGenerator.GeneratedFactories.Enumerate().Any(f =>
                                              f.Value.FactoryID == parent.FactoryID &&
                                              f.Key.Key == parent.ServiceType && f.Key.Value == parent.ServiceKey)));

            // Return collection of single matched item if key is specified.
            var serviceKey = request.ServiceKey;
            if (serviceKey != null)
                items = items.Match(it => serviceKey.Equals(it.OptionalServiceKey));

            var metadataKey = request.MetadataKey;
            var metadata = request.Metadata;
            if (metadataKey != null || metadata != null)
                items = items.Match(it => it.Factory.Setup.MatchesMetadata(metadataKey, metadata));

            List<Expression> itemExprList = null;
            if (!items.IsNullOrEmpty())
            {
                itemExprList = new List<Expression>(items.Length);
                for (var i = 0; i < items.Length; i++)
                {
                    var item = items[i];
                    var itemRequest = request.Push(itemType, item.OptionalServiceKey,
                        IfUnresolved.ReturnDefault, requiredServiceType: item.ServiceType);

                    var itemFactory = container.ResolveFactory(itemRequest);
                    if (itemFactory != null)
                    {
                        var itemExpr = itemFactory.GetExpressionOrDefault(itemRequest);
                        if (itemExpr != null)
                            itemExprList.Add(itemExpr);
                    }
                }
            }

            // add items from fallback containers if any
            var fallbackContainers = container.Rules.FallbackContainers;
            if (!fallbackContainers.IsNullOrEmpty())
            {
                for (var i = 0; i < fallbackContainers.Length; i++)
                {
                    var fallbackContainer = fallbackContainers[i];
                    var fallbackRequest = request.WithNewContainer(fallbackContainer);
                    var itemsExpr = (NewArrayExpression)GetArrayExpression(fallbackRequest);
                    if (itemsExpr.Expressions.Count != 0)
                        if (itemExprList != null)
                            itemExprList.AddRange(itemsExpr.Expressions);
                        else
                            itemExprList = new List<Expression>(itemsExpr.Expressions);
                }
            }

            return Expression.NewArrayInit(itemType, itemExprList ?? Enumerable.Empty<Expression>());
        }

        private static readonly MethodInfo _resolveManyMethod =
            typeof(IResolver).GetSingleMethodOrNull("ResolveMany").ThrowIfNull();

        private static Expression GetLazyEnumerableExpressionOrDefault(Request request)
        {
            if (request.IsWrappedInFuncWithArgs(immediateParent: true))
                Throw.It(Error.NotPossibleToResolveLazyEnumerableInsideFuncWithArgs, request);

            var container = request.Container;
            var collectionType = request.ServiceType;
            var itemType = collectionType.GetArrayElementTypeOrNull() ?? collectionType.GetGenericParamsAndArgs()[0];
            var requiredItemType = container.GetWrappedType(itemType, request.RequiredServiceType);

            // Composite pattern support: find composite parent key to exclude from result.
            object compositeParentKey = null;
            Type compositeParentRequiredType = null;
            var parent = request.Parent;
            if (!parent.IsEmpty && parent.GetActualServiceType() == requiredItemType)
            {
                compositeParentKey = parent.ServiceKey ?? DefaultKey.Value;
                compositeParentRequiredType = parent.RequiredServiceType;
            }

            var resolverExpr = Container.GetResolverExpr(request);
            var resolutionScopeExpr = Container.GetResolutionScopeExpression(request);
            var preResolveParentExpr = container.RequestInfoToExpression(request.RequestInfo);

            var callResolveManyExpr = Expression.Call(resolverExpr, _resolveManyMethod,
                Expression.Constant(itemType),
                container.GetOrAddStateItemExpression(request.ServiceKey),
                Expression.Constant(requiredItemType),
                container.GetOrAddStateItemExpression(compositeParentKey),
                Expression.Constant(compositeParentRequiredType, typeof(Type)),
                preResolveParentExpr,
                resolutionScopeExpr);

            if (itemType != typeof(object)) // cast to object is not required cause Resolve already return IEnumerable<object>
                callResolveManyExpr = Expression.Call(typeof(Enumerable), "Cast", new[] { itemType }, callResolveManyExpr);

            var lazyEnumerableCtor = typeof(LazyEnumerable<>).MakeGenericType(itemType).GetSingleConstructorOrNull();
            return Expression.New(lazyEnumerableCtor, callResolveManyExpr);
        }

        /// <summary>Gets the expression for <see cref="Lazy{T}"/> wrapper.</summary>
        /// <param name="request">The resolution request.</param>
        /// <param name="nullWrapperForUnresolvedService">if set to <c>true</c> then check for service registration before creating resolution expression.</param>
        /// <returns>Expression: r => new Lazy{TService}(() => r.Resolver.Resolve{TService}(key, ifUnresolved, requiredType));</returns>
        public static Expression GetLazyExpressionOrDefault(Request request, bool nullWrapperForUnresolvedService = false)
        {
            if (request.IsWrappedInFuncWithArgs(immediateParent: true))
                Throw.It(Error.NotPossibleToResolveLazyInsideFuncWithArgs, request);

            var lazyType = request.GetActualServiceType();
            var serviceType = lazyType.GetGenericParamsAndArgs()[0];
            var serviceRequest = request.Push(serviceType);

            var serviceFactory = request.Container.ResolveFactory(serviceRequest);
            if (serviceFactory == null)
            {
                if (request.IfUnresolved == IfUnresolved.ReturnDefault)
                    return Expression.Constant(null, lazyType);
                return null;
            }

            serviceRequest = serviceRequest.WithResolvedFactory(serviceFactory, skipRecursiveDependencyCheck: true);

            var serviceExpr = Resolver.CreateResolutionExpression(serviceRequest);

            // Note: the conversion is required in .NET 3.5 to handle lack of covariance for Func<out T>
            // So that Func<Derived> may be used for Func<Base>
            if (serviceExpr.Type != serviceType)
                serviceExpr = Expression.Convert(serviceExpr, serviceType);

            var factoryExpr = Expression.Lambda(serviceExpr, null);
            var serviceFuncType = typeof(Func<>).MakeGenericType(serviceType);
            var wrapperCtor = lazyType.GetConstructorOrNull(args: serviceFuncType);
            return Expression.New(wrapperCtor, factoryExpr);
        }

        private static Expression GetFuncOrActionExpressionOrDefault(Request request)
        {
            var wrapperType = request.GetActualServiceType();
            var isAction = wrapperType == typeof(Action);
            if (!isAction)
            {
                var openGenericWrapperType = wrapperType.GetGenericDefinitionOrNull().ThrowIfNull();
                var funcIndex = FuncTypes.IndexOf(openGenericWrapperType);
                if (funcIndex == -1)
                {
                    isAction = ActionTypes.IndexOf(openGenericWrapperType) != -1;
                    Throw.If(!isAction);
                }
            }

            var argTypes = wrapperType.GetGenericParamsAndArgs();
            var argCount = isAction ? argTypes.Length : argTypes.Length - 1;
            var serviceType = isAction ? typeof(void) : argTypes[argCount];

            var flags = RequestFlags.IsWrappedInFunc;

            var argExprs = new ParameterExpression[argCount]; // may be empty, that's OK
            if (argCount != 0)
            {
                for (var i = 0; i < argCount; ++i)
                {
                    var argType = argTypes[i];
                    var argName = "_" + argType.Name + i; // valid unique argument names for code generation
                    argExprs[i] = Expression.Parameter(argType, argName);
                }

                request = request.WithArgs(argExprs);
                flags |= RequestFlags.IsWrappedInFuncWithArgs;
            }

            var serviceRequest = request.Push(serviceType, flags: flags);
            var serviceFactory = request.Container.ResolveFactory(serviceRequest);
            if (serviceFactory == null)
                return null;

            var serviceExpr = serviceFactory.GetExpressionOrDefault(serviceRequest);
            if (serviceExpr == null)
                return null;

            // Note: the conversation is required in .NET 3.5 to handle lack of covariance for Func<out T>
            // So that Func<Derived> may be used for Func<Base>
            if (!isAction && serviceExpr.Type != serviceType)
                serviceExpr = Expression.Convert(serviceExpr, serviceType);

            return Expression.Lambda(wrapperType, serviceExpr, argExprs);
        }

        private static Expression GetLambdaExpressionExpressionOrDefault(Request request)
        {
            var serviceType = request.RequiredServiceType
                .ThrowIfNull(Error.ResolutionNeedsRequiredServiceType, request);
            var serviceRequest = request.Push(serviceType);
            var factory = request.Container.ResolveFactory(serviceRequest);
            var expr = factory == null ? null : factory.GetExpressionOrDefault(serviceRequest);
            return expr == null ? null : Expression.Constant(Container.WrapInFactoryExpression(expr), typeof(LambdaExpression));
        }

        private static Expression GetKeyValuePairExpressionOrDefault(Request request)
        {
            var keyValueType = request.GetActualServiceType();
            var typeArgs = keyValueType.GetGenericParamsAndArgs();
            var serviceKeyType = typeArgs[0];
            var serviceKey = request.ServiceKey;
            if (serviceKey == null && serviceKeyType.IsValueType() ||
                serviceKey != null && !serviceKeyType.IsTypeOf(serviceKey))
                return null;

            var serviceType = typeArgs[1];
            var serviceRequest = request.Push(serviceType, serviceKey);
            var serviceFactory = request.Container.ResolveFactory(serviceRequest);
            var serviceExpr = serviceFactory == null ? null : serviceFactory.GetExpressionOrDefault(serviceRequest);
            if (serviceExpr == null)
                return null;

            var pairCtor = keyValueType.GetSingleConstructorOrNull().ThrowIfNull();
            var keyExpr = request.Container.GetOrAddStateItemExpression(serviceKey, serviceKeyType);
            var pairExpr = Expression.New(pairCtor, keyExpr, serviceExpr);
            return pairExpr;
        }

        /// <summary>Discovers and combines service with its setup metadata.
        /// Works with any generic type with first Type arg - Service type and second Type arg - Metadata type,
        /// and constructor with Service and Metadata arguments respectively.
        /// - if service key is not specified in request then method will search for all
        /// registered factories with the same metadata type ignoring keys.
        /// - if metadata is IDictionary{string, object},
        ///  then the First value matching the TMetadata type will be returned.</summary>
        /// <param name="request">Requested service.</param>
        /// <returns>Wrapper creation expression.</returns>
        public static Expression GetMetaExpressionOrDefault(Request request)
        {
            var metaType = request.GetActualServiceType();
            var typeArgs = metaType.GetGenericParamsAndArgs();

            var metaCtor = metaType.GetConstructorOrNull(args: typeArgs)
                .ThrowIfNull(Error.NotFoundMetaCtorWithTwoArgs, typeArgs, request);

            var metadataType = typeArgs[1];
            var serviceType = typeArgs[0];

            var container = request.Container;
            var requiredServiceType = container.GetWrappedType(serviceType, request.RequiredServiceType);

            var factories = container
                .GetAllServiceFactories(requiredServiceType, bothClosedAndOpenGenerics: true)
                .ToArrayOrSelf();

            if (factories.Length == 0)
                return null;

            var serviceKey = request.ServiceKey;
            if (serviceKey != null)
            {
                factories = factories.Match(f => serviceKey.Equals(f.Key));
                if (factories.Length == 0)
                    return null;
            }

            // if the service keys for some reason are not unique
            factories = factories
                .Match(f =>
                {
                    var metadata = f.Value.Setup.Metadata;
                    if (metadata == null)
                        return false;

                    if (metadataType == typeof(object))
                        return true;

                    var metadataDict = metadata as IDictionary<string, object>;
                    if (metadataDict != null)
                        return metadataType == typeof(IDictionary<string, object>)
                               || metadataDict.Values.Any(m => metadataType.IsTypeOf(m));

                    return metadataType.IsTypeOf(metadata);
                });

            if (factories.Length == 0)
                return null;

            // Prevent non-determinism when more than 1 factory is matching the metadata
            if (factories.Length > 1)
            {
                if (request.IfUnresolved == IfUnresolved.Throw)
                    Throw.It(Error.UnableToSelectFromManyRegistrationsWithMatchingMetadata, metadataType, factories, request);
                return null;
            }

            var factory = factories[0];
            if (factory == null)
                return null;

            serviceKey = factory.Key;

            var serviceRequest = request.Push(serviceType, serviceKey);
            var serviceFactory = container.ResolveFactory(serviceRequest);
            if (serviceFactory == null)
                return null;

            var serviceExpr = serviceFactory.GetExpressionOrDefault(serviceRequest);
            if (serviceExpr == null)
                return null;

            var resultMetadata = factory.Value.Setup.Metadata;
            if (metadataType != typeof(object))
            {
                var resultMetadataDict = resultMetadata as IDictionary<string, object>;
                if (resultMetadataDict != null && metadataType != typeof(IDictionary<string, object>))
                    resultMetadata = resultMetadataDict.Values.FirstOrDefault(m => metadataType.IsTypeOf(m));
            }

            var metadataExpr = request.Container.GetOrAddStateItemExpression(resultMetadata, metadataType);
            return Expression.New(metaCtor, serviceExpr, metadataExpr);
        }
    }

    /// <summary>Represents info required for dynamic registration: service key, factory, 
    /// and <see cref="IfAlreadyRegistered"/> option how to combine dynamic with normal registrations.</summary>
    internal sealed class DynamicRegistration
    {
        /// <summary>Factory</summary>
        public readonly Factory Factory;

        /// <summary>Optional: will be <see cref="IfAlreadyRegistered.AppendNotKeyed"/> by default.</summary>
        public readonly IfAlreadyRegistered IfAlreadyRegistered;

        /// <summary>Optional service key: if null the default <see cref="DefaultDynamicKey"/> will be used. </summary>
        public readonly object ServiceKey;

        /// <summary>Constructs the info</summary>
        /// <param name="factory"></param>
        /// <param name="ifAlreadyRegistered">(optional) Defines how to combine with normal registrations.
        /// Will use <see cref="IfAlreadyRegistered.AppendNotKeyed"/> by default.</param>
        /// <param name="serviceKey">(optional) Service key.</param>
        public DynamicRegistration(Factory factory,
            IfAlreadyRegistered ifAlreadyRegistered = IfAlreadyRegistered.AppendNotKeyed, object serviceKey = null)
        {
            Factory = factory.ThrowIfNull();
            ServiceKey = serviceKey;
            IfAlreadyRegistered = ifAlreadyRegistered;
        }
    }

    /// <summary> Defines resolution/registration rules associated with Container instance. They may be different for different containers.</summary>
    internal sealed class Rules
    {
        /// <summary>No rules as staring point.</summary>
        public static readonly Rules Default = new Rules();

        /// <summary>Default value for <see cref="MaxObjectGraphSize"/></summary>
        public const int DefaultMaxObjectGraphSize = 32;

        /// <summary>Max number of dependencies including nested ones,
        /// before splitting the graph with Resolve calls.</summary>
        public int MaxObjectGraphSize { get; private set; }

        /// <summary>Sets <see cref="MaxObjectGraphSize"/>. 
        /// To disable the limit please use <see cref="WithoutMaxObjectGraphSize"/></summary>
        /// <param name="size">New  value. Should be <c>1</c> or higher.</param>
        /// <returns>New rules.</returns>
        public Rules WithMaxObjectGraphSize(int size)
        {
            Throw.If(size < 1);
            var newRules = (Rules)MemberwiseClone();
            newRules.MaxObjectGraphSize = size;
            return newRules;
        }

        /// <summary>Disables the <see cref="MaxObjectGraphSize"/> limitation, 
        /// so that object graph won't be split due this setting.</summary>
        /// <returns>New rules.</returns>
        public Rules WithoutMaxObjectGraphSize()
        {
            var newRules = (Rules)MemberwiseClone();
            newRules.MaxObjectGraphSize = -1;
            return newRules;
        }

        // todo: v3: Rename to remove
        /// <summary>Obsolete: replaced with <see cref="MaxObjectGraphSize"/></summary>
        public int LevelToSplitObjectGraphIntoResolveCalls { get; private set; }

        /// <summary>Shorthand to <see cref="Made.FactoryMethod"/></summary>
        public FactoryMethodSelector FactoryMethod { get { return _made.FactoryMethod; } }

        /// <summary>Shorthand to <see cref="Made.Parameters"/></summary>
        public ParameterSelector Parameters { get { return _made.Parameters; } }

        /// <summary>Shorthand to <see cref="Made.PropertiesAndFields"/></summary>
        public PropertiesAndFieldsSelector PropertiesAndFields { get { return _made.PropertiesAndFields; } }

        /// <summary>Instructs to override per-registration made settings with these rules settings.</summary>
        public bool OverrideRegistrationMade { get; private set; }

        /// <summary>Returns new instance of the rules new Made composed out of
        /// provided factory method, parameters, propertiesAndFields.</summary>
        /// <returns>New rules.</returns>
        public Rules With(
            FactoryMethodSelector factoryMethod = null,
            ParameterSelector parameters = null,
            PropertiesAndFieldsSelector propertiesAndFields = null)
        {
            return With(Made.Of(factoryMethod, parameters, propertiesAndFields));
        }

        // todo: may be add a override with option, e.g. to fallback from made.FactoryMethod to previous FM, used by MEF at least
        /// <summary>Returns new instance of the rules with specified <see cref="Made"/>.</summary>
        /// <param name="made">New Made.Of rules.</param>
        /// <param name="overrideRegistrationMade">Instructs to override registration level Made.Of</param>
        /// <returns>New rules.</returns>
        public Rules With(Made made, bool overrideRegistrationMade = false)
        {
            var newRules = (Rules)MemberwiseClone();
            newRules._made = _made == Made.Default
                ? made
                : Made.Of(
                    made.FactoryMethod ?? _made.FactoryMethod,
                    made.Parameters ?? _made.Parameters,
                    made.PropertiesAndFields ?? _made.PropertiesAndFields);
            newRules.OverrideRegistrationMade = overrideRegistrationMade;
            return newRules;
        }

        // todo: Replace KeyValuePair with KV for consistency, or vice versa.
        /// <summary>Defines single factory selector delegate.</summary>
        /// <param name="request">Provides service request leading to factory selection.</param>
        /// <param name="factories">Registered factories with corresponding key to select from.</param>
        /// <returns>Single selected factory, or null if unable to select.</returns>
        internal delegate Factory FactorySelectorRule(Request request, KeyValuePair<object, Factory>[] factories);

        /// <summary>Rules to select single matched factory default and keyed registered factory/factories.
        /// Selectors applied in specified array order, until first returns not null <see cref="Factory"/>.
        /// Default behavior is throw on multiple registered default factories, cause it is not obvious what to use.</summary>
        public FactorySelectorRule FactorySelector { get; private set; }

        /// <summary>Sets <see cref="FactorySelector"/></summary>
        /// <param name="rule">Selectors to set, could be null to use default approach.</param> <returns>New rules.</returns>
        public Rules WithFactorySelector(FactorySelectorRule rule)
        {
            var newRules = (Rules)MemberwiseClone();
            newRules.FactorySelector = rule;
            return newRules;
        }

        /// <summary>Select last registered factory from multiple default.</summary>
        /// <returns>Factory selection rule.</returns>
        public static FactorySelectorRule SelectLastRegisteredFactory()
        {
            return (request, factories) => factories.LastOrDefault(f => f.Key.Equals(request.ServiceKey)).Value;
        }

        //we are watching you...public static
        /// <summary>Prefer specified service key (if found) over default key.
        /// Help to override default registrations in Open Scope scenarios: 
        /// I may register service with key and resolve it as default in current scope.</summary>
        /// <param name="serviceKey">Service key to look for instead default.</param>
        /// <returns>Found factory or null.</returns>
        public static FactorySelectorRule SelectKeyedOverDefaultFactory(object serviceKey)
        {
            return (request, factories) => request.ServiceKey != null
                // if service key is not default, then look for it
                ? factories.FindFirst(f => f.Key.Equals(request.ServiceKey)).Value
                // otherwise look for specified service key, and if no found look for default.
                : factories.FindFirst(f => f.Key.Equals(serviceKey)).Value
                  ?? factories.FindFirst(f => f.Key.Equals(null)).Value;
        }

        /// <summary>Specify the method signature for returning multiple keyed factories. 
        /// This is dynamic analog to the normal Container Registry.</summary>
        /// <param name="serviceType">Requested service type.</param>
        /// <param name="serviceKey">(optional) If <c>null</c> will request all factories of <paramref name="serviceType"/></param> 
        /// <returns>Key-Factory pairs.</returns>
        internal delegate IEnumerable<DynamicRegistration> DynamicRegistrationProvider(Type serviceType, object serviceKey);

        /// <summary>Providers for resolving multiple not-registered services. Null by default.</summary>
        public DynamicRegistrationProvider[] DynamicRegistrationProviders { get; private set; }

        /// <summary>Appends handler to current unknown service providers.</summary>
        /// <param name="rules">Rules to append.</param> <returns>New Rules.</returns>
        public Rules WithDynamicRegistrations(params DynamicRegistrationProvider[] rules)
        {
            var newRules = (Rules)MemberwiseClone();
            newRules.DynamicRegistrationProviders = newRules.DynamicRegistrationProviders.Append(rules);
            return newRules;
        }

        /// <summary>Defines delegate to return factory for request not resolved by registered factories or prior rules.
        /// Applied in specified array order until return not null <see cref="Factory"/>.</summary>
        /// <param name="request">Request to return factory for</param> <returns>Factory to resolve request, or null if unable to resolve.</returns>
        internal delegate Factory UnknownServiceResolver(Request request);

        /// <summary>Gets rules for resolving not-registered services. Null by default.</summary>
        public UnknownServiceResolver[] UnknownServiceResolvers { get; private set; }

        /// <summary>Appends resolver to current unknown service resolvers.</summary>
        /// <param name="rules">Rules to append.</param> <returns>New Rules.</returns>
        public Rules WithUnknownServiceResolvers(params UnknownServiceResolver[] rules)
        {
            var newRules = (Rules)MemberwiseClone();
            newRules.UnknownServiceResolvers = newRules.UnknownServiceResolvers.Append(rules);
            return newRules;
        }

        /// <summary>Removes specified resolver from unknown service resolvers, and returns new Rules.
        /// If no resolver was found then <see cref="UnknownServiceResolvers"/> will stay the same instance,
        /// so it could be check for remove success or fail.</summary>
        /// <param name="rule">Rule tor remove.</param> <returns>New rules.</returns>
        public Rules WithoutUnknownServiceResolver(UnknownServiceResolver rule)
        {
            var newRules = (Rules)MemberwiseClone();
            newRules.UnknownServiceResolvers = newRules.UnknownServiceResolvers.Remove(rule);
            return newRules;
        }

        /// <summary>Sugar on top of <see cref="WithUnknownServiceResolvers"/> to simplify setting the diagnostic action.
        /// Does not guard you from action throwing an exception. Actually can be used to throw your custom exception
        /// instead of <see cref="ContainerException"/>.</summary>
        /// <param name="handler">May be a Logger action or any other diagnostic handler.</param> 
        /// <returns>Rules with unknown resolver.</returns>
        public Rules WithUnknownServiceHandler(Action<Request> handler)
        {
            return WithUnknownServiceResolvers(request =>
            {
                handler(request);
                return null;
            });
        }

        // todo: v3: Mark with ObsoleteAttribute
        /// <summary>Replaced by ConcreteTypeDynamicRegistrations</summary>
        public static UnknownServiceResolver AutoResolveConcreteTypeRule(Func<Request, bool> condition = null)
        {
            return request =>
            {
                var concreteServiceType = request.GetActualServiceType();
                if (concreteServiceType.IsAbstract() || condition != null && !condition(request))
                    return null;

                var openGenericServiceType = concreteServiceType.GetGenericDefinitionOrNull();
                if (openGenericServiceType != null &&
                    WrappersSupport.Wrappers.GetValueOrDefault(openGenericServiceType) != null)
                    return null;

                var factory = new ReflectionFactory(concreteServiceType,
                    made: DryIoc.FactoryMethod.Constructor(true, true));

                // try resolve expression first and return null, 
                // to enable fallback to other rules if unresolved
                var requestOrDefault = request
                    .WithChangedServiceInfo(_ => _.WithIfUnresolved(IfUnresolved.ReturnDefault));

                var factoryExpr = factory.GetExpressionOrDefault(requestOrDefault);
                if (factoryExpr == null)
                    return null;

                return factory;
            };
        }

        /// <summary>Rule to automatically resolves non-registered service type which is: nor interface, nor abstract.
        /// For constructor selection we are using <see cref="DryIoc.FactoryMethod.ConstructorWithResolvableArguments"/>.
        /// The resolution creates transient services.</summary>
        /// <param name="condition">(optional) Condition for requested service type and key.</param>
        /// <param name="reuse">(optional) Reuse.</param>
        /// <returns>New rule.</returns>
        public static DynamicRegistrationProvider ConcreteTypeDynamicRegistrations(
            Func<Type, object, bool> condition = null, IReuse reuse = null)
        {
            return AutoFallbackDynamicRegistrations((serviceType, serviceKey) =>
                {
                    if (serviceType.IsAbstract() ||
                        condition != null && !condition(serviceType, serviceKey))
                        return null;

                    // exclude concrete service types which are pre-defined DryIoc wrapper types
                    var openGenericServiceType = serviceType.GetGenericDefinitionOrNull();
                    if (openGenericServiceType != null &&
                        WrappersSupport.Wrappers.GetValueOrDefault(openGenericServiceType) != null)
                        return null;

                    return new[] { serviceType }; // use concrete service type as implementation type
                },
                implType =>
                {
                    ReflectionFactory factory = null;

                    factory = new ReflectionFactory(implType, reuse,
                        made: DryIoc.FactoryMethod.ConstructorWithResolvableArgumentsIncludingNonPublic,

                        // condition checks that factory is resolvable
                        setup: Setup.With(null, req =>
                            null != factory.GetExpressionOrDefault(
                                req.WithChangedServiceInfo(r => r.WithIfUnresolved(IfUnresolved.ReturnDefault)))));

                    return factory;
                });
        }

        /// <summary>Automatically resolves non-registered service type which is: nor interface, nor abstract.
        /// The resolution creates Transient services.</summary>
        /// <param name="condition">(optional) Condition for requested service type and key.</param>
        /// <param name="reuse">(optional) Reuse.</param>
        /// <returns>New rules.</returns>
        public Rules WithConcreteTypeDynamicRegistrations(
            Func<Type, object, bool> condition = null, IReuse reuse = null)
        {
            return WithDynamicRegistrations(ConcreteTypeDynamicRegistrations(condition, reuse));
        }

        // todo: v3: Mark with ObsoleteAttribute
        /// <summary>Replaced with WithConcreteTypeDynamicRegistrations</summary>
        public Rules WithAutoConcreteTypeResolution(Func<Request, bool> condition = null)
        {
            return WithUnknownServiceResolvers(AutoResolveConcreteTypeRule(condition));
        }

        /// <summary>Creates dynamic fallback registrations for the requested service type
        /// with provided <paramref name="getImplementationTypes"/>.
        /// Fallback means that the dynamic registrations will be applied Only if no normal registrations
        /// exist for the requested service type, hence the "fallback".</summary>
        /// <param name="getImplementationTypes">Implementation types to select for service.</param>
        /// <param name="factory">(optional) Handler to customize the factory, e.g.
        /// specify reuse or setup. Handler should not return <c>null</c>.</param>
        /// <returns>Registration provider.</returns>
        public static DynamicRegistrationProvider AutoFallbackDynamicRegistrations(
            Func<Type, object, IEnumerable<Type>> getImplementationTypes,
            Func<Type, Factory> factory = null)
        {
            // cache factory for implementation type to enable reuse semantics
            var factories = Ref.Of(ImTreeMap<Type, Factory>.Empty);

            return (serviceType, serviceKey) =>
            {
                var implementationTypes = getImplementationTypes(serviceType, serviceKey);

                return implementationTypes.Match(
                    implType => implType.ImplementsServiceType(serviceType),
                    implType =>
                    {
                        var implFactory = factories.Value.GetValueOrDefault(implType);
                        if (implFactory == null)
                        {
                            factories.Swap(existingFactories =>
                            {
                                implFactory = existingFactories.GetValueOrDefault(implType);
                                if (implFactory != null)
                                    return existingFactories;

                                implFactory = factory != null
                                    ? factory(implType).ThrowIfNull()
                                    : new ReflectionFactory(implType);

                                return existingFactories.AddOrUpdate(implType, implFactory);
                            });
                        }

                        return new DynamicRegistration(implFactory, IfAlreadyRegistered.Keep);
                    });
            };
        }

        // todo: v3: Remove
        /// <summary>Obsolete: replaced by <see cref="AutoFallbackDynamicRegistrations"/></summary>
        public static UnknownServiceResolver AutoRegisterUnknownServiceRule(
            IEnumerable<Type> implTypes,
            Func<IReuse, Request, IReuse> changeDefaultReuse = null,
            Func<Request, bool> condition = null)
        {
            return request =>
            {
                if (condition != null && !condition(request))
                    return null;

                var currentScope = request.Scopes.GetCurrentScope();
                var reuse = currentScope != null
                    ? Reuse.InCurrentNamedScope(currentScope.Name)
                    : Reuse.Singleton;

                if (changeDefaultReuse != null)
                    reuse = changeDefaultReuse(reuse, request);

                var requestedServiceType = request.GetActualServiceType();
                request.Container.RegisterMany(implTypes, reuse,
                    serviceTypeCondition: serviceType =>
                        serviceType.IsOpenGeneric() && requestedServiceType.IsClosedGeneric()
                            ? serviceType == requestedServiceType.GetGenericTypeDefinition()
                            : serviceType == requestedServiceType);

                return request.Container.GetServiceFactoryOrDefault(request);
            };
        }

        /// <summary>List of containers to fallback resolution to.</summary>
        public ContainerWeakRef[] FallbackContainers { get; private set; }

        /// <summary>Appends WeakReference to new fallback container to the end of the list.</summary>
        /// <param name="container">To append.</param> <returns>New rules.</returns>
        public Rules WithFallbackContainer(IContainer container)
        {
            var newRules = (Rules)MemberwiseClone();
            newRules.FallbackContainers = newRules.FallbackContainers.AppendOrUpdate(container.ContainerWeakRef);
            return newRules;
        }

        /// <summary>Removes WeakReference to fallback container from the list.</summary>
        /// <param name="container">To remove.</param> <returns>New rules.</returns>
        public Rules WithoutFallbackContainer(IContainer container)
        {
            var newRules = (Rules)MemberwiseClone();
            newRules.FallbackContainers = newRules.FallbackContainers.Remove(container.ContainerWeakRef);
            return newRules;
        }

        /// <summary>See <see cref="WithDefaultReuseInsteadOfTransient"/></summary>
        public IReuse DefaultReuseInsteadOfTransient { get; private set; }

        // todo: v3: Obsolete("Replaced by WithDefaultReuse because for some cases IsteadOfTransient does not make sense.", error: false) 
        /// <summary>Replaced by WithDefaultReuse because for some cases IsteadOfTransient does not make sense.</summary>
        public Rules WithDefaultReuseInsteadOfTransient(IReuse reuse)
        {
            var newRules = (Rules)MemberwiseClone();
            newRules.DefaultReuseInsteadOfTransient = reuse ?? Reuse.Transient;
            return newRules;
        }

        // todo: v3: Rename to WithDefaultReuse, because using rules.WithDefaultReuseInsteadOfTransient(Reuse.Transient)) seems off.
        /// <summary>The reuse used in case if reuse is unspecified (null) in Register methods.</summary>
        /// <param name="reuse">Reuse to set. If null the <see cref="Reuse.Transient"/> will be used</param>
        /// <returns>New rules.</returns>
        public Rules WithDefaultReuse(IReuse reuse)
        {
            var newRules = (Rules)MemberwiseClone();
            newRules.DefaultReuseInsteadOfTransient = reuse ?? Reuse.Transient;
            return newRules;
        }

        /// <summary>Given item object and its type should return item "pure" expression presentation,
        /// without side-effects or external dependencies.
        /// e.g. for string "blah" <code lang="cs"><![CDATA[]]>Expression.Constant("blah", typeof(string))</code>.
        /// If unable to convert should return null.</summary>
        /// <param name="item">Item object. Item is not null.</param>
        /// <param name="itemType">Item type. Item type is not null.</param>
        /// <returns>Expression or null.</returns>
        internal delegate Expression ItemToExpressionConverterRule(object item, Type itemType);

        /// <summary><see cref="WithItemToExpressionConverter"/>.</summary>
        public ItemToExpressionConverterRule ItemToExpressionConverter { get; private set; }

        /// <summary>Specifies custom rule to convert non-primitive items to their expression representation.
        /// That may be required because DryIoc by default does not support non-primitive service keys and registration metadata.
        /// To enable non-primitive values support DryIoc need a way to recreate them as expression tree.</summary>
        /// <returns>New rules</returns>
        public Rules WithItemToExpressionConverter(ItemToExpressionConverterRule itemToExpressionOrDefault)
        {
            var newRules = (Rules)MemberwiseClone();
            newRules.ItemToExpressionConverter = itemToExpressionOrDefault;
            return newRules;
        }

        /// <summary><see cref="WithoutThrowIfDependencyHasShorterReuseLifespan"/>.</summary>
        public bool ThrowIfDependencyHasShorterReuseLifespan
        {
            get { return (_settings & Settings.ThrowIfDependencyHasShorterReuseLifespan) != 0; }
        }

        /// <summary>Turns off throwing exception when dependency has shorter reuse lifespan than its parent or ancestor.</summary>
        /// <returns>New rules with new setting value.</returns>
        public Rules WithoutThrowIfDependencyHasShorterReuseLifespan()
        {
            var newRules = (Rules)MemberwiseClone();
            newRules._settings &= ~Settings.ThrowIfDependencyHasShorterReuseLifespan;
            return newRules;
        }

        /// <summary><see cref="WithoutThrowOnRegisteringDisposableTransient"/></summary>
        public bool ThrowOnRegisteringDisposableTransient
        {
            get { return (_settings & Settings.ThrowOnRegisteringDisposableTransient) != 0; }
        }

        /// <summary>Turns Off the rule <see cref="ThrowOnRegisteringDisposableTransient"/>.
        /// Allows to register disposable transient but it is up to you to handle their disposal.
        /// You can use <see cref="WithTrackingDisposableTransients"/> to actually track disposable transient in
        /// container, so that disposal will be handled by container.</summary>
        /// <returns>New rules with setting turned off.</returns>
        public Rules WithoutThrowOnRegisteringDisposableTransient()
        {
            var newRules = (Rules)MemberwiseClone();
            newRules._settings &= ~Settings.ThrowOnRegisteringDisposableTransient;
            return newRules;
        }

        /// <summary><see cref="WithTrackingDisposableTransients"/></summary>
        public bool TrackingDisposableTransients
        {
            get { return (_settings & Settings.TrackingDisposableTransients) != 0; }
        }

        /// <summary>Turns tracking of disposable transients in dependency parent scope, or in current scope if service
        /// is resolved directly.
        ///
        /// If no open scope at the moment then resolved transient won't be tracked and it is up to you
        /// to dispose it! That's is similar situation to creating service by new - you have full control.
        ///
        /// If dependency wrapped in Func somewhere in parent chain then it also won't be tracked, because
        /// Func supposedly means multiple object creation and for container it is not clear what to do, so container
        /// delegates that to user. Func here is the similar to Owned relationship type in Autofac library.
        /// </summary>
        /// <remarks>Turning this setting On automatically turns off <see cref="ThrowOnRegisteringDisposableTransient"/>.</remarks>
        /// <returns>New rules with setting turned On.</returns>
        public Rules WithTrackingDisposableTransients()
        {
            var newRules = (Rules)MemberwiseClone();
            newRules._settings |= Settings.TrackingDisposableTransients; // turning On
            newRules._settings &= ~Settings.ThrowOnRegisteringDisposableTransient; // turning Off
            return newRules;
        }

        /// <summary><see cref="WithoutEagerCachingSingletonForFasterAccess"/>.</summary>
        public bool EagerCachingSingletonForFasterAccess
        {
            get { return (_settings & Settings.EagerCachingSingletonForFasterAccess) != 0; }
        }

        /// <summary>Turns off optimization: creating singletons during resolution of object graph.</summary>
        /// <returns>New rules with singleton optimization turned off.</returns>
        public Rules WithoutEagerCachingSingletonForFasterAccess()
        {
            var newRules = (Rules)MemberwiseClone();
            newRules._settings &= ~Settings.EagerCachingSingletonForFasterAccess;
            return newRules;
        }

        /// <summary><see cref="WithDependencyResolutionCallExpressions"/>.</summary>
        public Ref<ImTreeMap<RequestInfo, Expression>> DependencyResolutionCallExpressions { get; private set; }

        /// <summary>Specifies to generate ResolutionCall dependency creation expression
        /// and put it into collection.</summary>
        /// <returns>New rules with resolution call expressions to be populated.</returns>
        public Rules WithDependencyResolutionCallExpressions()
        {
            var newRules = (Rules)MemberwiseClone();
            newRules.DependencyResolutionCallExpressions = Ref.Of(ImTreeMap<RequestInfo, Expression>.Empty);
            return newRules;
        }

        /// <summary><see cref="ImplicitCheckForReuseMatchingScope"/></summary>
        public bool ImplicitCheckForReuseMatchingScope
        {
            get { return (_settings & Settings.ImplicitCheckForReuseMatchingScope) != 0; }
        }

        /// <summary>Removes implicit Factory <see cref="Setup.Condition"/> for non-transient service.
        /// The Condition filters out factory without matching scope.</summary>
        /// <returns>Returns new rules with flag set.</returns>
        public Rules WithoutImplicitCheckForReuseMatchingScope()
        {
            var newRules = (Rules)MemberwiseClone();
            newRules._settings &= ~Settings.ImplicitCheckForReuseMatchingScope;
            return newRules;
        }

        /// <summary><see cref="WithResolveIEnumerableAsLazyEnumerable"/>.</summary>
        public bool ResolveIEnumerableAsLazyEnumerable
        {
            get { return (_settings & Settings.ResolveIEnumerableAsLazyEnumerable) != 0; }
        }

        /// <summary>Specifies to resolve IEnumerable as LazyEnumerable.</summary>
        /// <returns>Returns new rules with flag set.</returns>
        public Rules WithResolveIEnumerableAsLazyEnumerable()
        {
            var newRules = (Rules)MemberwiseClone();
            newRules._settings |= Settings.ResolveIEnumerableAsLazyEnumerable;
            return newRules;
        }

        /// <summary><see cref="WithoutVariantGenericTypesInResolvedCollection"/>.</summary>
        public bool VariantGenericTypesInResolvedCollection
        {
            get { return (_settings & Settings.VariantGenericTypesInResolvedCollection) != 0; }
        }

        /// <summary>Flag instructs to include covariant compatible types in resolved collection.</summary>
        /// <returns>Returns new rules with flag set.</returns>
        public Rules WithoutVariantGenericTypesInResolvedCollection()
        {
            var newRules = (Rules)MemberwiseClone();
            newRules._settings &= ~Settings.VariantGenericTypesInResolvedCollection;
            return newRules;
        }

        /// <summary><seew cref="WithDefaultIfAlreadyRegistered"/>.</summary>
        public IfAlreadyRegistered DefaultIfAlreadyRegistered { get; private set; }

        /// <summary>Specifies default setting for container. By default is <see cref="IfAlreadyRegistered.AppendNotKeyed"/>.
        /// Example of use: specify Keep as a container default, then set AppendNonKeyed for explicit collection registrations.</summary>
        /// <param name="rule">New setting.</param> <returns>New rules.</returns>
        public Rules WithDefaultIfAlreadyRegistered(IfAlreadyRegistered rule)
        {
            var newRules = (Rules)MemberwiseClone();
            newRules.DefaultIfAlreadyRegistered = rule;
            return newRules;
        }

        /// <summary><see cref="WithImplicitRootOpenScope"/>.</summary>
        public bool ImplicitOpenedRootScope
        {
            get { return (_settings & Settings.ImplicitRootOpenScope) != 0; }
        }

        /// <summary>Specifies to open scope as soon as container is created (the same as for Singleton scope).
        /// That way you don't need to call <see cref="IContainer.OpenScope"/>.
        /// Implicitly opened scope will be disposed together with Singletons when container is disposed.
        /// The name of root scope is <see cref="Container.NonAmbientRootScopeName"/>.</summary>
        /// <remarks>The setting is only valid for container without ambient scope context.</remarks>
        /// <returns>Returns new rules with flag set.</returns>
        public Rules WithImplicitRootOpenScope()
        {
            var newRules = (Rules)MemberwiseClone();
            newRules._settings |= Settings.ImplicitRootOpenScope;
            return newRules;
        }

        /// <summary><see cref="WithThrowIfRuntimeStateRequired"/>.</summary>
        public bool ThrowIfRuntimeStateRequired
        {
            get { return (_settings & Settings.ThrowIfRuntimeStateRequired) != 0; }
        }

        /// <summary>Specifies to throw an exception in attempt to resolve service which require runtime state for resolution.
        /// Runtime state may be introduced by RegisterDelegate, RegisterInstance, or registering with non-primitive service key, or metadata.</summary>
        /// <returns>Returns new rules with flag set.</returns>
        public Rules WithThrowIfRuntimeStateRequired()
        {
            var newRules = (Rules)MemberwiseClone();
            newRules._settings |= Settings.ThrowIfRuntimeStateRequired;
            return newRules;
        }

        /// <summary><see cref="WithCaptureContainerDisposeStackTrace"/>.</summary>
        public bool CaptureContainerDisposeStackTrace
        {
            get { return (_settings & Settings.CaptureContainerDisposeStackTrace) != 0; }
        }

        /// <summary>Instructs to capture Dispose stack-trace to include it later into <see cref="Error.ContainerIsDisposed"/>
        /// exception for easy diagnostics.</summary> <returns>New rules with option set.</returns>
        public Rules WithCaptureContainerDisposeStackTrace()
        {
            var newRules = (Rules)MemberwiseClone();
            newRules._settings |= Settings.CaptureContainerDisposeStackTrace;
            return newRules;
        }

        #region Implementation

        private Rules()
        {
            _made = Made.Default;
            _settings = DEFAULT_SETTINGS;
            DefaultReuseInsteadOfTransient = Reuse.Transient;
            MaxObjectGraphSize = DefaultMaxObjectGraphSize;
        }

        private Made _made;

        [Flags]
        private enum Settings
        {
            ThrowIfDependencyHasShorterReuseLifespan = 1 << 1,
            ThrowOnRegisteringDisposableTransient = 1 << 2,
            TrackingDisposableTransients = 1 << 3,
            ImplicitCheckForReuseMatchingScope = 1 << 4,
            VariantGenericTypesInResolvedCollection = 1 << 5,
            ResolveIEnumerableAsLazyEnumerable = 1 << 6,
            EagerCachingSingletonForFasterAccess = 1 << 7,
            ImplicitRootOpenScope = 1 << 8,
            ThrowIfRuntimeStateRequired = 1 << 9,
            CaptureContainerDisposeStackTrace = 1 << 10
        }

        private const Settings DEFAULT_SETTINGS
            = Settings.ThrowIfDependencyHasShorterReuseLifespan
              | Settings.ThrowOnRegisteringDisposableTransient
              | Settings.ImplicitCheckForReuseMatchingScope
              | Settings.VariantGenericTypesInResolvedCollection
              | Settings.EagerCachingSingletonForFasterAccess;

        private Settings _settings;

        #endregion
    }

    /// <summary>Wraps constructor or factory method optionally with factory instance to create service.</summary>
    internal sealed class FactoryMethod
    {
        /// <summary>Constructor or method to use for service creation.</summary>
        public readonly MemberInfo ConstructorOrMethodOrMember;

        /// <summary>Identifies factory service if factory method is instance member.</summary>
        public readonly ServiceInfo FactoryServiceInfo;

        /// <summary>Wraps method and factory instance.</summary>
        /// <param name="ctorOrMethodOrMember">Constructor, static or instance method, property or field.</param>
        /// <param name="factoryInfo">Factory info to resolve in case of instance <paramref name="ctorOrMethodOrMember"/>.</param>
        /// <returns>New factory method wrapper.</returns>
        public static FactoryMethod Of(MemberInfo ctorOrMethodOrMember, ServiceInfo factoryInfo = null)
        {
            return new FactoryMethod(ctorOrMethodOrMember, factoryInfo);
        }

        /// <summary>Discovers the static factory method or member by name in <typeparamref name="TFactory"/>.
        /// Should play nice with C# <c>nameof</c> operator.</summary>
        /// <param name="methodOrMemberName">Name or method or member.</param>
        /// <typeparam name="TFactory">Class with static member.</typeparam>
        /// <returns>Factory method info.</returns>
        public static FactoryMethod Of<TFactory>(string methodOrMemberName)
        {
            var methodOrMember = typeof(TFactory).GetAllMembers()
                .SingleOrDefault(m => m.Name == methodOrMemberName)
                .ThrowIfNull();
            return Of(methodOrMember);
        }

        /// <summary>Pretty prints wrapped method.</summary> <returns>Printed string.</returns>
        public override string ToString()
        {
            return new StringBuilder().Print(ConstructorOrMethodOrMember.DeclaringType)
                .Append("::").Append(ConstructorOrMethodOrMember).ToString();
        }

        /// <summary>Easy way to specify non-public and most resolvable constructor.</summary>
        /// <param name="mostResolvable">(optional) Instructs to select constructor with max number of params which all are resolvable.</param> 
        /// <param name="includeNonPublic">(optional) Consider the non-public constructors.</param>
        /// <returns>Constructor or null if not found.</returns>
        public static FactoryMethodSelector Constructor(bool mostResolvable = false, bool includeNonPublic = false)
        {
            return request =>
            {
                var implType = request.ImplementationType
                    .ThrowIfNull(Error.ImplTypeIsNotSpecifiedForAutoCtorSelection, request);

                var ctors = implType.GetAllConstructors(includeNonPublic).ToArrayOrSelf();
                if (ctors.Length == 0)
                    return null;

                // if there is only one constructor then use it
                if (ctors.Length == 1)
                    return Of(ctors[0]);

                // stop here for non-auto selection
                if (!mostResolvable)
                    return null;

                var ctorsWithMaxParamsFirst = ctors
                    .Select(c => new { Ctor = c, Params = c.GetParameters() })
                    .OrderByDescending(x => x.Params.Length);

                var containerRules = request.Rules;
                var selector = containerRules.OverrideRegistrationMade
                    ? request.Made.Parameters.OverrideWith(containerRules.Parameters)
                    : containerRules.Parameters.OverrideWith(request.Made.Parameters);

                var parameterSelector = selector(request);

                if (!request.IsWrappedInFuncWithArgs(immediateParent: true))
                {
                    var matchedCtor = ctorsWithMaxParamsFirst.FirstOrDefault(x =>
                        x.Params.All(p => IsResolvableParameter(p, parameterSelector, request)));
                    var ctor = matchedCtor.ThrowIfNull(Error.UnableToFindCtorWithAllResolvableArgs, request).Ctor;
                    return Of(ctor);
                }
                else
                {
                    // For Func with arguments the constructor should contain all input arguments and
                    // the rest should be resolvable.
                    var funcType = !request.RawParent.IsEmpty
                        ? request.RawParent.ServiceType
                        : request.PreResolveParent.ServiceType;

                    var funcArgs = funcType.GetGenericParamsAndArgs();
                    var inputArgCount = funcArgs.Length - 1;

                    var matchedCtor = ctorsWithMaxParamsFirst
                        .Where(x => x.Params.Length >= inputArgCount)
                        .FirstOrDefault(x =>
                        {
                            var matchedIndecesMask = 0;
                            return x.Params.Except(
                                x.Params.Where(p =>
                                {
                                    var inputArgIndex = funcArgs.IndexOf(p.ParameterType);
                                    if (inputArgIndex == -1 || inputArgIndex == inputArgCount ||
                                        (matchedIndecesMask & inputArgIndex << 1) != 0)
                                        // input argument was already matched by another parameter
                                        return false;
                                    matchedIndecesMask |= inputArgIndex << 1;
                                    return true;
                                })).All(p => IsResolvableParameter(p, parameterSelector, request));
                        });

                    var ctor = matchedCtor.ThrowIfNull(Error.UnableToFindMatchingCtorForFuncWithArgs, funcType, request).Ctor;
                    return Of(ctor);
                }
            };
        }

        /// <summary>Easy way to specify non-public or / and most resolvable constructor.</summary>
        /// <param name="includeNonPublic">(optional) Consider the non-public constructors.</param>
        /// <returns>Constructor or null if not found.</returns>
        public static FactoryMethodSelector DefaultConstructor(bool includeNonPublic = false)
        {
            return request =>
            {
                var implType = request.ImplementationType
                    .ThrowIfNull(Error.ImplTypeIsNotSpecifiedForAutoCtorSelection, request);
                var defaultCtor = implType.GetConstructorOrNull(includeNonPublic, args: ArrayTools.Empty<Type>());
                return defaultCtor != null ? Of(defaultCtor) : null;
            };
        }

        /// <summary>Searches for public constructor with most resolvable parameters or throws <see cref="ContainerException"/> if not found.
        /// Works both for resolving service and Func{TArgs..., TService}</summary>
        public static readonly FactoryMethodSelector ConstructorWithResolvableArguments =
            Constructor(mostResolvable: true);

        /// <summary>Searches for constructor (including non public ones) with most 
        /// resolvable parameters or throws <see cref="ContainerException"/> if not found.
        /// Works both for resolving service and Func{TArgs..., TService}</summary>
        public static readonly FactoryMethodSelector ConstructorWithResolvableArgumentsIncludingNonPublic =
            Constructor(mostResolvable: true, includeNonPublic: true);

        /// <summary>Checks that parameter is selected on requested path and with provided parameter selector.</summary>
        /// <param name="parameter"></param> <param name="parameterSelector"></param> <param name="request"></param>
        /// <returns>True if parameter is resolvable.</returns>
        public static bool IsResolvableParameter(ParameterInfo parameter,
            Func<ParameterInfo, ParameterServiceInfo> parameterSelector, Request request)
        {
            var parameterServiceInfo = parameterSelector(parameter) ?? ParameterServiceInfo.Of(parameter);
            var parameterRequest = request.Push(parameterServiceInfo.WithDetails(ServiceDetails.IfUnresolvedReturnDefault, null));
            if (parameterServiceInfo.Details.HasCustomValue)
            {
                var customValue = parameterServiceInfo.Details.CustomValue;
                return customValue == null || customValue.GetType().IsAssignableTo(parameterRequest.GetActualServiceType());
            }

            var factory = parameterRequest.Container.ResolveFactory(parameterRequest);
            return factory != null && factory.GetExpressionOrDefault(parameterRequest) != null;
        }

        private FactoryMethod(MemberInfo constructorOrMethodOrMember, ServiceInfo factoryServiceInfo = null)
        {
            ConstructorOrMethodOrMember = constructorOrMethodOrMember;
            FactoryServiceInfo = factoryServiceInfo;
        }
    }

    /// <summary>Rules how to: <list type="bullet">
    /// <item>Select constructor for creating service with <see cref="FactoryMethod"/>.</item>
    /// <item>Specify how to resolve constructor parameters with <see cref="Parameters"/>.</item>
    /// <item>Specify what properties/fields to resolve and how with <see cref="PropertiesAndFields"/>.</item>
    /// </list></summary>
    internal class Made
    {
        /// <summary>Returns delegate to select constructor based on provided request.</summary>
        public FactoryMethodSelector FactoryMethod { get; private set; }

        /// <summary>Return type of strongly-typed factory method expression.</summary>
        public Type FactoryMethodKnownResultType { get; private set; }

        /// <summary>True is made has properties or parameters with custom value.
        /// That's mean the whole made become context based which affects caching</summary>
        public bool HasCustomDependencyValue { get; private set; }

        /// <summary>Specifies how constructor parameters should be resolved:
        /// parameter service key and type, throw or return default value if parameter is unresolved.</summary>
        public ParameterSelector Parameters { get; private set; }

        /// <summary>Specifies what <see cref="ServiceInfo"/> should be used when resolving property or field.</summary>
        public PropertiesAndFieldsSelector PropertiesAndFields { get; private set; }

        /// <summary>Container will use some sensible defaults for service creation.</summary>
        public static readonly Made Default = new Made();

        /// <summary>Creates rules with only <see cref="FactoryMethod"/> specified.</summary>
        /// <param name="factoryMethod">To use.</param> <returns>New rules.</returns>
        public static implicit operator Made(FactoryMethodSelector factoryMethod)
        {
            return Of(factoryMethod);
        }

        /// <summary>Creates rules with only <see cref="Parameters"/> specified.</summary>
        /// <param name="parameters">To use.</param> <returns>New rules.</returns>
        public static implicit operator Made(ParameterSelector parameters)
        {
            return Of(parameters: parameters);
        }

        /// <summary>Creates rules with only <see cref="PropertiesAndFields"/> specified.</summary>
        /// <param name="propertiesAndFields">To use.</param> <returns>New rules.</returns>
        public static implicit operator Made(PropertiesAndFieldsSelector propertiesAndFields)
        {
            return Of(propertiesAndFields: propertiesAndFields);
        }

        /// <summary>Specifies injections rules for Constructor, Parameters, Properties and Fields. If no rules specified returns <see cref="Default"/> rules.</summary>
        /// <param name="factoryMethod">(optional)</param> <param name="parameters">(optional)</param> <param name="propertiesAndFields">(optional)</param>
        /// <returns>New injection rules or <see cref="Default"/>.</returns>
        public static Made Of(FactoryMethodSelector factoryMethod = null,
            ParameterSelector parameters = null, PropertiesAndFieldsSelector propertiesAndFields = null)
        {
            return factoryMethod == null && parameters == null && propertiesAndFields == null
                ? Default : new Made(factoryMethod, parameters, propertiesAndFields);
        }

        /// <summary>Specifies injections rules for Constructor, Parameters, Properties and Fields. If no rules specified returns <see cref="Default"/> rules.</summary>
        /// <param name="factoryMethod">Known factory method.</param>
        /// <param name="parameters">(optional)</param> <param name="propertiesAndFields">(optional)</param>
        /// <returns>New injection rules.</returns>
        public static Made Of(FactoryMethod factoryMethod,
            ParameterSelector parameters = null, PropertiesAndFieldsSelector propertiesAndFields = null)
        {
            var methodReturnType = factoryMethod.ConstructorOrMethodOrMember.GetReturnTypeOrDefault();

            // Normalizes open-generic type to open-generic definition,
            // because for base classes and return types it may not be the case.
            if (methodReturnType != null && methodReturnType.IsOpenGeneric())
                methodReturnType = methodReturnType.GetGenericTypeDefinition();

            return new Made(_ => factoryMethod, parameters, propertiesAndFields, methodReturnType);
        }

        /// <summary>Creates rules with only <see cref="FactoryMethod"/> specified.</summary>
        /// <param name="factoryMethodOrMember">To create service.</param>
        /// <param name="factoryInfo">(optional) Factory info to resolve in case of instance member.</param>
        /// <param name="parameters">(optional)</param> <param name="propertiesAndFields">(optional)</param>
        /// <returns>New rules.</returns>
        public static Made Of(MemberInfo factoryMethodOrMember, ServiceInfo factoryInfo = null,
            ParameterSelector parameters = null, PropertiesAndFieldsSelector propertiesAndFields = null)
        {
            return Of(DryIoc.FactoryMethod.Of(factoryMethodOrMember, factoryInfo), parameters, propertiesAndFields);
        }

        /// <summary>Creates factory specification with method or member selector based on request.</summary>
        /// <param name="getMethodOrMember">Method, or constructor, or member selector.</param>
        /// <param name="factoryInfo">(optional) Factory info to resolve in case of instance method/member.</param>
        /// <param name="parameters">(optional)</param> <param name="propertiesAndFields">(optional)</param>
        /// <returns>New specification.</returns>
        public static Made Of(Func<Request, MemberInfo> getMethodOrMember, ServiceInfo factoryInfo = null,
            ParameterSelector parameters = null, PropertiesAndFieldsSelector propertiesAndFields = null)
        {
            return Of(r => DryIoc.FactoryMethod.Of(getMethodOrMember(r), factoryInfo), parameters, propertiesAndFields);
        }

        /// <summary>Defines how to select constructor from implementation type.</summary>
        /// <param name="getConstructor">Delegate taking implementation type as input and returning selected constructor info.</param>
        /// <param name="parameters">(optional)</param> <param name="propertiesAndFields">(optional)</param>
        /// <returns>New instance of <see cref="Made"/> with <see cref="FactoryMethod"/> set to specified delegate.</returns>
        public static Made Of(Func<Type, ConstructorInfo> getConstructor, ParameterSelector parameters = null,
            PropertiesAndFieldsSelector propertiesAndFields = null)
        {
            return Of(r => DryIoc.FactoryMethod.Of(getConstructor(r.ImplementationType)
                    .ThrowIfNull(Error.GotNullConstructorFromFactoryMethod, r)),
                parameters, propertiesAndFields);
        }

        /// <summary>Defines factory method using expression of constructor call (with properties), or static method call.</summary>
        /// <typeparam name="TService">Type with constructor or static method.</typeparam>
        /// <param name="serviceReturningExpr">Expression tree with call to constructor with properties:
        /// <code lang="cs"><![CDATA[() => new Car(Arg.Of<IEngine>()) { Color = Arg.Of<Color>("CarColor") }]]></code>
        /// or static method call <code lang="cs"><![CDATA[() => Car.Create(Arg.Of<IEngine>())]]></code></param>
        /// <param name="argValues">(optional) Primitive custom values for dependencies.</param>
        /// <returns>New Made specification.</returns>
        public static TypedMade<TService> Of<TService>(
            Expression<Func<TService>> serviceReturningExpr,
            params Func<RequestInfo, object>[] argValues)
        {
            return FromExpression<TService>(null, serviceReturningExpr, argValues);
        }

        /// <summary>Defines creation info from factory method call Expression without using strings.
        /// You can supply any/default arguments to factory method, they won't be used, it is only to find the <see cref="MethodInfo"/>.</summary>
        /// <typeparam name="TFactory">Factory type.</typeparam> <typeparam name="TService">Factory product type.</typeparam>
        /// <param name="getFactoryInfo">Returns or resolves factory instance.</param>
        /// <param name="serviceReturningExpr">Method, property or field expression returning service.</param>
        /// <param name="argValues">(optional) Primitive custom values for dependencies.</param>
        /// <returns>New Made specification.</returns>
        public static TypedMade<TService> Of<TFactory, TService>(
            Func<Request, ServiceInfo.Typed<TFactory>> getFactoryInfo,
            Expression<Func<TFactory, TService>> serviceReturningExpr,
            params Func<RequestInfo, object>[] argValues)
            where TFactory : class
        {
            getFactoryInfo.ThrowIfNull();
            // NOTE: cannot convert to method group because of lack of covariance support in .Net 3.5
            return FromExpression<TService>(r => getFactoryInfo(r).ThrowIfNull(), serviceReturningExpr, argValues);
        }

        private static TypedMade<TService> FromExpression<TService>(
            Func<Request, ServiceInfo> getFactoryInfo,
            LambdaExpression serviceReturningExpr,
            params Func<RequestInfo, object>[] argValues)
        {
            var callExpr = serviceReturningExpr.ThrowIfNull().Body;
            if (callExpr.NodeType == ExpressionType.Convert) // proceed without Cast expression.
                return FromExpression<TService>(getFactoryInfo,
                    Expression.Lambda(((UnaryExpression)callExpr).Operand, ArrayTools.Empty<ParameterExpression>()),
                    argValues);

            MemberInfo ctorOrMethodOrMember;
            IList<Expression> argExprs = null;
            IList<MemberBinding> memberBindingExprs = null;
            ParameterInfo[] parameters = null;

            if (callExpr.NodeType == ExpressionType.New || callExpr.NodeType == ExpressionType.MemberInit)
            {
                var newExpr = callExpr as NewExpression ?? ((MemberInitExpression)callExpr).NewExpression;
                ctorOrMethodOrMember = newExpr.Constructor;
                parameters = newExpr.Constructor.GetParameters();
                argExprs = newExpr.Arguments;
                if (callExpr is MemberInitExpression)
                    memberBindingExprs = ((MemberInitExpression)callExpr).Bindings;
            }
            else if (callExpr.NodeType == ExpressionType.Call)
            {
                var methodCallExpr = (MethodCallExpression)callExpr;
                ctorOrMethodOrMember = methodCallExpr.Method;
                parameters = methodCallExpr.Method.GetParameters();
                argExprs = methodCallExpr.Arguments;
            }
            else if (callExpr.NodeType == ExpressionType.Invoke)
            {
                var invokeExpr = (InvocationExpression)callExpr;
                var invokedDelegateExpr = invokeExpr.Expression;
                var invokeMethod = invokedDelegateExpr.Type.GetSingleMethodOrNull("Invoke");
                ctorOrMethodOrMember = invokeMethod;
                parameters = invokeMethod.GetParameters();
                argExprs = invokeExpr.Arguments;
            }

            else if (callExpr.NodeType == ExpressionType.MemberAccess)
            {
                var member = ((MemberExpression)callExpr).Member;
                Throw.If(!(member is PropertyInfo) && !(member is FieldInfo),
                    Error.UnexpectedFactoryMemberExpression, member);
                ctorOrMethodOrMember = member;
            }
            else return Throw.For<TypedMade<TService>>(Error.NotSupportedMadeExpression, callExpr);

            FactoryMethodSelector factoryMethod = request =>
                DryIoc.FactoryMethod.Of(ctorOrMethodOrMember, getFactoryInfo == null ? null : getFactoryInfo(request));

            var hasCustomValue = false;

            var parameterSelector = parameters.IsNullOrEmpty() ? null
                : ComposeParameterSelectorFromArgs(ref hasCustomValue, parameters, argExprs, argValues);

            var propertiesAndFieldsSelector =
                memberBindingExprs == null || memberBindingExprs.Count == 0 ? null
                    : ComposePropertiesAndFieldsSelector(ref hasCustomValue, memberBindingExprs, argValues);

            return new TypedMade<TService>(factoryMethod, parameterSelector, propertiesAndFieldsSelector, hasCustomValue);
        }

        /// <summary>Typed version of <see cref="Made"/> specified with statically typed expression tree.</summary>
        /// <typeparam name="TService">Type that expression returns.</typeparam>
        internal sealed class TypedMade<TService> : Made
        {
            /// <summary>Creates typed version.</summary>
            /// <param name="factoryMethod"></param> <param name="parameters"></param> <param name="propertiesAndFields"></param>
            /// <param name="hasCustomValue"></param>
            internal TypedMade(FactoryMethodSelector factoryMethod = null,
                ParameterSelector parameters = null, PropertiesAndFieldsSelector propertiesAndFields = null,
                bool hasCustomValue = false)
                : base(factoryMethod, parameters, propertiesAndFields, typeof(TService), hasCustomValue)
            { }
        }

        #region Implementation

        private Made(FactoryMethodSelector factoryMethod = null, ParameterSelector parameters = null, PropertiesAndFieldsSelector propertiesAndFields = null,
            Type factoryMethodKnownResultType = null, bool hasCustomValue = false)
        {
            FactoryMethod = factoryMethod;
            Parameters = parameters;
            PropertiesAndFields = propertiesAndFields;
            FactoryMethodKnownResultType = factoryMethodKnownResultType;
            HasCustomDependencyValue = hasCustomValue;
        }

        private static ParameterSelector ComposeParameterSelectorFromArgs(ref bool hasCustomValue,
            ParameterInfo[] parameterInfos, IList<Expression> argExprs, params Func<RequestInfo, object>[] argValues)
        {
            var parameters = DryIoc.Parameters.Of;
            for (var i = 0; i < argExprs.Count; i++)
            {
                var parameter = parameterInfos[i];
                var methodCallExpr = argExprs[i] as MethodCallExpression;
                if (methodCallExpr != null)
                {
                    Throw.If(methodCallExpr.Method.DeclaringType != typeof(Arg),
                        Error.UnexpectedExpressionInsteadOfArgMethod, methodCallExpr);

                    if (methodCallExpr.Method.Name == Arg.ArgIndexMethodName)
                    {
                        var getArgValue = GetArgCustomValueProvider(methodCallExpr, argValues);
                        parameters = parameters.Details((r, p) => p.Equals(parameter)
                            ? ServiceDetails.Of(getArgValue(r.RequestInfo))
                            : null);
                        hasCustomValue = true;
                    }
                    else // handle service details
                    {
                        var defaultValue = parameter.IsOptional ? parameter.DefaultValue : null;
                        var argDetails = GetArgServiceDetails(methodCallExpr, parameter.ParameterType, IfUnresolved.Throw, defaultValue);
                        parameters = parameters.Details((r, p) => p.Equals(parameter) ? argDetails : null);
                    }
                }
                else
                {
                    var customValue = GetArgExpressionValueOrThrow(argExprs[i]);
                    parameters = parameters.Details((r, p) => p.Equals(parameter) ? ServiceDetails.Of(customValue) : null);
                }
            }
            return parameters;
        }

        private static PropertiesAndFieldsSelector ComposePropertiesAndFieldsSelector(ref bool hasCustomValue,
            IList<MemberBinding> memberBindings, params Func<RequestInfo, object>[] argValues)
        {
            var propertiesAndFields = DryIoc.PropertiesAndFields.Of;
            for (var i = 0; i < memberBindings.Count; i++)
            {
                var memberAssignment = (memberBindings[i] as MemberAssignment).ThrowIfNull();
                var member = memberAssignment.Member;

                var methodCallExpr = memberAssignment.Expression as MethodCallExpression;
                if (methodCallExpr == null) // not an Arg.Of: e.g. constant or variable
                {
                    var customValue = GetArgExpressionValueOrThrow(memberAssignment.Expression);
                    propertiesAndFields = propertiesAndFields.OverrideWith(r => new[]
                    {
                        PropertyOrFieldServiceInfo.Of(member).WithDetails(
                            ServiceDetails.Of(customValue), r)
                    });
                }
                else
                {
                    Throw.If(methodCallExpr.Method.DeclaringType != typeof(Arg),
                        Error.UnexpectedExpressionInsteadOfArgMethod, methodCallExpr);

                    if (methodCallExpr.Method.Name == Arg.ArgIndexMethodName) // handle custom value
                    {
                        var getArgValue = GetArgCustomValueProvider(methodCallExpr, argValues);
                        propertiesAndFields = propertiesAndFields.OverrideWith(r => new[]
                        {
                            PropertyOrFieldServiceInfo.Of(member).WithDetails(
                                ServiceDetails.Of(getArgValue(r.RequestInfo)), r)
                        });
                        hasCustomValue = true;
                    }
                    else
                    {
                        var memberType = member.GetReturnTypeOrDefault();
                        var argServiceDetails = GetArgServiceDetails(methodCallExpr, memberType, IfUnresolved.ReturnDefault, null);
                        propertiesAndFields = propertiesAndFields.OverrideWith(r => new[]
                        {
                            PropertyOrFieldServiceInfo.Of(member).WithDetails(argServiceDetails, r)
                        });
                    }
                }
            }
            return propertiesAndFields;
        }

        private static Func<RequestInfo, object> GetArgCustomValueProvider(MethodCallExpression methodCallExpr, Func<RequestInfo, object>[] argValues)
        {
            Throw.If(argValues.IsNullOrEmpty(), Error.ArgValueIndexIsProvidedButNoArgValues);

            var argIndexExpr = methodCallExpr.Arguments[0];
            var argIndex = (int)GetArgExpressionValueOrThrow(argIndexExpr);

            Throw.If(argIndex < 0 || argIndex >= argValues.Length,
                Error.ArgValueIndexIsOutOfProvidedArgValues, argIndex, argValues);

            var getArgValue = argValues[argIndex];
            return getArgValue;
        }

        private static ServiceDetails GetArgServiceDetails(MethodCallExpression methodCallExpr,
            Type dependencyType, IfUnresolved defaultIfUnresolved, object defaultValue)
        {
            var requiredServiceType = methodCallExpr.Method.GetGenericArguments().Last();
            if (requiredServiceType == dependencyType)
                requiredServiceType = null;

            var serviceKey = default(object);
            var metadataKey = default(string);
            var metadata = default(object);
            var ifUnresolved = defaultIfUnresolved;

            var hasPrevArg = false;

            var argExprs = methodCallExpr.Arguments;
            if (argExprs.Count == 2 &&
                argExprs[0].Type == typeof(string) &&
                argExprs[1].Type != typeof(IfUnresolved)) // matches the Of overload for metadata
            {
                metadataKey = (string)GetArgExpressionValueOrThrow(argExprs[0]);
                metadata = GetArgExpressionValueOrThrow(argExprs[1]);
            }
            else
            {
                for (var a = 0; a < argExprs.Count; a++)
                {
                    var argValue = GetArgExpressionValueOrThrow(argExprs[a]);
                    if (argValue != null)
                    {
                        if (argValue is IfUnresolved)
                        {
                            ifUnresolved = (IfUnresolved)argValue;
                            if (hasPrevArg) // the only possible argument is default value.
                            {
                                defaultValue = serviceKey;
                                serviceKey = null;
                            }
                        }
                        else
                        {
                            serviceKey = argValue;
                            hasPrevArg = true;
                        }
                    }
                }
            }

            return ServiceDetails.Of(requiredServiceType, serviceKey, ifUnresolved, defaultValue, metadataKey, metadata);
        }

        private static object GetArgExpressionValueOrThrow(Expression arg)
        {
            var valueExpr = arg as ConstantExpression;
            if (valueExpr != null)
                return valueExpr.Value;

            var convert = arg as UnaryExpression; // e.g. (object)SomeEnum.Value
            if (convert != null && convert.NodeType == ExpressionType.Convert)
                return GetArgExpressionValueOrThrow(convert.Operand as ConstantExpression);

            var member = arg as MemberExpression;
            if (member != null)
            {
                var memberOwner = member.Expression as ConstantExpression;
                if (memberOwner != null && memberOwner.Type.IsClosureType())
                {
                    var memberField = member.Member as FieldInfo;
                    if (memberField != null)
                        return memberField.GetValue(memberOwner.Value);
                }
            }

            return Throw.For<object>(Error.UnexpectedExpressionInsteadOfConstant, arg);
        }

        #endregion
    }

    /// <summary>Class for defining parameters/properties/fields service info in <see cref="Made"/> expressions.
    /// Its methods are NOT actually called, they just used to reflect service info from call expression.</summary>
    internal static class Arg
    {
        /// <summary>Specifies required service type of parameter or member. If required type is the same as parameter/member type,
        /// the method is just a placeholder to help detect constructor or factory method, and does not have additional meaning.</summary>
        /// <typeparam name="TRequired">Required service type if different from parameter/member type.</typeparam>
        /// <returns>Returns some (ignored) value.</returns>
        public static TRequired Of<TRequired>() { return default(TRequired); }

        /// <summary>Specifies both service and required service types.</summary>
        /// <typeparam name="TService">Service type.</typeparam> <typeparam name="TRequired">Required service type.</typeparam>
        /// <returns>Ignored default of Service type.</returns>
        public static TService Of<TService, TRequired>() { return default(TService); }

        /// <summary>Specifies required service type of parameter or member. Plus specifies if-unresolved policy.</summary>
        /// <typeparam name="TRequired">Required service type if different from parameter/member type.</typeparam>
        /// <param name="ifUnresolved">Defines to throw or to return default if unresolved.</param>
        /// <returns>Returns some (ignored) value.</returns>
        public static TRequired Of<TRequired>(IfUnresolved ifUnresolved) { return default(TRequired); }

        /// <summary>Specifies both service and required service types.</summary>
        /// <typeparam name="TService">Service type.</typeparam> <typeparam name="TRequired">Required service type.</typeparam>
        /// <param name="ifUnresolved">Defines to throw or to return default if unresolved.</param>
        /// <returns>Ignored default of Service type.</returns>
        public static TService Of<TService, TRequired>(IfUnresolved ifUnresolved) { return default(TService); }

        /// <summary>Specifies required service type of parameter or member. Plus specifies service key.</summary>
        /// <typeparam name="TRequired">Required service type if different from parameter/member type.</typeparam>
        /// <param name="serviceKey">Service key object.</param>
        /// <returns>Returns some (ignored) value.</returns>
        public static TRequired Of<TRequired>(object serviceKey) { return default(TRequired); }

        /// <summary>Specifies both service and required service types.</summary>
        /// <typeparam name="TService">Service type.</typeparam> <typeparam name="TRequired">Required service type.</typeparam>
        /// <param name="serviceKey">Service key object.</param>
        /// <returns>Ignored default of Service type.</returns>
        public static TService Of<TService, TRequired>(object serviceKey) { return default(TService); }

        /// <summary>Specifies required service type of parameter or member. Plus specifies service key.</summary>
        /// <typeparam name="TRequired">Required service type if different from parameter/member type.</typeparam>
        /// <param name="metadataKey">Required metadata key</param> <param name="metadata">Required metadata or value.</param>
        /// <returns>Returns some (ignored) value.</returns>
        public static TRequired Of<TRequired>(string metadataKey, object metadata) { return default(TRequired); }

        /// <summary>Specifies both service and required service types.</summary>
        /// <typeparam name="TService">Service type.</typeparam> <typeparam name="TRequired">Required service type.</typeparam>
        /// <param name="metadataKey">Required metadata key</param> <param name="metadata">Required metadata or value.</param>
        /// <returns>Ignored default of Service type.</returns>
        public static TService Of<TService, TRequired>(string metadataKey, object metadata) { return default(TService); }

        /// <summary>Specifies required service type of parameter or member. Plus specifies if-unresolved policy. Plus specifies service key.</summary>
        /// <typeparam name="TRequired">Required service type if different from parameter/member type.</typeparam>
        /// <param name="ifUnresolved">Defines to throw or to return default if unresolved.</param>
        /// <param name="serviceKey">Service key object.</param>
        /// <returns>Returns some (ignored) value.</returns>
        public static TRequired Of<TRequired>(IfUnresolved ifUnresolved, object serviceKey) { return default(TRequired); }

        /// <summary>Specifies both service and required service types.</summary>
        /// <typeparam name="TService">Service type.</typeparam> <typeparam name="TRequired">Required service type.</typeparam>
        /// <param name="ifUnresolved">Defines to throw or to return default if unresolved.</param>
        /// <param name="serviceKey">Service key object.</param>
        /// <returns>Ignored default of Service type.</returns>
        public static TService Of<TService, TRequired>(IfUnresolved ifUnresolved, object serviceKey) { return default(TService); }

        /// <summary>Specifies required service type, default value and <see cref="IfUnresolved.ReturnDefault"/>.</summary>
        /// <typeparam name="TRequired">Required service type.</typeparam>
        /// <param name="defaultValue">Primitive default value.</param>
        /// <param name="ifUnresolved">Only the <see cref="IfUnresolved.ReturnDefault"/> value is accepted.</param>
        /// <returns>Ignored.</returns>
        public static TRequired Of<TRequired>(TRequired defaultValue, IfUnresolved ifUnresolved) { return default(TRequired); }

        /// <summary>Specifies required service type, default value and <see cref="IfUnresolved.ReturnDefault"/>.</summary>
        /// <typeparam name="TRequired">Required service type.</typeparam>
        /// <param name="defaultValue">Primitive default value.</param>
        /// <param name="ifUnresolved">Only the <see cref="IfUnresolved.ReturnDefault"/> value is accepted.</param>
        /// <param name="serviceKey">Service key object.</param>
        /// <returns>Ignored.</returns>
        public static TRequired Of<TRequired>(TRequired defaultValue, IfUnresolved ifUnresolved, object serviceKey) { return default(TRequired); }

        /// <summary>Specifies argument index starting from 0 to use corresponding custom value factory,
        /// similar to String.Format <c>"{0}, {1}, etc"</c>.</summary>
        /// <typeparam name="T">Type of dependency. Difference from actual parameter type is ignored.</typeparam>
        /// <param name="argIndex">Argument index starting from 0</param> <returns>Ignored.</returns>
        public static T Index<T>(int argIndex) { return default(T); }

        /// <summary>Name is close to method itself to not forget when renaming the method.</summary>
        public static string ArgIndexMethodName = "Index";
    }

    /// <summary>Contains <see cref="IRegistrator"/> extension methods to simplify general use cases.</summary>
    internal static class Registrator
    {
        /// <summary>Registers service of <paramref name="serviceType"/>.</summary>
        /// <param name="registrator">Any <see cref="IRegistrator"/> implementation, e.g. <see cref="Container"/>.</param>
        /// <param name="serviceType">The service type to register</param>
        /// <param name="factory"><see cref="Factory"/> details object.</param>
        /// <param name="ifAlreadyRegistered">(optional) policy to deal with case when service with such type and name is already registered.</param>
        /// <param name="serviceKey">(optional Could be of any type with overridden <see cref="object.GetHashCode"/> and <see cref="object.Equals(object)"/>.</param>
        public static void Register(this IRegistrator registrator, Type serviceType, Factory factory,
            IfAlreadyRegistered ifAlreadyRegistered = IfAlreadyRegistered.AppendNotKeyed,
            object serviceKey = null)
        {
            registrator.Register(factory, serviceType, serviceKey, ifAlreadyRegistered, false);
        }

        /// <summary>Registers service <paramref name="serviceType"/> with corresponding <paramref name="implementationType"/>.</summary>
        /// <param name="registrator">Any <see cref="IRegistrator"/> implementation, e.g. <see cref="Container"/>.</param>
        /// <param name="serviceType">The service type to register.</param>
        /// <param name="implementationType">Implementation type. Concrete and open-generic class are supported.</param>
        /// <param name="reuse">(optional) <see cref="IReuse"/> implementation, e.g. <see cref="Reuse.Singleton"/>.
        ///     Default value means no reuse, aka Transient.</param>
        /// <param name="made">(optional) specifies <see cref="Made"/>.</param>
        /// <param name="setup">(optional) Factory setup, by default is (<see cref="Setup.Default"/>)</param>
        /// <param name="ifAlreadyRegistered">(optional) Policy to deal with case when service with such type and name is already registered.</param>
        /// <param name="serviceKey">(optional) Could be of any of type with overridden <see cref="object.GetHashCode"/> and <see cref="object.Equals(object)"/>.</param>
        public static void Register(this IRegistrator registrator, Type serviceType, Type implementationType,
            IReuse reuse = null, Made made = null, Setup setup = null,
            IfAlreadyRegistered ifAlreadyRegistered = IfAlreadyRegistered.AppendNotKeyed,
            object serviceKey = null)
        {
            var factory = new ReflectionFactory(implementationType, reuse, made, setup);
            registrator.Register(factory, serviceType, serviceKey, ifAlreadyRegistered, false);
        }

        /// <summary>Registers service of <paramref name="serviceAndMayBeImplementationType"/>. ServiceType will be the same as <paramref name="serviceAndMayBeImplementationType"/>.</summary>
        /// <param name="registrator">Any <see cref="IRegistrator"/> implementation, e.g. <see cref="Container"/>.</param>
        /// <param name="serviceAndMayBeImplementationType">Implementation type. Concrete and open-generic class are supported.</param>
        /// <param name="reuse">(optional) <see cref="IReuse"/> implementation, e.g. <see cref="Reuse.Singleton"/>. Default value means no reuse, aka Transient.</param>
        /// <param name="made">(optional) specifies <see cref="Made"/>.</param>
        /// <param name="setup">(optional) factory setup, by default is (<see cref="Setup.Default"/>)</param>
        /// <param name="ifAlreadyRegistered">(optional) policy to deal with case when service with such type and name is already registered.</param>
        /// <param name="serviceKey">(optional) Could be of any of type with overridden <see cref="object.GetHashCode"/> and <see cref="object.Equals(object)"/>.</param>
        public static void Register(this IRegistrator registrator, Type serviceAndMayBeImplementationType,
            IReuse reuse = null, Made made = null, Setup setup = null,
            IfAlreadyRegistered ifAlreadyRegistered = IfAlreadyRegistered.AppendNotKeyed,
            object serviceKey = null)
        {
            var factory = new ReflectionFactory(serviceAndMayBeImplementationType, reuse, made, setup);
            registrator.Register(factory, serviceAndMayBeImplementationType, serviceKey, ifAlreadyRegistered, false);
        }

        /// <summary>Registers service of <typeparamref name="TService"/> type implemented by <typeparamref name="TImplementation"/> type.</summary>
        /// <typeparam name="TService">The type of service.</typeparam>
        /// <typeparam name="TImplementation">The type of service.</typeparam>
        /// <param name="registrator">Any <see cref="IRegistrator"/> implementation, e.g. <see cref="Container"/>.</param>
        /// <param name="reuse">(optional) <see cref="IReuse"/> implementation, e.g. <see cref="Reuse.Singleton"/>. Default value means no reuse, aka Transient.</param>
        /// <param name="made">(optional) specifies <see cref="Made"/>.</param>
        /// <param name="setup">(optional) factory setup, by default is (<see cref="Setup.Default"/>)</param>
        /// <param name="ifAlreadyRegistered">(optional) policy to deal with case when service with such type and name is already registered.</param>
        /// <param name="serviceKey">(optional) Could be of any of type with overridden <see cref="object.GetHashCode"/> and <see cref="object.Equals(object)"/>.</param>
        public static void Register<TService, TImplementation>(this IRegistrator registrator,
            IReuse reuse = null, Made made = null, Setup setup = null,
            IfAlreadyRegistered ifAlreadyRegistered = IfAlreadyRegistered.AppendNotKeyed,
            object serviceKey = null)
            where TImplementation : TService
        {
            var factory = new ReflectionFactory(typeof(TImplementation), reuse, made, setup);
            registrator.Register(factory, typeof(TService), serviceKey, ifAlreadyRegistered, isStaticallyChecked: true);
        }

        /// <summary>Registers implementation type <typeparamref name="TImplementation"/> with itself as service type.</summary>
        /// <typeparam name="TImplementation">The type of service.</typeparam>
        /// <param name="registrator">Any <see cref="IRegistrator"/> implementation, e.g. <see cref="Container"/>.</param>
        /// <param name="reuse">(optional) <see cref="IReuse"/> implementation, e.g. <see cref="Reuse.Singleton"/>. Default value means no reuse, aka Transient.</param>
        /// <param name="made">(optional) specifies <see cref="Made"/>.</param>
        /// <param name="setup">(optional) Factory setup, by default is (<see cref="Setup.Default"/>)</param>
        /// <param name="ifAlreadyRegistered">(optional) Policy to deal with case when service with such type and name is already registered.</param>
        /// <param name="serviceKey">(optional) Service key (name). Could be of any of type with overridden <see cref="object.GetHashCode"/> and <see cref="object.Equals(object)"/>.</param>
        public static void Register<TImplementation>(this IRegistrator registrator,
            IReuse reuse = null, Made made = null, Setup setup = null,
            IfAlreadyRegistered ifAlreadyRegistered = IfAlreadyRegistered.AppendNotKeyed,
            object serviceKey = null)
        {
            registrator.Register<TImplementation, TImplementation>(reuse, made, setup, ifAlreadyRegistered, serviceKey);
        }

        /// <summary>Registers service type returned by Made expression.</summary>
        /// <typeparam name="TService">The type of service.</typeparam>
        /// <typeparam name="TMadeResult">The type returned by Made expression.</typeparam>
        /// <param name="registrator">Any <see cref="IRegistrator"/> implementation, e.g. <see cref="Container"/>.</param>
        /// <param name="made">Made specified with strongly-typed service creation expression.</param>
        /// <param name="reuse">(optional) <see cref="IReuse"/> implementation, e.g. <see cref="Reuse.Singleton"/>. Default value means no reuse, aka Transient.</param>
        /// <param name="setup">(optional) Factory setup, by default is (<see cref="Setup.Default"/>)</param>
        /// <param name="ifAlreadyRegistered">(optional) Policy to deal with case when service with such type and name is already registered.</param>
        /// <param name="serviceKey">(optional) Service key (name). Could be of any of type with overridden <see cref="object.GetHashCode"/> and <see cref="object.Equals(object)"/>.</param>
        public static void Register<TService, TMadeResult>(this IRegistrator registrator,
            Made.TypedMade<TMadeResult> made, IReuse reuse = null, Setup setup = null,
            IfAlreadyRegistered ifAlreadyRegistered = IfAlreadyRegistered.AppendNotKeyed,
            object serviceKey = null) where TMadeResult : TService
        {
            var factory = new ReflectionFactory(default(Type), reuse, made, setup);
            registrator.Register(factory, typeof(TService), serviceKey, ifAlreadyRegistered, isStaticallyChecked: true);
        }

        /// <summary>Registers service type returned by Made expression.</summary>
        /// <typeparam name="TService">The type of service.</typeparam>
        /// <param name="registrator">Any <see cref="IRegistrator"/> implementation, e.g. <see cref="Container"/>.</param>
        /// <param name="made">Made specified with strongly-typed service creation expression.</param>
        /// <param name="reuse">(optional) <see cref="IReuse"/> implementation, e.g. <see cref="Reuse.Singleton"/>. Default value means no reuse, aka Transient.</param>
        /// <param name="setup">(optional) Factory setup, by default is (<see cref="Setup.Default"/>)</param>
        /// <param name="ifAlreadyRegistered">(optional) Policy to deal with case when service with such type and name is already registered.</param>
        /// <param name="serviceKey">(optional) Service key (name). Could be of any of type with overridden <see cref="object.GetHashCode"/> and <see cref="object.Equals(object)"/>.</param>
        public static void Register<TService>(this IRegistrator registrator,
            Made.TypedMade<TService> made, IReuse reuse = null, Setup setup = null,
            IfAlreadyRegistered ifAlreadyRegistered = IfAlreadyRegistered.AppendNotKeyed,
            object serviceKey = null)
        {
            registrator.Register<TService, TService>(made, reuse, setup, ifAlreadyRegistered, serviceKey);
        }

        /// <summary>Action that could be used by User to customize register many default behavior.</summary>
        /// <param name="r">Registrator provided to do any arbitrary registration User wants.</param>
        /// <param name="serviceTypes">Valid service type that could be used with <paramref name="implType"/>.</param>
        /// <param name="implType">Concrete or open-generic implementation type.</param>
        internal delegate void RegisterManyAction(IRegistrator r, Type[] serviceTypes, Type implType);

        // todo: Perf: Add optional @isStaticallyChecked to skip check for implemented types.
        /// <summary>Registers many service types with the same implementation.</summary>
        /// <param name="registrator">Registrator/Container</param>
        /// <param name="serviceTypes">1 or more service types.</param>
        /// <param name="implementationType">Should implement service types. Will throw if not.</param>
        /// <param name="reuse">(optional)</param> <param name="made">(optional) How to create implementation instance.</param>
        /// <param name="setup">(optional)</param> <param name="ifAlreadyRegistered">(optional) By default <see cref="IfAlreadyRegistered.AppendNotKeyed"/></param>
        /// <param name="serviceKey">(optional)</param>
        public static void RegisterMany(this IRegistrator registrator, Type[] serviceTypes, Type implementationType,
            IReuse reuse = null, Made made = null, Setup setup = null,
            IfAlreadyRegistered ifAlreadyRegistered = IfAlreadyRegistered.AppendNotKeyed,
            object serviceKey = null)
        {
            var factory = new ReflectionFactory(implementationType, reuse, made, setup);
            if (serviceTypes.Length == 1)
                registrator.Register(serviceTypes[0], factory, ifAlreadyRegistered, serviceKey);
            else
                for (var i = 0; i < serviceTypes.Length; i++)
                    registrator.Register(serviceTypes[i], factory, ifAlreadyRegistered, serviceKey);
        }

        /// <summary>List of types excluded by default from RegisterMany convention.</summary>
        public static readonly string[] ExcludedGeneralPurposeServiceTypes =
        {
            "System.IDisposable",
            "System.ValueType",
            "System.ICloneable",
            "System.IEquatable",
            "System.IComparable",
            "System.Runtime.Serialization.ISerializable",
            "System.Collections.IStructuralEquatable",
            "System.Collections.IEnumerable",
            "System.Collections.IList",
            "System.Collections.ICollection",
        };

        /// <summary>Checks that type is not in the list of <see cref="ExcludedGeneralPurposeServiceTypes"/>.</summary>
        /// <param name="type">Type to check</param> <returns>True if not in the list.</returns>
        public static bool IsExcludedGeneralPurposeServiceType(this Type type)
        {
            return ExcludedGeneralPurposeServiceTypes.IndexOf((type.Namespace + "." + type.Name).Split('`')[0]) != -1;
        }

        /// <summary>Returns only those types that could be used as service types of <paramref name="type"/>. It means that
        /// for open-generic <paramref name="type"/> its service type should supply all type arguments.</summary>
        /// <param name="type">Source type: may be concrete, abstract or generic definition.</param>
        /// <param name="nonPublicServiceTypes">(optional) Include non public service types.</param>
        /// <returns>Array of types or empty.</returns>
        public static Type[] GetImplementedServiceTypes(this Type type, bool nonPublicServiceTypes = false)
        {
            var implementedTypes = type.GetImplementedTypes(ReflectionTools.AsImplementedType.SourceType);

            var serviceTypes = implementedTypes.Match(t =>
                (nonPublicServiceTypes || t.IsPublicOrNestedPublic()) &&
                !t.IsPrimitive() &&
                !t.IsExcludedGeneralPurposeServiceType());

            if (type.IsGenericDefinition())
                serviceTypes = serviceTypes.Match(
                    t => t.ContainsAllGenericTypeParameters(type.GetGenericParamsAndArgs()),
                    t => t.GetGenericDefinitionOrNull());

            return serviceTypes;
        }

        /// <summary>Returns the sensible services automatically discovered for RegisterMany implementation type.
        /// Excludes the collection wrapper interfaces.</summary>
        /// <param name="type">Source type, may be concrete, abstract or generic definition.</param>
        /// <param name="nonPublicServiceTypes">(optional) Include non public service types.</param>
        /// <returns>Array of types or empty.</returns>
        public static Type[] GetRegisterManyImplementedServiceTypes(this Type type, bool nonPublicServiceTypes = false)
        {
            return GetImplementedServiceTypes(type, nonPublicServiceTypes)
                .Match(t => !t.IsGenericDefinition() || WrappersSupport.ArrayInterfaces.IndexOf(t) == -1);
        }

        /// <summary>Returns the types suitable to be an implementation types for <see cref="ReflectionFactory"/>:
        /// actually a non abstract and not compiler generated classes.</summary>
        /// <param name="assembly">Assembly to get types from.</param>
        /// <returns>Types.</returns>
        public static IEnumerable<Type> GetImplementationTypes(this Assembly assembly)
        {
            return Portable.GetAssemblyTypes(assembly).Where(IsImplementationType);
        }

        /// <summary>Checks if type can be used as implementation type for reflection factory, 
        /// and therefore registered to container. Usually used to discover implementation types from assembly.</summary>
        /// <param name="type">Type to check.</param> <returns>True if implementation type.</returns>
        public static bool IsImplementationType(this Type type)
        {
            return type.IsClass() && !type.IsAbstract() && !type.IsCompilerGenerated();
        }

        /// <summary>Checks if <paramref name="type"/> implements the <paramref name="serviceType"/>,
        /// along the line checking if <paramref name="type"/> and <paramref name="serviceType"/>
        /// are valid implementation and service types.</summary>
        /// <param name="type">Implementation type.</param>
        /// <param name="serviceType">Service type.</param>
        /// <returns>Check result.</returns>
        public static bool ImplementsServiceType(this Type type, Type serviceType)
        {
            if (!type.IsImplementationType())
                return false;

            var serviceTypes = type.GetImplementedServiceTypes(nonPublicServiceTypes: true);
            if (serviceTypes.Length == 0)
                return false;

            if (!type.IsOpenGeneric())
                return serviceTypes.IndexOf(serviceType) != -1;

            if (!serviceType.IsGeneric()) // should be generic to supply arguments to implType
                return false;

            return serviceTypes.IndexOf(serviceType.GetGenericTypeDefinition()) != -1;
        }

        /// <summary>Registers many implementations with the auto-figured service types.</summary>
        /// <param name="registrator">Registrator/Container to register with.</param>
        /// <param name="implTypes">Implementation type provider.</param>
        /// <param name="action">(optional) User specified registration action:
        /// may be used to filter registrations or specify non-default registration options, e.g. Reuse or ServiceKey, etc.</param>
        /// <param name="nonPublicServiceTypes">(optional) Include non public service types.</param>
        public static void RegisterMany(this IRegistrator registrator, IEnumerable<Type> implTypes, RegisterManyAction action,
            bool nonPublicServiceTypes = false)
        {
            foreach (var implType in implTypes)
            {
                var serviceTypes = implType.GetRegisterManyImplementedServiceTypes(nonPublicServiceTypes);
                if (serviceTypes.IsNullOrEmpty())
                    continue;

                if (action == null)
                    registrator.RegisterMany(serviceTypes, implType);
                else
                    action(registrator, serviceTypes, implType);
            }
        }

        /// <summary>Registers many implementations with their auto-figured service types.</summary>
        /// <param name="registrator">Registrator/Container to register with.</param>
        /// <param name="implTypes">Implementation type provider.</param>
        /// <param name="reuse">(optional) Reuse to apply to all service registrations.</param>
        /// <param name="made">(optional) Allow to select constructor/method to create service, specify how to inject its parameters and properties/fields.</param>
        /// <param name="setup">(optional) Factory setup, by default is <see cref="Setup.Default"/>, check <see cref="Setup"/> class for available setups.</param>
        /// <param name="ifAlreadyRegistered">(optional) Policy to deal with existing service registrations.</param>
        /// <param name="serviceTypeCondition">(optional) Condition to select only specific service type to register.</param>
        /// <param name="nonPublicServiceTypes">(optional) Include non public service types.</param>
        /// <param name="serviceKey">(optional) service key (name). Could be of any of type with overridden <see cref="object.GetHashCode"/> and <see cref="object.Equals(object)"/>.</param>
        public static void RegisterMany(this IRegistrator registrator, IEnumerable<Type> implTypes,
            IReuse reuse = null, Made made = null, Setup setup = null,
            IfAlreadyRegistered ifAlreadyRegistered = IfAlreadyRegistered.AppendNotKeyed,
            Func<Type, bool> serviceTypeCondition = null, bool nonPublicServiceTypes = false,
            object serviceKey = null)
        {
            registrator.RegisterMany(implTypes, (r, serviceTypes, implType) =>
                {
                    if (serviceTypeCondition != null)
                        serviceTypes = serviceTypes.Match(serviceTypeCondition);
                    if (serviceTypes.Length != 0)
                        r.RegisterMany(serviceTypes, implType, reuse, made, setup, ifAlreadyRegistered, serviceKey);
                },
                nonPublicServiceTypes);
        }

        /// <summary>Registers single registration for all implemented internal interfaces and base classes.</summary>
        /// <typeparam name="TImplementation">The type to get service types from.</typeparam>
        /// <param name="registrator">Any <see cref="IRegistrator"/> implementation, e.g. <see cref="Container"/>.</param>
        /// <param name="reuse">(optional) <see cref="IReuse"/> implementation, e.g. <see cref="Reuse.Singleton"/>. Default value means no reuse, aka Transient.</param>
        /// <param name="made">(optional) Allow to select constructor/method to create service, specify how to inject its parameters and properties/fields.</param>
        /// <param name="setup">(optional) Factory setup, by default is <see cref="Setup.Default"/>, check <see cref="Setup"/> class for available setups.</param>
        /// <param name="ifAlreadyRegistered">(optional) policy to deal with case when service with such type and name is already registered.</param>
        /// <param name="serviceTypeCondition">(optional) Condition to select only specific service type to register.</param>
        /// <param name="nonPublicServiceTypes">(optional) Include non public service types.</param>
        /// <param name="serviceKey">(optional) service key (name). Could be of any of type with overridden <see cref="object.GetHashCode"/> and <see cref="object.Equals(object)"/>.</param>
        public static void RegisterMany<TImplementation>(this IRegistrator registrator,
            IReuse reuse = null, Made made = null, Setup setup = null,
            IfAlreadyRegistered ifAlreadyRegistered = IfAlreadyRegistered.AppendNotKeyed,
            Func<Type, bool> serviceTypeCondition = null, bool nonPublicServiceTypes = false,
            object serviceKey = null)
        {
            registrator.RegisterMany(new[] { typeof(TImplementation) }, (r, serviceTypes, implType) =>
                {
                    if (serviceTypeCondition != null)
                        serviceTypes = serviceTypes.Match(serviceTypeCondition);
                    if (serviceTypes.Length != 0)
                        r.RegisterMany(serviceTypes, implType, reuse, made, setup, ifAlreadyRegistered, serviceKey);
                },
                nonPublicServiceTypes);
        }

        /// <summary>Registers single registration for all implemented internal interfaces and base classes.</summary>
        /// <typeparam name="TMadeResult">The type returned by Made factory expression.</typeparam>
        /// <param name="registrator">Any <see cref="IRegistrator"/> implementation, e.g. <see cref="Container"/>.</param>
        /// <param name="made">Made specified with strongly-typed factory expression.</param>
        /// <param name="reuse">(optional) <see cref="IReuse"/> implementation, e.g. <see cref="Reuse.Singleton"/>. Default value means no reuse, aka Transient.</param>
        /// <param name="setup">(optional) Factory setup, by default is <see cref="Setup.Default"/>, check <see cref="Setup"/> class for available setups.</param>
        /// <param name="ifAlreadyRegistered">(optional) policy to deal with case when service with such type and name is already registered.</param>
        /// <param name="serviceTypeCondition">(optional) Condition to select only specific service type to register.</param>
        /// <param name="nonPublicServiceTypes">(optional) Include non public service types.</param>
        /// <param name="serviceKey">(optional) service key (name). Could be of any of type with overridden <see cref="object.GetHashCode"/> and <see cref="object.Equals(object)"/>.</param>
        public static void RegisterMany<TMadeResult>(this IRegistrator registrator, Made.TypedMade<TMadeResult> made,
            IReuse reuse = null, Setup setup = null,
            IfAlreadyRegistered ifAlreadyRegistered = IfAlreadyRegistered.AppendNotKeyed,
            Func<Type, bool> serviceTypeCondition = null, bool nonPublicServiceTypes = false,
            object serviceKey = null)
        {
            registrator.RegisterMany<TMadeResult>(reuse, made.ThrowIfNull(), setup,
                ifAlreadyRegistered, serviceTypeCondition, nonPublicServiceTypes, serviceKey);
        }

        /// <summary>Registers many implementations with their auto-figured service types.</summary>
        /// <param name="registrator">Registrator/Container to register with.</param>
        /// <param name="implTypeAssemblies">Assemblies with implementation/service types to register.</param>
        /// <param name="action">(optional) User specified registration action:
        /// may be used to filter registrations or specify non-default registration options, e.g. Reuse or ServiceKey, etc..</param>
        /// <param name="nonPublicServiceTypes">(optional) Include non public service types.</param>
        public static void RegisterMany(this IRegistrator registrator, IEnumerable<Assembly> implTypeAssemblies,
            RegisterManyAction action = null, bool nonPublicServiceTypes = false)
        {
            var implTypes = implTypeAssemblies.ThrowIfNull().SelectMany(GetImplementationTypes);
            registrator.RegisterMany(implTypes, action, nonPublicServiceTypes);
        }

        // todo: Add overload to specify list of service types to support case when I know contracts (service types) and provide implementation locations (assemblies)
        // and do not care about concrete implementation which is good principle.
        /// <summary>Registers many implementations with their auto-figured service types.</summary>
        /// <param name="registrator">Registrator/Container to register with.</param>
        /// <param name="implTypeAssemblies">Assemblies with implementation/service types to register.</param>
        /// <param name="serviceTypeCondition">Condition to select only specific service type to register.</param>
        /// <param name="reuse">(optional) Reuse to apply to all service registrations.</param>
        /// <param name="made">(optional) Allow to select constructor/method to create service, specify how to inject its parameters and properties/fields.</param>
        /// <param name="setup">(optional) Factory setup, by default is <see cref="Setup.Default"/>, check <see cref="Setup"/> class for available setups.</param>
        /// <param name="ifAlreadyRegistered">(optional) Policy to deal with existing service registrations.</param>
        /// <param name="nonPublicServiceTypes">(optional) Include non public service types.</param>
        /// <param name="serviceKey">(optional) service key (name). Could be of any of type with overridden <see cref="object.GetHashCode"/> and <see cref="object.Equals(object)"/>.</param>
        public static void RegisterMany(this IRegistrator registrator,
            IEnumerable<Assembly> implTypeAssemblies, Func<Type, bool> serviceTypeCondition,
            IReuse reuse = null, Made made = null, Setup setup = null,
            IfAlreadyRegistered ifAlreadyRegistered = IfAlreadyRegistered.AppendNotKeyed,
            bool nonPublicServiceTypes = false, object serviceKey = null)
        {
            var implTypes = implTypeAssemblies.ThrowIfNull().SelectMany(GetImplementationTypes);
            registrator.RegisterMany(implTypes,
                reuse, made, setup, ifAlreadyRegistered, serviceTypeCondition, nonPublicServiceTypes, serviceKey);
        }

        /// <summary>Registers a factory delegate for creating an instance of <typeparamref name="TService"/>.
        /// Delegate can use <see cref="IResolver"/> parameter to resolve any required dependencies, e.g.:
        /// <code lang="cs"><![CDATA[container.RegisterDelegate<ICar>(r => new Car(r.Resolve<IEngine>()))]]></code></summary>
        /// <typeparam name="TService">The type of service.</typeparam>
        /// <param name="registrator">Any <see cref="IRegistrator"/> implementation, e.g. <see cref="Container"/>.</param>
        /// <param name="factoryDelegate">The delegate used to create a instance of <typeparamref name="TService"/>.</param>
        /// <param name="reuse">(optional) <see cref="IReuse"/> implementation, e.g. <see cref="Reuse.Singleton"/>. Default value means no reuse, aka Transient.</param>
        /// <param name="setup">(optional) factory setup, by default is (<see cref="Setup.Default"/>)</param>
        /// <param name="ifAlreadyRegistered">(optional) policy to deal with case when service with such type and name is already registered.</param>
        /// <param name="serviceKey">(optional). Could be of any of type with overridden <see cref="object.GetHashCode"/> and <see cref="object.Equals(object)"/>.</param>
        /// <remarks>IMPORTANT: The method should be used as the last resort only! Though powerful it is a black-box for container,
        /// which prevents diagnostics, plus it is easy to get memory leaks (due variables captured in delegate closure),
        /// and impossible to use in compile-time scenarios.
        /// Consider using <see cref="Made"/> instead:
        /// <code lang="cs"><![CDATA[container.Register<ICar>(Made.Of(() => new Car(Arg.Of<IEngine>())))]]></code>.
        /// </remarks>
        public static void RegisterDelegate<TService>(this IRegistrator registrator, Func<IResolver, TService> factoryDelegate,
            IReuse reuse = null, Setup setup = null, IfAlreadyRegistered ifAlreadyRegistered = IfAlreadyRegistered.AppendNotKeyed,
            object serviceKey = null)
        {
            var factory = new DelegateFactory(r => factoryDelegate(r), reuse, setup);
            registrator.Register(factory, typeof(TService), serviceKey, ifAlreadyRegistered, false);
        }

        /// <summary>Registers a factory delegate for creating an instance of <paramref name="serviceType"/>.
        /// Delegate can use <see cref="IResolver"/> parameter to resolve any required dependencies, e.g.:
        /// <code lang="cs"><![CDATA[container.RegisterDelegate<ICar>(r => new Car(r.Resolve<IEngine>()))]]></code></summary>
        /// <param name="registrator">Any <see cref="IRegistrator"/> implementation, e.g. <see cref="Container"/>.</param>
        /// <param name="serviceType">Service type to register.</param>
        /// <param name="factoryDelegate">The delegate used to create a instance of <paramref name="serviceType"/>.</param>
        /// <param name="reuse">(optional) <see cref="IReuse"/> implementation, e.g. <see cref="Reuse.Singleton"/>. Default value means no reuse, aka Transient.</param>
        /// <param name="setup">(optional) factory setup, by default is (<see cref="Setup.Default"/>)</param>
        /// <param name="ifAlreadyRegistered">(optional) policy to deal with case when service with such type and name is already registered.</param>
        /// <param name="serviceKey">(optional) Could be of any of type with overridden <see cref="object.GetHashCode"/> and <see cref="object.Equals(object)"/>.</param>
        /// <remarks>IMPORTANT: The method should be used as the last resort only! Though powerful it is a black-box for container,
        /// which prevents diagnostics, plus it is easy to get memory leaks (due variables captured in delegate closure),
        /// and impossible to use in compile-time scenarios.
        /// Consider using <see cref="Made"/> instead:
        /// <code lang="cs"><![CDATA[container.Register<ICar>(Made.Of(() => new Car(Arg.Of<IEngine>())))]]></code>.
        /// </remarks>
        public static void RegisterDelegate(this IRegistrator registrator, Type serviceType, Func<IResolver, object> factoryDelegate,
            IReuse reuse = null, Setup setup = null, IfAlreadyRegistered ifAlreadyRegistered = IfAlreadyRegistered.AppendNotKeyed,
            object serviceKey = null)
        {
            if (serviceType.IsOpenGeneric())
                Throw.It(Error.RegisteringOpenGenericRequiresFactoryProvider, serviceType);
            Func<IResolver, object> checkedDelegate = r => factoryDelegate(r)
                .ThrowIfNotOf(serviceType, Error.RegedFactoryDlgResultNotOfServiceType, r);
            var factory = new DelegateFactory(checkedDelegate, reuse, setup);
            registrator.Register(factory, serviceType, serviceKey, ifAlreadyRegistered, false);
        }

        /// <summary>Registers decorator function that gets decorated value as input and returns decorator.
        /// Note: Delegate decorator will use <see cref="Reuse"/> of decoratee service.</summary>
        /// <typeparam name="TService">Registered service type to decorate.</typeparam>
        /// <param name="registrator">Registrator/Container.</param>
        /// <param name="getDecorator">Delegate returning decorating function.</param>
        /// <param name="condition">(optional) Condition for decorator application.</param>
        public static void RegisterDelegateDecorator<TService>(this IRegistrator registrator,
            Func<IResolver, Func<TService, TService>> getDecorator, Func<RequestInfo, bool> condition = null)
        {
            getDecorator.ThrowIfNull();

            // unique key to binds decorator factory and decorator registrations
            var factoryKey = new object();

            registrator.RegisterDelegate(_ =>
                    new DecoratorDelegateFactory<TService>(getDecorator),
                serviceKey: factoryKey);

            registrator.Register(Made.Of(
                    _ => ServiceInfo.Of<DecoratorDelegateFactory<TService>>(serviceKey: factoryKey),
                    f => f.Decorate(Arg.Of<TService>(), Arg.Of<IResolver>())),
                setup: Setup.DecoratorWith(condition, useDecorateeReuse: true));
        }

        internal sealed class DecoratorDelegateFactory<TDecoratee>
        {
            private readonly Func<IResolver, Func<TDecoratee, TDecoratee>> _getDecorator;

            public DecoratorDelegateFactory(Func<IResolver, Func<TDecoratee, TDecoratee>> getDecorator)
            {
                _getDecorator = getDecorator;
            }

            public TDecoratee Decorate(TDecoratee decoratee, IResolver resolver)
            {
                return _getDecorator(resolver)(decoratee);
            }
        }

        // todo: v3: remove
        /// <summary>Obsolete: replaced with UseInstance</summary>
        public static void RegisterInstance(this IContainer container, Type serviceType, object instance,
            IReuse reuse = null, IfAlreadyRegistered ifAlreadyRegistered = IfAlreadyRegistered.AppendNotKeyed,
            bool preventDisposal = false, bool weaklyReferenced = false, object serviceKey = null)
        {
            if (instance != null)
                instance.ThrowIfNotOf(serviceType, Error.RegisteringInstanceNotAssignableToServiceType);

            Throw.If(reuse is ResolutionScopeReuse, Error.ResolutionScopeIsNotSupportedForRegisterInstance, instance);
            reuse = reuse ?? Reuse.Singleton;

            var scopedReuse = reuse as CurrentScopeReuse;
            var scope = scopedReuse != null
                ? ((IScopeAccess)container).GetCurrentNamedScope(scopedReuse.Name, throwIfNotFound: true)
                : ((Container)container).SingletonScope;

            var setup = _defaultInstanceSetup;
            if (preventDisposal)
            {
                instance = new[] { instance };
                setup = _preventDisposableInstanceSetup;
            }
            if (weaklyReferenced)
            {
                instance = new WeakReference(instance);
                setup = preventDisposal
                    ? _weaklyReferencedAndPreventDisposableInstanceSetup
                    : _weaklyReferencedInstanceSetup;
            }

            InstanceFactory factory = null;
            if (ifAlreadyRegistered == IfAlreadyRegistered.Replace ||
                ifAlreadyRegistered == IfAlreadyRegistered.Keep)
            {
                var factories = container.GetAllServiceFactories(serviceType);
                if (serviceKey != null)
                    factories = factories.Where(f => serviceKey.Equals(f.Key));

                // Replace the single factory
                var factoriesList = factories.ToArray();
                if (factoriesList.Length == 1)
                    factory = factoriesList[0].Value as InstanceFactory;

                if (ifAlreadyRegistered == IfAlreadyRegistered.Keep && factoriesList.Length != 0)
                    return;
            }

            var canReuseAlreadyRegisteredFactory =
                factory != null && factory.Reuse == reuse && factory.Setup == setup;
            if (canReuseAlreadyRegisteredFactory)
                factory.ReplaceInstance(instance);
            else
                factory = new InstanceFactory(instance, reuse, setup);

            if (!canReuseAlreadyRegisteredFactory)
                container.Register(factory, serviceType, serviceKey, ifAlreadyRegistered, isStaticallyChecked: false);

            scope.SetOrAdd(scope.GetScopedItemIdOrSelf(factory.FactoryID), instance);
        }

        private static readonly Setup _defaultInstanceSetup =
            Setup.With(asResolutionCall: true);
        private static readonly Setup _weaklyReferencedInstanceSetup =
            Setup.With(weaklyReferenced: true, asResolutionCall: true);
        private static readonly Setup _preventDisposableInstanceSetup =
            Setup.With(preventDisposal: true, asResolutionCall: true);
        private static readonly Setup _weaklyReferencedAndPreventDisposableInstanceSetup =
            Setup.With(weaklyReferenced: true, preventDisposal: true, asResolutionCall: true);

        // todo: v3: remove
        /// <summary>Obsolete: replaced with UseInstance</summary>
        public static void RegisterInstance<TService>(this IContainer container, TService instance,
            IReuse reuse = null, IfAlreadyRegistered ifAlreadyRegistered = IfAlreadyRegistered.AppendNotKeyed,
            bool preventDisposal = false, bool weaklyReferenced = false, object serviceKey = null)
        {
            container.RegisterInstance(typeof(TService), instance, reuse, ifAlreadyRegistered,
                preventDisposal, weaklyReferenced, serviceKey);
        }

        /// <summary>Stores the externally created instance into open scope or singleton,
        /// replacing the existing registration and instance if any.</summary>
        /// <typeparam name="TService">Specified instance type. May be a base type or interface of instance actual type.</typeparam>
        /// <param name="container">Container to register</param>
        /// <param name="instance">Instance to register</param>
        /// <param name="preventDisposal">(optional) Prevents disposing of disposable instance by container.</param>
        /// <param name="weaklyReferenced">(optional)Stores the weak reference to instance, allowing to GC it.</param>
        /// <param name="serviceKey">(optional) Service key to identify instance from many.</param>
        public static void UseInstance<TService>(this IContainer container, TService instance,
            bool preventDisposal = false, bool weaklyReferenced = false,
            object serviceKey = null)
        {
            container.UseInstance(typeof(TService), instance, IfAlreadyRegistered.Replace, preventDisposal, weaklyReferenced, serviceKey);
        }

        /// <summary>Stores the externally created instance into open scope or singleton,
        /// replacing the existing registration and instance if any.</summary>
        /// <param name="container">Container to register</param>
        /// <param name="serviceType">Runtime service type to register instance with</param>
        /// <param name="instance">Instance to register</param>
        /// <param name="preventDisposal">(optional) Prevents disposing of disposable instance by container.</param>
        /// <param name="weaklyReferenced">(optional)Stores the weak reference to instance, allowing to GC it.</param>
        /// <param name="serviceKey">(optional) Service key to identify instance from many.</param>
        public static void UseInstance(this IContainer container, Type serviceType, object instance,
            bool preventDisposal = false, bool weaklyReferenced = false,
            object serviceKey = null)
        {
            container.UseInstance(serviceType, instance, IfAlreadyRegistered.Replace, preventDisposal, weaklyReferenced, serviceKey);
        }

        /// <summary>Stores the externally created instance into open scope or singleton,
        /// replacing the existing registration and instance if any.</summary>
        /// <typeparam name="TService">Specified instance type. May be a base type or interface of instance actual type.</typeparam>
        /// <param name="container">Container to register</param>
        /// <param name="instance">Instance to register</param>
        /// <param name="IfAlreadyRegistered">The default is <see cref="IfAlreadyRegistered.Replace"/>.</param>
        /// <param name="preventDisposal">(optional) Prevents disposing of disposable instance by container.</param>
        /// <param name="weaklyReferenced">(optional)Stores the weak reference to instance, allowing to GC it.</param>
        /// <param name="serviceKey">(optional) Service key to identify instance from many.</param>
        public static void UseInstance<TService>(this IContainer container, TService instance,
            IfAlreadyRegistered IfAlreadyRegistered,
            bool preventDisposal = false, bool weaklyReferenced = false,
            object serviceKey = null)
        {
            container.UseInstance(typeof(TService), instance, IfAlreadyRegistered, preventDisposal, weaklyReferenced, serviceKey);
        }

        /// <summary>Stores the externally created instance into open scope or singleton,
        /// replacing the existing registration and instance if any.</summary>
        /// <param name="container">Container to register</param>
        /// <param name="serviceType">Runtime service type to register instance with</param>
        /// <param name="instance">Instance to register</param>
        /// <param name="IfAlreadyRegistered">The default is <see cref="IfAlreadyRegistered.Replace"/>.</param>
        /// <param name="preventDisposal">(optional) Prevents disposing of disposable instance by container.</param>
        /// <param name="weaklyReferenced">(optional)Stores the weak reference to instance, allowing to GC it.</param>
        /// <param name="serviceKey">(optional) Service key to identify instance from many.</param>
        public static void UseInstance(this IContainer container, Type serviceType, object instance,
            IfAlreadyRegistered IfAlreadyRegistered,
            bool preventDisposal = false, bool weaklyReferenced = false,
            object serviceKey = null)
        {
            if (instance != null)
                instance.ThrowIfNotOf(serviceType, Error.RegisteringInstanceNotAssignableToServiceType);

            if (preventDisposal)
                instance = new[] { instance };

            if (weaklyReferenced)
                instance = new WeakReference(instance);

            // todo: v3: remove the hack
            ((Container)container).UseInstanceInternal(serviceType, instance, IfAlreadyRegistered, serviceKey);
        }

        /// <summary>Registers initializing action that will be called after service is resolved just before returning it to caller.
        /// Check example below for using initializer to automatically subscribe to singleton event aggregator.
        /// You can register multiple initializers for single service.
        /// Or you can register initializer for <see cref="Object"/> type to be applied for all services and use <paramref name="condition"/>
        /// to filter target services. </summary>
        /// <typeparam name="TTarget">Any type implemented by requested service type including service type itself and object type.</typeparam>
        /// <param name="registrator">Usually is <see cref="Container"/> object.</param>
        /// <param name="initialize">Delegate with <typeparamref name="TTarget"/> object and
        /// <see cref="IResolver"/> to resolve additional services required by initializer.</param>
        /// <param name="condition">(optional) Additional condition to select required target.</param>
        public static void RegisterInitializer<TTarget>(this IRegistrator registrator,
            Action<TTarget, IResolver> initialize, Func<RequestInfo, bool> condition = null)
        {
            initialize.ThrowIfNull();
            registrator.Register<object>(
                made: Made.Of(r => _initializerMethod.MakeGenericMethod(typeof(TTarget), r.ServiceType),
                    parameters: Parameters.Of.Type(_ => initialize)),
                setup: Setup.DecoratorWith(useDecorateeReuse: true,
                    condition: r => r.ServiceType.IsAssignableTo(typeof(TTarget)) &&
                                    (condition == null || condition(r))));
        }

        private static readonly MethodInfo _initializerMethod =
            typeof(Registrator).GetSingleMethodOrNull("Initializer", includeNonPublic: true).ThrowIfNull();

        internal static TService Initializer<TTarget, TService>(
            TService service, IResolver resolver, Action<TTarget, IResolver> initialize) where TService : TTarget
        {
            initialize(service, resolver);
            return service;
        }

        /// <summary>Registers dispose action for reused target service.</summary>
        /// <typeparam name="TService">Target service type.</typeparam>
        /// <param name="registrator">Registrator to use.</param>
        /// <param name="dispose">Actual dispose action to be invoke when scope is disposed.</param>
        /// <param name="condition">(optional) Additional way to identify the service.</param>
        public static void RegisterDisposer<TService>(this IRegistrator registrator,
            Action<TService> dispose, Func<RequestInfo, bool> condition = null)
        {
            dispose.ThrowIfNull();

            var disposerKey = new object();

            registrator.RegisterDelegate(_ => new Disposer<TService>(dispose),
                serviceKey: disposerKey,
                setup: Setup.With(useParentReuse: true));

            registrator.Register(Made.Of(
                    r => ServiceInfo.Of<Disposer<TService>>(serviceKey: disposerKey),
                    f => f.TrackForDispose(Arg.Of<TService>())),
                setup: Setup.DecoratorWith(condition, useDecorateeReuse: true));
        }

        [Janitor.SkipWeaving]
        internal sealed class Disposer<T> : IDisposable
        {
            private readonly Action<T> _dispose;
            private int _state;
            private const int Tracked = 1, Disposed = 2;
            private T _item;

            public Disposer(Action<T> dispose)
            {
                _dispose = dispose.ThrowIfNull();
            }

            public T TrackForDispose(T item)
            {
                if (Interlocked.CompareExchange(ref _state, Tracked, 0) != 0)
                    Throw.It(Error.Of("Something is {0} already."), _state == Tracked ? " tracked" : "disposed");
                _item = item;
                return item;
            }

            public void Dispose()
            {
                if (Interlocked.CompareExchange(ref _state, Disposed, Tracked) != Tracked)
                    return;
                var item = _item;
                if (item != null)
                {
                    _dispose(item);
                    _item = default(T);
                }
            }
        }

        /// <summary>Returns true if <paramref name="serviceType"/> is registered in container or its open generic definition is registered in container.</summary>
        /// <param name="registrator">Usually <see cref="Container"/> to explore or any other <see cref="IRegistrator"/> implementation.</param>
        /// <param name="serviceType">The type of the registered service.</param>
        /// <param name="serviceKey">(optional) Identifies registration via service key.
        /// Not provided or <c>null</c> service key means to check the <paramref name="serviceType"/> alone with any service key.</param>
        /// <param name="factoryType">(optional) factory type to lookup, <see cref="FactoryType.Service"/> by default.</param>
        /// <param name="condition">(optional) condition to specify what registered factory do you expect.</param>
        /// <returns>True if <paramref name="serviceType"/> is registered, false - otherwise.</returns>
        public static bool IsRegistered(this IRegistrator registrator, Type serviceType,
            object serviceKey = null, FactoryType factoryType = FactoryType.Service, Func<Factory, bool> condition = null)
        {
            return registrator.IsRegistered(serviceType, serviceKey, factoryType, condition);
        }

        /// <summary>Returns true if <typeparamref name="TService"/> type is registered in container or its open generic definition is registered in container.</summary>
        /// <typeparam name="TService">The type of service.</typeparam>
        /// <param name="registrator">Usually <see cref="Container"/> to explore or any other <see cref="IRegistrator"/> implementation.</param>
        /// <param name="serviceKey">(optional) Identifies registration via service key.
        /// Not provided or <c>null</c> service key means to check the <typeparamref name="TService"/> alone with any service key.</param>
        /// <param name="factoryType">(optional) factory type to lookup, <see cref="FactoryType.Service"/> by default.</param>
        /// <param name="condition">(optional) condition to specify what registered factory do you expect.</param>
        /// <returns>True if <typeparamref name="TService"/> name="serviceType"/> is registered, false - otherwise.</returns>
        public static bool IsRegistered<TService>(this IRegistrator registrator,
            object serviceKey = null, FactoryType factoryType = FactoryType.Service, Func<Factory, bool> condition = null)
        {
            return registrator.IsRegistered(typeof(TService), serviceKey, factoryType, condition);
        }

        /// <summary>Removes specified registration from container.</summary>
        /// <param name="registrator">Usually <see cref="Container"/> to explore or any other <see cref="IRegistrator"/> implementation.</param>
        /// <param name="serviceType">Type of service to remove.</param>
        /// <param name="serviceKey">(optional) Could be of any of type with overridden <see cref="object.GetHashCode"/> and <see cref="object.Equals(object)"/>.</param>
        /// <param name="factoryType">(optional) Factory type to lookup, <see cref="FactoryType.Service"/> by default.</param>
        /// <param name="condition">(optional) Condition for Factory to be removed.</param>
        public static void Unregister(this IRegistrator registrator, Type serviceType,
            object serviceKey = null, FactoryType factoryType = FactoryType.Service, Func<Factory, bool> condition = null)
        {
            registrator.Unregister(serviceType, serviceKey, factoryType, condition);
        }

        /// <summary>Removes specified registration from container.</summary>
        /// <typeparam name="TService">The type of service to remove.</typeparam>
        /// <param name="registrator">Usually <see cref="Container"/> or any other <see cref="IRegistrator"/> implementation.</param>
        /// <param name="serviceKey">(optional) Could be of any of type with overridden <see cref="object.GetHashCode"/> and <see cref="object.Equals(object)"/>.</param>
        /// <param name="factoryType">(optional) Factory type to lookup, <see cref="FactoryType.Service"/> by default.</param>
        /// <param name="condition">(optional) Condition for Factory to be removed.</param>
        public static void Unregister<TService>(this IRegistrator registrator,
            object serviceKey = null, FactoryType factoryType = FactoryType.Service, Func<Factory, bool> condition = null)
        {
            registrator.Unregister(typeof(TService), serviceKey, factoryType, condition);
        }
    }

    /// <summary>Defines convenient extension methods for <see cref="IResolver"/>.</summary>
    internal static class Resolver
    {
        /// <summary>Returns instance of <typepsaramref name="TService"/> type.</summary>
        /// <param name="serviceType">The type of the requested service.</param>
        /// <param name="resolver">Any <see cref="IResolver"/> implementation, e.g. <see cref="Container"/>.</param>
        /// <returns>The requested service instance.</returns>
        public static object Resolve(this IResolver resolver, Type serviceType)
        {
            return resolver.Resolve(serviceType, false);
        }

        /// <summary>Returns instance of <typepsaramref name="TService"/> type.</summary>
        /// <param name="serviceType">The type of the requested service.</param>
        /// <param name="resolver">Any <see cref="IResolver"/> implementation, e.g. <see cref="Container"/>.</param>
        /// <param name="ifUnresolved">Says how to handle unresolved service.</param>
        /// <returns>The requested service instance.</returns>
        public static object Resolve(this IResolver resolver, Type serviceType, IfUnresolved ifUnresolved)
        {
            return resolver.Resolve(serviceType, ifUnresolved == IfUnresolved.ReturnDefault);
        }

        /// <summary>Returns instance of <typepsaramref name="TService"/> type.</summary>
        /// <typeparam name="TService">The type of the requested service.</typeparam>
        /// <param name="resolver">Any <see cref="IResolver"/> implementation, e.g. <see cref="Container"/>.</param>
        /// <param name="ifUnresolved">(optional) Says how to handle unresolved service.</param>
        /// <returns>The requested service instance.</returns>
        public static TService Resolve<TService>(this IResolver resolver, IfUnresolved ifUnresolved = IfUnresolved.Throw)
        {
            return (TService)resolver.Resolve(typeof(TService), ifUnresolved == IfUnresolved.ReturnDefault);
        }

        /// <summary>Returns instance of <typeparamref name="TService"/> searching for <paramref name="requiredServiceType"/>.
        /// In case of <typeparamref name="TService"/> being generic wrapper like Func, Lazy, IEnumerable, etc., <paramref name="requiredServiceType"/>
        /// could specify wrapped service type.</summary>
        /// <typeparam name="TService">The type of the requested service.</typeparam>
        /// <param name="resolver">Any <see cref="IResolver"/> implementation, e.g. <see cref="Container"/>.</param>
        /// <param name="requiredServiceType">(optional) Service or wrapped type assignable to <typeparamref name="TService"/>.</param>
        /// <param name="ifUnresolved">(optional) Says how to handle unresolved service.</param>
        /// <returns>The requested service instance.</returns>
        /// <remarks>Using <paramref name="requiredServiceType"/> implicitly support Covariance for generic wrappers even in .Net 3.5.</remarks>
        /// <example><code lang="cs"><![CDATA[
        ///     container.Register<IService, Service>();
        ///     var services = container.Resolve<IEnumerable<object>>(typeof(IService));
        /// ]]></code></example>
        public static TService Resolve<TService>(this IResolver resolver, Type requiredServiceType, IfUnresolved ifUnresolved = IfUnresolved.Throw)
        {
            var ifUnresolvedReturnDefault = ifUnresolved == IfUnresolved.ReturnDefault;
            return (TService)resolver.Resolve(typeof(TService), null, ifUnresolvedReturnDefault, requiredServiceType, null, null);
        }

        /// <summary>Returns instance of <paramref name="serviceType"/> searching for <paramref name="requiredServiceType"/>.
        /// In case of <paramref name="serviceType"/> being generic wrapper like Func, Lazy, IEnumerable, etc., <paramref name="requiredServiceType"/>
        /// could specify wrapped service type.</summary>
        /// <param name="serviceType">The type of the requested service.</param>
        /// <param name="resolver">Any <see cref="IResolver"/> implementation, e.g. <see cref="Container"/>.</param>
        /// <param name="serviceKey">Service key (any type with <see cref="object.GetHashCode"/> and <see cref="object.Equals(object)"/> defined).</param>
        /// <param name="ifUnresolved">(optional) Says how to handle unresolved service.</param>
        /// <param name="requiredServiceType">(optional) Service or wrapped type assignable to <paramref name="serviceType"/>.</param>
        /// <returns>The requested service instance.</returns>
        /// <remarks>Using <paramref name="requiredServiceType"/> implicitly support Covariance for generic wrappers even in .Net 3.5.</remarks>
        /// <example><code lang="cs"><![CDATA[
        ///     container.Register<IService, Service>();
        ///     var services = container.Resolve(typeof(Lazy<object>), "someKey", requiredServiceType: typeof(IService));
        /// ]]></code></example>
        public static object Resolve(this IResolver resolver, Type serviceType, object serviceKey,
            IfUnresolved ifUnresolved = IfUnresolved.Throw, Type requiredServiceType = null)
        {
            var ifUnresolvedReturnDefault = ifUnresolved == IfUnresolved.ReturnDefault;
            return serviceKey == null && requiredServiceType == null
                ? resolver.Resolve(serviceType, ifUnresolvedReturnDefault)
                : resolver.Resolve(serviceType, serviceKey, ifUnresolvedReturnDefault, requiredServiceType, null, null);
        }

        /// <summary>Returns instance of <typepsaramref name="TService"/> type.</summary>
        /// <typeparam name="TService">The type of the requested service.</typeparam>
        /// <param name="resolver">Any <see cref="IResolver"/> implementation, e.g. <see cref="Container"/>.</param>
        /// <param name="serviceKey">Service key (any type with <see cref="object.GetHashCode"/> and <see cref="object.Equals(object)"/> defined).</param>
        /// <param name="ifUnresolved">(optional) Says how to handle unresolved service.</param>
        /// <param name="requiredServiceType">(optional) Service or wrapped type assignable to <typeparamref name="TService"/>.</param>
        /// <returns>The requested service instance.</returns>
        /// <remarks>Using <paramref name="requiredServiceType"/> implicitly support Covariance for generic wrappers even in .Net 3.5.</remarks>
        public static TService Resolve<TService>(this IResolver resolver, object serviceKey,
            IfUnresolved ifUnresolved = IfUnresolved.Throw, Type requiredServiceType = null)
        {
            return (TService)resolver.Resolve(typeof(TService), serviceKey, ifUnresolved, requiredServiceType);
        }

        /// <summary>Returns all registered services instances including all keyed and default registrations.
        /// Use <paramref name="behavior"/> to return either all registered services at the moment of resolve (dynamic fresh view) or
        /// the same services that were returned with first <see cref="ResolveMany{TService}"/> call (fixed view).</summary>
        /// <typeparam name="TService">Return collection item type. It denotes registered service type if <paramref name="requiredServiceType"/> is not specified.</typeparam>
        /// <param name="resolver">Usually <see cref="Container"/> object.</param>
        /// <param name="requiredServiceType">(optional) Denotes registered service type. Should be assignable to <typeparamref name="TService"/>.</param>
        /// <param name="behavior">(optional) Specifies new registered services awareness. Aware by default.</param>
        /// <param name="serviceKey">(optional) Service key.</param>
        /// <returns>Result collection of services.</returns>
        /// <remarks>The same result could be achieved by directly calling:
        /// <code lang="cs"><![CDATA[
        ///     container.Resolve<LazyEnumerable<IService>>();  // for dynamic result - default behavior
        ///     container.Resolve<IService[]>();                // for fixed array
        ///     container.Resolve<IEnumerable<IService>>();     // same as fixed array
        /// ]]></code>
        /// </remarks>
        public static IEnumerable<TService> ResolveMany<TService>(this IResolver resolver,
            Type requiredServiceType = null, ResolveManyBehavior behavior = ResolveManyBehavior.AsLazyEnumerable,
            object serviceKey = null)
        {
            return behavior == ResolveManyBehavior.AsLazyEnumerable
                ? resolver.ResolveMany(typeof(TService), serviceKey, requiredServiceType, null, null, RequestInfo.Empty, null).Cast<TService>()
                : resolver.Resolve<IEnumerable<TService>>(serviceKey, IfUnresolved.Throw, requiredServiceType);
        }

        /// <summary>Returns all registered services as objects, including all keyed and default registrations.</summary>
        /// <param name="resolver">Usually <see cref="Container"/> object.</param>
        /// <param name="serviceType">Type of item to resolve.</param>
        /// <param name="behavior">(optional) Specifies new registered services awareness. Aware by default.</param>
        /// <param name="serviceKey">(optional) Service key.</param>
        /// <returns>Result collection of services.</returns>
        /// <returns></returns>
        public static IEnumerable<object> ResolveMany(this IResolver resolver, Type serviceType,
            ResolveManyBehavior behavior = ResolveManyBehavior.AsLazyEnumerable,
            object serviceKey = null)
        {
            return resolver.ResolveMany<object>(serviceType, behavior, serviceKey);
        }

        internal static Expression CreateResolutionExpression(Request request,
            bool openResolutionScope = false, bool isRuntimeDependency = false)
        {
            request.ContainsNestedResolutionCall = true;

            var container = request.Container;

            if (!isRuntimeDependency && container.Rules.DependencyResolutionCallExpressions != null)
                PopulateDependencyResolutionCallExpressions(request, openResolutionScope);

            var serviceTypeExpr = Expression.Constant(request.ServiceType, typeof(Type));
            var ifUnresolvedExpr = Expression.Constant(request.IfUnresolved == IfUnresolved.ReturnDefault, typeof(bool));
            var requiredServiceTypeExpr = Expression.Constant(request.RequiredServiceType, typeof(Type));
            var serviceKeyExpr = container.GetOrAddStateItemExpression(request.ServiceKey, typeof(object));

            // first ensure that we have parent scope if any to propagate it across resolution call boundaries
            var scopeExpr = Container.GetResolutionScopeExpression(request);
            if (openResolutionScope)
            {
                // creates new scope and link it to existing (or new) parent
                // Here the code looks: scope = new Scope(scope, new KV<Type, object>(serviceType, serviceKey));
                var actualServiceTypeExpr = Expression.Constant(request.GetActualServiceType(), typeof(Type));
                var scopeCtor = typeof(Scope).GetSingleConstructorOrNull().ThrowIfNull();
                scopeExpr = Expression.New(scopeCtor, scopeExpr,
                    Expression.New(typeof(KV<Type, object>).GetSingleConstructorOrNull().ThrowIfNull(),
                        actualServiceTypeExpr, serviceKeyExpr));
            }

            var resolverExpr = Container.GetResolverExpr(request);

            // Only parent is converted to be passed to Resolve (the current request is formed by rest of Resolve parameters)
            var parentRequestInfo =
                request.RawParent.IsEmpty
                    ? request.PreResolveParent
                    : request.RawParent.RequestInfo;

            var preResolveParentExpr = container.RequestInfoToExpression(parentRequestInfo);

            var resolveCallExpr = Expression.Call(
                resolverExpr, "Resolve", ArrayTools.Empty<Type>(),
                serviceTypeExpr, serviceKeyExpr, ifUnresolvedExpr, requiredServiceTypeExpr,
                preResolveParentExpr, scopeExpr);

            return Expression.Convert(resolveCallExpr, request.ServiceType);
        }

        private static void PopulateDependencyResolutionCallExpressions(Request request, bool openResolutionScope)
        {
            // Actually calls nested Resolution Call and stores produced expression in collection:
            // - if the collection to accumulate call expressions is defined and:
            //   - Resolve call is the first nested in chain
            //   - Resolve call is not repeated for recursive dependency, e.g. new A(new Lazy<r => r.Resolve<B>()>) and new B(new A())
            var preResolveParent = request.PreResolveParent;
            if (preResolveParent.IsEmpty ||
                !request.RawParent.IsEmpty && !preResolveParent.EqualsWithoutParent(request.RawParent))
            {
                var serviceType = request.ServiceType;
                var serviceKey = request.ServiceKey;

                // Create scope for first nesting level or where corresponding setting is saying so
                var scope = request.Scope;
                if (scope == null || openResolutionScope)
                    scope = new Scope(scope, new KV<Type, object>(serviceType, serviceKey));

                var newRequest = Request.Create(request.Container, serviceType, serviceKey,
                    request.IfUnresolved, request.RequiredServiceType, scope,
                    request.ParentOrWrapper);

                var factory = request.Container.ResolveFactory(newRequest);
                var factoryExpr = factory == null ? null : factory.GetExpressionOrDefault(newRequest);
                if (factoryExpr != null)
                    request.Rules.DependencyResolutionCallExpressions.Swap(it =>
                        it.AddOrUpdate(newRequest.RequestInfo, factoryExpr));
            }
        }
    }

    /// <summary>Specifies result of <see cref="Resolver.ResolveMany{TService}"/>: either dynamic(lazy) or fixed view.</summary>
    internal enum ResolveManyBehavior
    {
        /// <summary>Lazy/dynamic item resolve.</summary>
        AsLazyEnumerable,
        /// <summary>Fixed array of item at time of resolve, newly registered/removed services won't be listed.</summary>
        AsFixedArray
    }

    /// <summary>Provides information required for service resolution: service type,
    /// and optional <see cref="ServiceDetails"/>: key, what to do if service unresolved, and required service type.</summary>
    internal interface IServiceInfo
    {
        /// <summary>The required piece of info: service type.</summary>
        Type ServiceType { get; }

        /// <summary>Additional optional details: service key, if-unresolved policy, required service type.</summary>
        ServiceDetails Details { get; }

        /// <summary>Creates info from service type and details.</summary>
        /// <param name="serviceType">Required service type.</param> <param name="details">Optional details.</param> <returns>Create info.</returns>
        IServiceInfo Create(Type serviceType, ServiceDetails details);
    }

    /// <summary>Provides optional service resolution details: service key, required service type, what return when service is unresolved,
    /// default value if service is unresolved, custom service value.</summary>
    internal class ServiceDetails
    {
        /// <summary>Default details if not specified, use default setting values, e.g. <see cref="DryIoc.IfUnresolved.Throw"/></summary>
        public static readonly ServiceDetails Default = Of();

        /// <summary>The same as <see cref="Default"/> with only difference <see cref="IfUnresolved"/> set to <see cref="DryIoc.IfUnresolved.ReturnDefault"/>.</summary>
        public static readonly ServiceDetails IfUnresolvedReturnDefault = Of(ifUnresolved: IfUnresolved.ReturnDefault);

        /// <summary>Creates new details out of provided settings, or returns default if all settings have default value.</summary>
        /// <param name="requiredServiceType">Registered service type to search for.</param>
        /// <param name="serviceKey">Service key.</param> <param name="ifUnresolved">If unresolved policy.</param>
        /// <param name="defaultValue">Custom default value, if specified it will automatically set <paramref name="ifUnresolved"/> to <see cref="DryIoc.IfUnresolved.ReturnDefault"/>.</param>
        /// <param name="metadataKey">(optional) Required metadata key</param> <param name="metadata">Required metadata or the value if key passed.</param>
        /// <returns>New details.</returns>
        public static ServiceDetails Of(Type requiredServiceType = null,
            object serviceKey = null, IfUnresolved ifUnresolved = IfUnresolved.Throw,
            object defaultValue = null, string metadataKey = null, object metadata = null)
        {
            if (defaultValue != null && ifUnresolved == IfUnresolved.Throw)
                ifUnresolved = IfUnresolved.ReturnDefault;

            return new ServiceDetails(requiredServiceType, ifUnresolved,
                serviceKey, metadataKey, metadata,
                defaultValue, hasCustomValue: false);
        }

        /// <summary>Sets custom value for service. This setting is orthogonal to the rest.</summary>
        /// <param name="value">Custom value.</param> <returns>Details with custom value.</returns>
        public static ServiceDetails Of(object value)
        {
            // Using default value with invalid ifUnresolved state to indicate custom value.
            return new ServiceDetails(null, IfUnresolved.Throw, null, null, null, value, hasCustomValue: true);
        }

        /// <summary>Service type to search in registry. Should be assignable to user requested service type.</summary>
        public readonly Type RequiredServiceType;

        /// <summary>Service key provided with registration.</summary>
        public readonly object ServiceKey;

        /// <summary>Metadata key to find in metadata dictionary in resolved service.</summary>
        public readonly string MetadataKey;

        /// <summary>Metadata value to find in resolved service.</summary>
        public readonly object Metadata;

        /// <summary>Policy to deal with unresolved request.</summary>
        public readonly IfUnresolved IfUnresolved;

        /// <summary>Indicates that the custom value is specified.</summary>
        public readonly bool HasCustomValue;

        /// <summary>Either default or custom value depending on <see cref="IfUnresolved"/> setting.</summary>
        private readonly object _value;

        /// <summary>Value to use in case <see cref="IfUnresolved"/> is set to <see cref="DryIoc.IfUnresolved.ReturnDefault"/>.</summary>
        public object DefaultValue { get { return IfUnresolved == IfUnresolved.ReturnDefault ? _value : null; } }

        /// <summary>Custom value specified for dependency.</summary>
        public object CustomValue { get { return IfUnresolved != IfUnresolved.ReturnDefault ? _value : null; } }

        /// <summary>Pretty prints service details to string for debugging and errors.</summary> <returns>Details string.</returns>
        public override string ToString()
        {
            var s = new StringBuilder();

            if (HasCustomValue)
                return s.Append("{CustomValue=").Print(CustomValue ?? "null", "\"").Append("}").ToString();

            if (RequiredServiceType != null)
                s.Append("{RequiredServiceType=").Print(RequiredServiceType);
            if (ServiceKey != null)
                (s.Length == 0 ? s.Append('{') : s.Append(", ")).Append("ServiceKey=").Print(ServiceKey, "\"");
            if (MetadataKey != null || Metadata != null)
                (s.Length == 0 ? s.Append('{') : s.Append(", "))
                    .Append("Metadata=").Append(new KeyValuePair<string, object>(MetadataKey, Metadata));
            if (IfUnresolved != IfUnresolved.Throw)
                (s.Length == 0 ? s.Append('{') : s.Append(", ")).Append(IfUnresolved);
            return (s.Length == 0 ? s : s.Append('}')).ToString();
        }

        private ServiceDetails(Type requiredServiceType, IfUnresolved ifUnresolved,
            object serviceKey, string metadataKey, object metadata,
            object value, bool hasCustomValue)
        {
            RequiredServiceType = requiredServiceType;
            IfUnresolved = ifUnresolved;
            ServiceKey = serviceKey;
            MetadataKey = metadataKey;
            Metadata = metadata;
            _value = value;
            HasCustomValue = hasCustomValue;
        }
    }

    /// <summary>Contains tools for combining or propagating of <see cref="IServiceInfo"/> independent of its concrete implementations.</summary>
    internal static class ServiceInfoTools
    {
        /// <summary>Creates service info with new type but keeping the details.</summary>
        /// <param name="source">Source info.</param> <param name="serviceType">New service type.</param>
        /// <returns>New info.</returns>
        public static IServiceInfo With(this IServiceInfo source, Type serviceType)
        {
            return source.Create(serviceType, source.Details);
        }

        /// <summary>Creates new info with new IfUnresolved behavior or returns the original info if behavior is not different,
        /// or the passed info is not a <see cref="ServiceDetails.HasCustomValue"/>.</summary>
        /// <param name="source">Registered service type to search for.</param>
        /// <param name="ifUnresolved">New If unresolved behavior.</param>
        /// <returns>New info if the new details are different from the old one, and original info otherwise.</returns>
        public static IServiceInfo WithIfUnresolved(this IServiceInfo source, IfUnresolved ifUnresolved)
        {
            var details = source.Details;
            if (details.IfUnresolved == ifUnresolved || details.HasCustomValue)
                return source;

            if (details == ServiceDetails.Default)
                details = ServiceDetails.IfUnresolvedReturnDefault;
            else
                details = ServiceDetails.Of(details.RequiredServiceType, details.ServiceKey,
                    ifUnresolved, details.DefaultValue, details.MetadataKey, details.Metadata);

            return source.Create(source.ServiceType, details);
        }

        // todo: Should be renamed or better to be removed, the whole operation should be hidden behind abstraction
        // todo: Remove request parameter as it is not used anymore
        /// <summary>Combines service info with details: the main task is to combine service and required service type.</summary>
        /// <typeparam name="T">Type of <see cref="IServiceInfo"/>.</typeparam>
        /// <param name="serviceInfo">Source info.</param> <param name="details">Details to combine with info.</param>
        /// <param name="request">Owner request.</param> <returns>Original source or new combined info.</returns>
        public static T WithDetails<T>(this T serviceInfo, ServiceDetails details, Request request/*ignored*/)
            where T : IServiceInfo
        {
            details = details ?? ServiceDetails.Default;
            var sourceDetails = serviceInfo.Details;
            if (!details.HasCustomValue &&
                sourceDetails != ServiceDetails.Default &&
                sourceDetails != details)
            {
                var serviceKey = details.ServiceKey ?? sourceDetails.ServiceKey;
                var metadataKey = details.MetadataKey ?? sourceDetails.MetadataKey;
                var metadata = metadataKey == details.MetadataKey ? details.Metadata : sourceDetails.Metadata;
                var defaultValue = details.DefaultValue ?? sourceDetails.DefaultValue;

                details = ServiceDetails.Of(details.RequiredServiceType, serviceKey,
                    details.IfUnresolved, defaultValue, metadataKey, metadata);
            }

            return WithRequiredServiceType(serviceInfo, details, null);
        }

        // todo: v3: remove unused @request parameter
        internal static T WithRequiredServiceType<T>(T serviceInfo, ServiceDetails details, Request request)
            where T : IServiceInfo
        {
            var serviceType = serviceInfo.ServiceType;
            var requiredServiceType = details.RequiredServiceType;

            if (requiredServiceType != null && requiredServiceType == serviceType)
                details = ServiceDetails.Of(null,
                    details.ServiceKey, details.IfUnresolved, details.DefaultValue,
                    details.MetadataKey, details.Metadata);

            return serviceType == serviceInfo.ServiceType
                   && (details == null || details == serviceInfo.Details)
                ? serviceInfo // if service type unchanged and details absent, or details are the same return original info.
                : (T)serviceInfo.Create(serviceType, details); // otherwise: create new.
        }

        // todo: v3: remove unused @shouldInheritServiceKey parameter
        // todo: v3: make @container parameter non optional
        /// <summary>Enables propagation/inheritance of info between dependency and its owner:
        /// for instance <see cref="ServiceDetails.RequiredServiceType"/> for wrappers.</summary>
        /// <param name="dependency">Dependency info.</param>
        /// <param name="owner">Dependency holder/owner info.</param>
        /// <param name="shouldInheritServiceKey">(optional) to be removed</param>
        /// <param name="ownerType">(optional)to be removed</param>
        /// <param name="container">required for <see cref="IContainer.GetWrappedType"/></param>
        /// <returns>Either input dependency info, or new info with properties inherited from the owner.</returns>
        public static IServiceInfo InheritInfoFromDependencyOwner(this IServiceInfo dependency, IServiceInfo owner,
            bool shouldInheritServiceKey = false, FactoryType ownerType = FactoryType.Service,
            IContainer container = null)
        {
            var ownerDetails = owner.Details;
            if (ownerDetails == null || ownerDetails == ServiceDetails.Default)
                return dependency;

            var dependencyDetails = dependency.Details;

            var ifUnresolved = ownerDetails.IfUnresolved == IfUnresolved.Throw
                ? dependencyDetails.IfUnresolved
                : ownerDetails.IfUnresolved;

            var serviceType = dependency.ServiceType;
            var requiredServiceType = dependencyDetails.RequiredServiceType;
            var ownerRequiredServiceType = ownerDetails.RequiredServiceType;

            var serviceKey = dependencyDetails.ServiceKey;
            var metadataKey = dependencyDetails.MetadataKey;
            var metadata = dependencyDetails.Metadata;

            // propagate key and meta to the actual service
            if (ownerType == FactoryType.Wrapper ||
                // for decorated dependency, but not for other decorator dependencies
                ownerType == FactoryType.Decorator &&
                container != null &&
                container.GetWrappedType(serviceType, requiredServiceType).IsAssignableTo(owner.ServiceType))
            {
                if (serviceKey == null)
                {
                    serviceKey = ownerDetails.ServiceKey;
                }

                if (metadataKey == null && metadata == null)
                {
                    metadataKey = ownerDetails.MetadataKey;
                    metadata = ownerDetails.Metadata;
                }
            }

            if (ownerType != FactoryType.Service && ownerRequiredServiceType != null &&
                requiredServiceType == null) // if only dependency does not have its own
                requiredServiceType = ownerRequiredServiceType;

            if (serviceType == dependency.ServiceType && serviceKey == dependencyDetails.ServiceKey &&
                metadataKey == dependencyDetails.MetadataKey && metadata == dependencyDetails.Metadata &&
                ifUnresolved == dependencyDetails.IfUnresolved && requiredServiceType == dependencyDetails.RequiredServiceType)
                return dependency;

            if (serviceType == requiredServiceType)
                requiredServiceType = null;

            var serviceDetails = ServiceDetails.Of(requiredServiceType,
                serviceKey, ifUnresolved, dependencyDetails.DefaultValue,
                metadataKey, metadata);

            return dependency.Create(serviceType, serviceDetails);
        }

        /// <summary>Returns required service type if it is specified and assignable to service type,
        /// otherwise returns service type.</summary>
        /// <returns>The type to be used for lookup in registry.</returns>
        public static Type GetActualServiceType(this IServiceInfo info)
        {
            var requiredServiceType = info.Details.RequiredServiceType;

            return requiredServiceType != null && requiredServiceType.IsAssignableTo(info.ServiceType)
                ? requiredServiceType : info.ServiceType;
        }

        /// <summary>Appends info string representation into provided builder.</summary>
        /// <param name="s">String builder to print to.</param> <param name="info">Info to print.</param>
        /// <returns>String builder with appended info.</returns>
        public static StringBuilder Print(this StringBuilder s, IServiceInfo info)
        {
            s.Print(info.ServiceType);
            var details = info.Details.ToString();
            return details == string.Empty ? s : s.Append(' ').Append(details);
        }
    }

    /// <summary>Represents custom or resolution root service info, there is separate representation for parameter,
    /// property and field dependencies.</summary>
    internal class ServiceInfo : IServiceInfo
    {
        /// <summary>Empty service info for convenience.</summary>
        public static readonly IServiceInfo Empty = new ServiceInfo(null);

        /// <summary>Creates info out of provided settings</summary>
        /// <param name="serviceType">Service type</param>
        /// <param name="ifUnresolved">(optional) If unresolved policy. Set to Throw if not specified.</param>
        ///  <param name="serviceKey">(optional) Service key.</param>
        /// <returns>Created info.</returns>
        public static ServiceInfo Of(Type serviceType,
            IfUnresolved ifUnresolved = IfUnresolved.Throw, object serviceKey = null)
        {
            return Of(serviceType, null, ifUnresolved, serviceKey);
        }

        /// <summary>Creates info out of provided settings</summary>
        /// <param name="serviceType">Service type</param>
        /// <param name="requiredServiceType">Registered service type to search for.</param>
        /// <param name="ifUnresolved">(optional) If unresolved policy. Set to Throw if not specified.</param>
        /// <param name="serviceKey">(optional) Service key.</param>
        /// <param name="metadataKey">(optional) Required metadata key</param>
        /// <param name="metadata">Required metadata or the value if key passed.</param>
        /// <returns>Created info.</returns>
        public static ServiceInfo Of(Type serviceType, Type requiredServiceType,
            IfUnresolved ifUnresolved = IfUnresolved.Throw, object serviceKey = null,
            string metadataKey = null, object metadata = null)
        {
            serviceType.ThrowIfNull();

            // remove unnecessary details if service and required type are the same
            if (serviceType == requiredServiceType)
                requiredServiceType = null;

            return serviceKey == null && requiredServiceType == null
                   && metadataKey == null && metadata == null
                ? (ifUnresolved == IfUnresolved.Throw
                    ? new ServiceInfo(serviceType)
                    : new WithDetails(serviceType,
                        ServiceDetails.IfUnresolvedReturnDefault))
                : new WithDetails(serviceType,
                    ServiceDetails.Of(requiredServiceType, serviceKey, ifUnresolved, null, metadataKey, metadata));
        }

        /// <summary>Creates service info using typed <typeparamref name="TService"/>.</summary>
        /// <typeparam name="TService">Service type.</typeparam>
        /// <param name="ifUnresolved">(optional)</param> <param name="serviceKey">(optional)</param>
        /// <returns>Created info.</returns>
        public static Typed<TService> Of<TService>(IfUnresolved ifUnresolved = IfUnresolved.Throw, object serviceKey = null)
        {
            return serviceKey == null && ifUnresolved == IfUnresolved.Throw
                ? new Typed<TService>()
                : new TypedWithDetails<TService>(ServiceDetails.Of(null, serviceKey, ifUnresolved));
        }

        /// <summary>Strongly-typed version of Service Info.</summary> <typeparam name="TService">Service type.</typeparam>
        internal class Typed<TService> : ServiceInfo
        {
            /// <summary>Creates service info object.</summary>
            public Typed() : base(typeof(TService)) { }
        }

        /// <summary>Type of service to create. Indicates registered service in registry.</summary>
        public Type ServiceType { get; private set; }

        /// <summary>Additional settings. If not specified uses <see cref="ServiceDetails.Default"/>.</summary>
        public virtual ServiceDetails Details { get { return ServiceDetails.Default; } }

        /// <summary>Creates info from service type and details.</summary>
        /// <param name="serviceType">Required service type.</param> <param name="details">Optional details.</param> <returns>Create info.</returns>
        public IServiceInfo Create(Type serviceType, ServiceDetails details)
        {
            return details == ServiceDetails.Default
                ? new ServiceInfo(serviceType)
                : new WithDetails(serviceType, details);
        }

        /// <summary>Prints info to string using <see cref="ServiceInfoTools.Print"/>.</summary> <returns>Printed string.</returns>
        public override string ToString()
        {
            return new StringBuilder().Print(this).ToString();
        }

        #region Implementation

        private ServiceInfo(Type serviceType)
        {
            ServiceType = serviceType;
        }

        private class WithDetails : ServiceInfo
        {
            public override ServiceDetails Details { get { return _details; } }
            public WithDetails(Type serviceType, ServiceDetails details) : base(serviceType) { _details = details; }
            private readonly ServiceDetails _details;
        }

        private class TypedWithDetails<TService> : Typed<TService>
        {
            public override ServiceDetails Details { get { return _details; } }
            public TypedWithDetails(ServiceDetails details) { _details = details; }
            private readonly ServiceDetails _details;
        }

        #endregion
    }

    /// <summary>Provides <see cref="IServiceInfo"/> for parameter,
    /// by default using parameter name as <see cref="IServiceInfo.ServiceType"/>.</summary>
    /// <remarks>For parameter default setting <see cref="ServiceDetails.IfUnresolved"/> is <see cref="IfUnresolved.Throw"/>.</remarks>
    internal class ParameterServiceInfo : IServiceInfo
    {
        /// <summary>Creates service info from parameter alone, setting service type to parameter type,
        /// and setting resolution policy to <see cref="IfUnresolved.ReturnDefault"/> if parameter is optional.</summary>
        /// <param name="parameter">Parameter to create info for.</param>
        /// <returns>Parameter service info.</returns>
        public static ParameterServiceInfo Of(ParameterInfo parameter)
        {
            parameter.ThrowIfNull();

            var isOptional = parameter.IsOptional;
            var defaultValue = isOptional ? parameter.DefaultValue : null;
            var hasDefaultValue = defaultValue != null && parameter.ParameterType.IsTypeOf(defaultValue);

            return !isOptional
                ? new ParameterServiceInfo(parameter)
                : new WithDetails(parameter, !hasDefaultValue
                    ? ServiceDetails.IfUnresolvedReturnDefault
                    : ServiceDetails.Of(ifUnresolved: IfUnresolved.ReturnDefault, defaultValue: defaultValue));
        }

        /// <summary>Service type specified by <see cref="ParameterInfo.ParameterType"/>.</summary>
        public virtual Type ServiceType { get { return Parameter.ParameterType; } }

        /// <summary>Optional service details.</summary>
        public virtual ServiceDetails Details { get { return ServiceDetails.Default; } }

        /// <summary>Creates info from service type and details.</summary>
        /// <param name="serviceType">Required service type.</param> <param name="details">Optional details.</param> <returns>Create info.</returns>
        public IServiceInfo Create(Type serviceType, ServiceDetails details)
        {
            return serviceType == ServiceType
                ? new WithDetails(Parameter, details)
                : new TypeWithDetails(Parameter, serviceType, details);
        }

        /// <summary>Parameter info.</summary>
        public readonly ParameterInfo Parameter;

        /// <summary>Prints info to string using <see cref="ServiceInfoTools.Print"/>.</summary> <returns>Printed string.</returns>
        public override string ToString()
        {
            return new StringBuilder().Print(this).Append(" as parameter ").Print(Parameter.Name, "\"").ToString();
        }

        #region Implementation

        private ParameterServiceInfo(ParameterInfo parameter) { Parameter = parameter; }

        private class WithDetails : ParameterServiceInfo
        {
            public override ServiceDetails Details { get { return _details; } }
            public WithDetails(ParameterInfo parameter, ServiceDetails details)
                : base(parameter)
            { _details = details; }
            private readonly ServiceDetails _details;
        }

        private sealed class TypeWithDetails : WithDetails
        {
            public override Type ServiceType { get { return _serviceType; } }
            public TypeWithDetails(ParameterInfo parameter, Type serviceType, ServiceDetails details)
                : base(parameter, details)
            { _serviceType = serviceType; }
            private readonly Type _serviceType;
        }

        #endregion
    }

    /// <summary>Base class for property and field dependency info.</summary>
    internal abstract class PropertyOrFieldServiceInfo : IServiceInfo
    {
        /// <summary>Create member info out of provide property or field.</summary>
        /// <param name="member">Member is either property or field.</param> <returns>Created info.</returns>
        public static PropertyOrFieldServiceInfo Of(MemberInfo member)
        {
            return member.ThrowIfNull() is PropertyInfo ? (PropertyOrFieldServiceInfo)
                new Property((PropertyInfo)member) : new Field((FieldInfo)member);
        }

        /// <summary>The required service type. It will be either <see cref="FieldInfo.FieldType"/> or <see cref="PropertyInfo.PropertyType"/>.</summary>
        public abstract Type ServiceType { get; }

        /// <summary>Optional details: service key, if-unresolved policy, required service type.</summary>
        public virtual ServiceDetails Details { get { return ServiceDetails.IfUnresolvedReturnDefault; } }

        /// <summary>Creates info from service type and details.</summary>
        /// <param name="serviceType">Required service type.</param> <param name="details">Optional details.</param> <returns>Create info.</returns>
        public abstract IServiceInfo Create(Type serviceType, ServiceDetails details);

        /// <summary>Either <see cref="PropertyInfo"/> or <see cref="FieldInfo"/>.</summary>
        public abstract MemberInfo Member { get; }

        /// <summary>Sets property or field value on provided holder object.</summary>
        /// <param name="holder">Holder of property or field.</param> <param name="value">Value to set.</param>
        public abstract void SetValue(object holder, object value);

        #region Implementation

        private class Property : PropertyOrFieldServiceInfo
        {
            public override Type ServiceType { get { return _property.PropertyType; } }
            public override IServiceInfo Create(Type serviceType, ServiceDetails details)
            {
                return serviceType == ServiceType
                    ? new WithDetails(_property, details)
                    : new TypeWithDetails(_property, serviceType, details);
            }

            public override MemberInfo Member { get { return _property; } }
            public override void SetValue(object holder, object value)
            {
                _property.SetValue(holder, value, null);
            }

            public override string ToString()
            {
                return new StringBuilder().Print(this).Append(" as property ").Print(_property.Name, "\"").ToString();
            }

            private readonly PropertyInfo _property;
            public Property(PropertyInfo property)
            {
                _property = property;
            }

            private class WithDetails : Property
            {
                public override ServiceDetails Details { get { return _details; } }
                public WithDetails(PropertyInfo property, ServiceDetails details)
                    : base(property)
                { _details = details; }
                private readonly ServiceDetails _details;
            }

            private sealed class TypeWithDetails : WithDetails
            {
                public override Type ServiceType { get { return _serviceType; } }
                public TypeWithDetails(PropertyInfo property, Type serviceType, ServiceDetails details)
                    : base(property, details)
                { _serviceType = serviceType; }
                private readonly Type _serviceType;
            }
        }

        private class Field : PropertyOrFieldServiceInfo
        {
            public override Type ServiceType { get { return _field.FieldType; } }
            public override IServiceInfo Create(Type serviceType, ServiceDetails details)
            {
                return serviceType == null
                    ? new WithDetails(_field, details)
                    : new TypeWithDetails(_field, serviceType, details);
            }

            public override MemberInfo Member { get { return _field; } }
            public override void SetValue(object holder, object value)
            {
                _field.SetValue(holder, value);
            }

            public override string ToString()
            {
                return new StringBuilder().Print(this).Append(" as field ").Print(_field.Name, "\"").ToString();
            }

            private readonly FieldInfo _field;
            public Field(FieldInfo field)
            {
                _field = field;
            }

            private class WithDetails : Field
            {
                public override ServiceDetails Details { get { return _details; } }
                public WithDetails(FieldInfo field, ServiceDetails details)
                    : base(field)
                { _details = details; }
                private readonly ServiceDetails _details;
            }

            private sealed class TypeWithDetails : WithDetails
            {
                public override Type ServiceType { get { return _serviceType; } }
                public TypeWithDetails(FieldInfo field, Type serviceType, ServiceDetails details)
                    : base(field, details)
                { _serviceType = serviceType; }
                private readonly Type _serviceType;
            }
        }

        #endregion
    }

    /// <summary>Stored check results of two kinds: inherited down dependency chain and not.</summary>
    [Flags]
    internal enum RequestFlags
    {
        /// <summary>Not inherited</summary>
        TracksTransientDisposable = 1 << 1,
        /// <summary>Not inherited</summary>
        IsServiceCollection = 1 << 2,

        /// <summary>Inherited</summary>
        IsSingletonOrDependencyOfSingleton = 1 << 3,
        /// <summary>Inherited</summary>
        IsWrappedInFunc = 1 << 4,
        /// <summary>Inherited</summary>
        IsWrappedInFuncWithArgs = 1 << 5,
    }

    /// <summary>Contains resolution stack with information about resolved service and factory for it,
    /// Additionally request contain weak reference to <see cref="IContainer"/>. That the all required information for resolving services.
    /// Request implements <see cref="IResolver"/> interface on top of provided container, which could be use by delegate factories.</summary>
    internal sealed class Request
    {
        /// <summary>Not inherited down dependency chain.</summary>
        public static readonly RequestFlags NotInheritedFlags
            = RequestFlags.TracksTransientDisposable
              | RequestFlags.IsServiceCollection;

        // todo: v3: remove
        /// <summary>Obsolete: replaced with <see cref="Create"/>/.</summary>
        public static Request CreateEmpty(Container container)
        {
            var resolverContext = new RequestContext(container, container, null, RequestInfo.Empty);
            return new Request(resolverContext, null, ServiceInfo.Empty, null, null, null, default(RequestFlags));
        }

        private static readonly Request _empty = new Request(null, null, ServiceInfo.Empty, null, null, null, default(RequestFlags));

        /// <summary>Creates empty request associated with container. 
        /// The shared part of request is stored in request context. Pre-request info is also store once in shared context.</summary>
        /// <param name="container">Associated container - part of request context.</param>
        /// <param name="serviceType">Service type to resolve.</param>
        /// <param name="serviceKey">(optional) Service key to resolve.</param>
        /// <param name="ifUnresolved">(optional) How to handle unresolved service.</param>
        /// <param name="requiredServiceType">(optional) Actual registered or unwrapped service type to look for.</param>
        /// <param name="scope">(optional) Pre-request info: resolution scope.</param> // todo: v3: remove
        /// <param name="preResolveParent">(optional) Request info preceding Resolve call.</param>
        /// <returns>New request with provided info.</returns>
        public static Request Create(IContainer container, Type serviceType,
            object serviceKey = null, IfUnresolved ifUnresolved = IfUnresolved.Throw, Type requiredServiceType = null,
            IScope scope = null, RequestInfo preResolveParent = null)
        {
            serviceType.ThrowIfNull()
                .ThrowIf(serviceType.IsOpenGeneric(), Error.ResolvingOpenGenericServiceTypeIsNotPossible);

            if (preResolveParent == null)
                preResolveParent = RequestInfo.Empty;

            var resolverContext = new RequestContext(container, (IScopeAccess)container, scope, preResolveParent);

            IServiceInfo serviceInfo = ServiceInfo.Of(serviceType, requiredServiceType, ifUnresolved, serviceKey);

            // inherit some flags and service details from parent (if any)
            var flags = default(RequestFlags);
            if (!preResolveParent.IsEmpty)
            {
                serviceInfo = serviceInfo.InheritInfoFromDependencyOwner(preResolveParent.ServiceInfo,
                    ownerType: preResolveParent.FactoryType, container: container);

                // filter out not propagated flags
                flags = preResolveParent.Flags & ~NotInheritedFlags;
            }

            return new Request(resolverContext, _empty, serviceInfo, null, null, null, flags);
        }

        /// <summary>Indicates that request is empty initial request: there is no <see cref="RequestInfo"/> in such a request.</summary>
        public bool IsEmpty { get { return RawParent == null; } }

        /// <summary>Request parent with all runtime info available.</summary>
        public readonly Request RawParent;

        /// <summary>Returns true if request is First in Resolve call.</summary>
        public bool IsResolutionCall { get { return !IsEmpty && RawParent.IsEmpty; } }

        /// <summary>Returns true if request is First in First Resolve call.</summary>
        public bool IsResolutionRoot { get { return IsResolutionCall && PreResolveParent.IsEmpty; } }

        /// <summary>Request prior to Resolve call.</summary>
        public RequestInfo PreResolveParent { get { return _requestContext.PreResolveParent; } }

        /// <summary>Checks if request is wrapped in Func,
        ///  where Func is one of request immediate wrappers.</summary>
        /// <returns>True if has Func ancestor.</returns>
        public bool IsWrappedInFunc()
        {
            return (_flags & RequestFlags.IsWrappedInFunc) != 0;
        }

        /// <summary>Checks if request has parent with service type of Func with arguments.</summary>
        /// <param name="immediateParent">If set indicate to check for immediate parent only,
        /// otherwise will check whole parent chain.</param>
        /// <returns>True if has Func with arguments ancestor.</returns>
        public bool IsWrappedInFuncWithArgs(bool immediateParent = false)
        {
            if ((_flags & RequestFlags.IsWrappedInFuncWithArgs) != 0)
            {
                if (!immediateParent)
                    return true; // skip other checks

                // first run-time parent
                if (!RawParent.IsEmpty)
                    return (RawParent._flags & RequestFlags.IsWrappedInFuncWithArgs) == 0;

                // and if run-time parent does not exist then check the pre-resolve parent
                if (!PreResolveParent.IsEmpty)
                    return (PreResolveParent.Flags & RequestFlags.IsWrappedInFuncWithArgs) != 0;
            }

            return false;
        }

        /// <summary>Indicates that requested service is transient disposable that should be tracked.</summary>
        public bool TracksTransientDisposable
        {
            get { return (_flags & RequestFlags.TracksTransientDisposable) != 0; }
        }

        /// <summary>Indicates the request is singleton or has singleton upper in dependency chain.</summary>
        public bool IsSingletonOrDependencyOfSingleton
        {
            get { return (_flags & RequestFlags.IsSingletonOrDependencyOfSingleton) != 0; }
        }

        /// <summary>Gathers the info from resolved dependency graph. 
        /// If dependency injected <c>asResolutionCall</c> the whole graph is not cacheable (issue #416).</summary>
        /// <returns>True if contains, false - otherwise or if not known.</returns>
        public bool ContainsNestedResolutionCall
        {
            get { return _requestContext.ContainsNestedResolutionCall; }
            set { if (value) _requestContext.ContainsNestedResolutionCall = true; }
        }

        /// <summary>Provides approximate number of dependencies in resolution graph (starting from Resolve method), 
        /// excluding registered delegates, instances, and wrappers.</summary>
        public int DependencyCount { get { return _requestContext.DependencyCount; } }

        /// <summary>Returns true if object graph should be split due <see cref="DryIoc.Rules.MaxObjectGraphSize"/> setting.</summary>
        /// <returns>True if should be split, and false otherwise.</returns>
        public bool ShouldSplitObjectGraph()
        {
            if (FactoryType != FactoryType.Service)
                return false;
            var maxObjectGraphSize = Rules.MaxObjectGraphSize;
            return maxObjectGraphSize != -1 && DependencyCount > maxObjectGraphSize;
        }

        /// <summary>Returns service parent of request, skipping intermediate wrappers if any.</summary>
        public RequestInfo Parent { get { return RequestInfo.Parent; } }

        /// <summary>Returns direct parent either it service or not (wrapper).
        /// In comparison with logical <see cref="Parent"/> which returns first service parent skipping wrapper if any.</summary>
        public RequestInfo ParentOrWrapper { get { return RequestInfo.ParentOrWrapper; } }

        /// <summary>Provides access to container currently bound to request.
        /// By default it is container initiated request by calling resolve method,
        /// but could be changed along the way: for instance when resolving from parent container.</summary>
        public IContainer Container { get { return _requestContext.Container; } }

        /// <summary>Shortcut to issued container rules.</summary>
        public Rules Rules { get { return _requestContext.Container.Rules; } }

        /// <summary>Separate from container because while container may be switched from parent to child, scopes should be from child/facade.</summary>
        public IScopeAccess Scopes { get { return _requestContext.Scopes; } }

        /// <summary>Singletons</summary>
        public IScope SingletonScope { get { return ((Container)_requestContext.Container).SingletonScope; } }

        /// <summary>Weak reference to container. May be replaced in request flowed from parent to child container.</summary>
        public ContainerWeakRef ContainerWeakRef { get { return _requestContext.Container.ContainerWeakRef; } }

        /// <summary>Resolution scope.</summary>
        public IScope Scope { get { return _requestContext.Scope; } }

        /// <summary>(optional) Made spec used for resolving request.</summary>
        public Made Made { get { return Factory == null ? null : Factory.Made; } }

        /// <summary>User provided arguments: key tracks what args are still unused.</summary>
        /// <remarks>Mutable: tracks used arguments</remarks>
        public readonly KV<bool[], ParameterExpression[]> FuncArgs;

        /// <summary>Requested service type.</summary>
        public Type ServiceType { get { return _serviceInfo.ServiceType; } }

        /// <summary>Optional service key to identify service of the same type.</summary>
        public object ServiceKey { get { return _serviceInfo.Details.ServiceKey; } }

        /// <summary>Metadata key to find in metadata dictionary in resolved service.</summary>
        public string MetadataKey { get { return _serviceInfo.Details.MetadataKey; } }

        /// <summary>Metadata or the value (if key specified) to find in resolved service.</summary>
        public object Metadata { get { return _serviceInfo.Details.Metadata; } }

        /// <summary>Policy to deal with unresolved service.</summary>
        public IfUnresolved IfUnresolved { get { return _serviceInfo.Details.IfUnresolved; } }

        /// <summary>Required service type if specified.</summary>
        public Type RequiredServiceType { get { return _serviceInfo.Details.RequiredServiceType; } }

        /// <summary>Resolved factory, initially is null.</summary>
        public readonly Factory Factory;

        /// <summary>Implementation FactoryID.</summary>
        /// <remarks>The default unassigned value of ID is 0.</remarks>
        public int FactoryID { get { return Factory.ThrowIfNull().FactoryID; } }

        /// <summary>Type of factory: Service, Wrapper, or Decorator.</summary>
        public FactoryType FactoryType { get { return Factory.ThrowIfNull().FactoryType; } }

        /// <summary>Service implementation type if known.</summary>
        public Type ImplementationType { get { return Factory.ThrowIfNull().ImplementationType; } }

        /// <summary>Service reuse.</summary>
        public IReuse Reuse { get { return _reuse; } }

        /// <summary>Relative number representing reuse lifespan.</summary>
        public int ReuseLifespan { get { return Reuse == null ? 0 : Reuse.Lifespan; } }

        /// <summary>Returns result of <see cref="DryIoc.RequestInfo.GetActualServiceType"/>>.</summary>
        /// <returns>The type to be used for lookup in registry.</returns>
        public Type GetActualServiceType()
        {
            return _serviceInfo.GetActualServiceType();
        }

        /// <summary>Returns known implementation, or otherwise actual service type.</summary> 
        /// <returns>The subject.</returns>
        public Type GetKnownImplementationOrServiceType()
        {
            return ImplementationType ?? GetActualServiceType();
        }

        /// <summary>Creates new request with provided info, and attaches current request as new request parent.</summary>
        /// <param name="info">Info about service to resolve.</param>
        /// <param name="flags">(optional) Pushed flags.</param>
        /// <returns>New request for provided info.</returns>
        /// <remarks>Existing/parent request should be resolved to factory (<see cref="WithResolvedFactory"/>), before pushing info into it.</remarks>
        public Request Push(IServiceInfo info, RequestFlags flags = default(RequestFlags))
        {
            info.ThrowIfNull();

            if (Factory == null)
                Throw.It(Error.PushingToRequestWithoutFactory, info, this);

            var parentInfo = ChangeIfUnresolvedForCollectionServiceDependency();

            var inheritedInfo = info.InheritInfoFromDependencyOwner(parentInfo, ownerType: FactoryType, container: Container);
            var inheritedFlags = _flags & ~NotInheritedFlags | flags;

            return new Request(_requestContext, this, inheritedInfo, null, null, FuncArgs, inheritedFlags);
        }

        // todo: v3: review and remove if possible
        // if service info is dependency of service wrapped in collection,
        // then change policy to collection policy
        private IServiceInfo ChangeIfUnresolvedForCollectionServiceDependency()
        {
            if (IfUnresolved == IfUnresolved.ReturnDefault && FactoryType == FactoryType.Service)
            {
                var p = RequestInfo;
                while (!p.IsEmpty &&
                       !(p.FactoryType == FactoryType.Wrapper && p.GetActualServiceType().IsSupportedCollectionType()))
                    p = p.ParentOrWrapper;

                if (!p.IsEmpty && p.IfUnresolved == IfUnresolved.Throw)
                    return ServiceInfo.Of(ServiceType, RequiredServiceType, IfUnresolved.Throw, ServiceKey,
                        MetadataKey, Metadata);
            }

            return _serviceInfo;
        }

        /// <summary>Composes service description into <see cref="IServiceInfo"/> and calls Push.</summary>
        /// <param name="serviceType">Service type to resolve.</param>
        /// <param name="serviceKey">(optional) Service key to resolve.</param>
        /// <param name="ifUnresolved">(optional) Instructs how to handle unresolved service.</param>
        /// <param name="requiredServiceType">(optional) Registered/unwrapped service type to find.</param>
        /// <param name="scope">(optional) Resolution scope.</param>
        /// <param name="preResolveParent">(optional) Request info preceding Resolve call.</param>
        /// <param name="flags">(optional) Sets some flags.</param>
        /// <returns>New request with provided info.</returns>
        public Request Push(Type serviceType,
            object serviceKey = null, IfUnresolved ifUnresolved = IfUnresolved.Throw, Type requiredServiceType = null,
            IScope scope = null, RequestInfo preResolveParent = null, RequestFlags flags = default(RequestFlags))
        {
            serviceType.ThrowIfNull()
                .ThrowIf(serviceType.IsOpenGeneric(), Error.ResolvingOpenGenericServiceTypeIsNotPossible);

            var serviceInfo = ServiceInfo.Of(serviceType, requiredServiceType, ifUnresolved, serviceKey);
            return Push(serviceInfo, flags);
        }

        /// <summary>Allow to switch current service info to new one: for instance it is used be decorators.</summary>
        /// <param name="getInfo">Gets new info to switch to.</param>
        /// <returns>New request with new service info but the same implementation and context.</returns>
        public Request WithChangedServiceInfo(Func<IServiceInfo, IServiceInfo> getInfo)
        {
            var newInfo = getInfo(_serviceInfo);
            if (newInfo == _serviceInfo)
                return this;
            return new Request(_requestContext, RawParent, getInfo(_serviceInfo), Factory, _reuse, FuncArgs, _flags);
        }

        // todo: v3: Try to removed in future versions.
        /// <summary>Sets service key to passed value. Required for multiple default services to change null key to
        /// actual <see cref="DefaultKey"/></summary>
        /// <param name="serviceKey">Key to set.</param>
        public void ChangeServiceKey(object serviceKey)
        {
            var i = _serviceInfo;
            var d = i.Details;
            var newDetails = ServiceDetails.Of(d.RequiredServiceType, serviceKey, d.IfUnresolved, d.DefaultValue);
            _serviceInfo = i.Create(i.ServiceType, newDetails);
        }

        /// <summary>Adds input argument expression list to request. 
        /// The arguments are provided by Func and Action wrappers.</summary>
        /// <param name="argExpressions">Argument parameter expressions.</param> <returns>New request.</returns>
        public Request WithArgs(ParameterExpression[] argExpressions)
        {
            var argsUsed = new bool[argExpressions.Length];
            var argsInfo = new KV<bool[], ParameterExpression[]>(argsUsed, argExpressions);
            return new Request(_requestContext, RawParent, _serviceInfo, Factory, _reuse, argsInfo, _flags);
        }

        // todo: v3: remove
        /// <summary>Obsolete: replaced with <see cref="WithArgs"/>.</summary>
        public Request WithFuncArgs(Type funcType)
        {
            var openGenType = funcType.GetGenericDefinitionOrNull().ThrowIfNull();

            var funcIndex = WrappersSupport.FuncTypes.IndexOf(openGenType);
            var actionIndex = funcIndex != -1 ? -1 : WrappersSupport.ActionTypes.IndexOf(openGenType);
            Throw.If(funcIndex < 1 && actionIndex < 1);

            var argTypes = funcType.GetGenericParamsAndArgs();
            var argCount = funcIndex > 0 ? argTypes.Length - 1 : argTypes.Length;

            var argExprs = new ParameterExpression[argCount];
            for (var i = 0; i < argCount; ++i)
            {
                var argType = argTypes[i];
                var argName = "_" + argType.Name + i; // Valid unique argument names for code generation
                argExprs[i] = Expression.Parameter(argType, argName);
            }

            var argsUsed = new bool[argExprs.Length];
            var argsInfo = new KV<bool[], ParameterExpression[]>(argsUsed, argExprs);
            return new Request(_requestContext, RawParent, _serviceInfo, Factory, _reuse, argsInfo, _flags);
        }

        /// <summary>Changes container to passed one. Could be used by child container,
        /// to switch child container to parent preserving the rest of request state.</summary>
        /// <param name="newContainer">Reference to container to switch to.</param>
        /// <returns>Request with replaced container.</returns>
        public Request WithNewContainer(ContainerWeakRef newContainer)
        {
            var newContext = _requestContext.With(newContainer.Container);
            return new Request(newContext, RawParent, _serviceInfo, Factory, _reuse, FuncArgs, _flags);
        }

        /// <summary>Returns new request with set implementation details.</summary>
        /// <param name="factory">Factory to which request is resolved.</param>
        /// <param name="skipRecursiveDependencyCheck">(optional) does not check for recursive dependency. 
        /// Use with caution. Make sense for Resolution expression.</param>
        /// <param name="skipCaptiveDependencyCheck">(optional) allows to skip captive dependency check.</param>
        /// <returns>New request with set factory.</returns>
        public Request WithResolvedFactory(Factory factory,
            bool skipRecursiveDependencyCheck = false,
            bool skipCaptiveDependencyCheck = false)
        {
            if (IsEmpty || Factory != null && Factory.FactoryID == factory.FactoryID)
                return this; // resolving only once, no need to check recursion again.

            if (factory.FactoryType == FactoryType.Service && !skipRecursiveDependencyCheck)
                for (var p = RawParent; !p.IsEmpty; p = p.RawParent)
                    if (p.FactoryID == factory.FactoryID)
                        Throw.It(Error.RecursiveDependencyDetected, Print(factory.FactoryID));

            var reuse = factory.Reuse;
            if (reuse == null)
                reuse = GetDefaultReuse(factory);

            var flags = _flags;

            if (!skipCaptiveDependencyCheck && reuse.Lifespan != 0 &&
                Rules.ThrowIfDependencyHasShorterReuseLifespan)
                ThrowIfReuseHasShorterLifespanThanParent(reuse);

            if (reuse == DryIoc.Reuse.Singleton)
                flags |= RequestFlags.IsSingletonOrDependencyOfSingleton;
            // check for disposable transient
            else if (reuse == DryIoc.Reuse.Transient)
            {
                reuse = GetTransientDisposableTrackingReuse(factory);
                if (reuse != DryIoc.Reuse.Transient)
                    flags |= RequestFlags.TracksTransientDisposable;
            }

            _requestContext.IncrementDependencyCount();
            return new Request(_requestContext, RawParent, _serviceInfo, factory, reuse, FuncArgs, flags);
        }

        private IReuse GetDefaultReuse(Factory factory)
        {
            if (factory.Setup.UseParentReuse)
                return GetFirstParentNonTransientReuseUntilFunc();

            if (factory.Setup.FactoryType == FactoryType.Decorator
                && ((Setup.DecoratorSetup)factory.Setup).UseDecorateeReuse)
                return Reuse; // use reuse of resolved service factory for decorator

            // if no specified the wrapper reuse is always Transient,
            // other container-wide default reuse is applied
            return factory.FactoryType == FactoryType.Wrapper
                ? DryIoc.Reuse.Transient
                : Container.Rules.DefaultReuseInsteadOfTransient;
        }

        private IReuse GetTransientDisposableTrackingReuse(Factory factory)
        {
            // Track transient disposable in parent scope (if any), or open scope (if any)
            var setup = factory.Setup;
            var tracksTransientDisposable =
                !setup.PreventDisposal &&
                (setup.TrackDisposableTransient || !setup.AllowDisposableTransient && Rules.TrackingDisposableTransients) &&
                (factory.ImplementationType ?? GetActualServiceType()).IsAssignableTo(typeof(IDisposable));

            if (!tracksTransientDisposable)
                return DryIoc.Reuse.Transient;

            var parentReuse = GetFirstParentNonTransientReuseUntilFunc();
            if (parentReuse != DryIoc.Reuse.Transient)
                return parentReuse;

            if (IsWrappedInFunc())
                return DryIoc.Reuse.Transient;

            // If no parent with reuse found, then track in current open scope or in singletons scope
            return DryIoc.Reuse.ScopedOrSingleton;
        }

        private void ThrowIfReuseHasShorterLifespanThanParent(IReuse reuse)
        {
            if (!RawParent.IsEmpty)
                for (var p = RawParent; !p.IsEmpty; p = p.RawParent)
                {
                    if (p.FactoryType == FactoryType.Wrapper && p.GetActualServiceType().IsFunc())
                        break;

                    if (p.FactoryType == FactoryType.Service && p.ReuseLifespan > reuse.Lifespan)
                        Throw.It(Error.DependencyHasShorterReuseLifespan, PrintCurrent(), reuse, p);
                }

            if (!PreResolveParent.IsEmpty)
            {
                for (var p = PreResolveParent; !p.IsEmpty; p = p.ParentOrWrapper)
                {
                    if (p.FactoryType == FactoryType.Wrapper && p.GetActualServiceType().IsFunc())
                        break;

                    if (p.FactoryType == FactoryType.Service && p.ReuseLifespan > reuse.Lifespan)
                        Throw.It(Error.DependencyHasShorterReuseLifespan, PrintCurrent(), reuse, p);
                }
            }
        }

        private IReuse GetFirstParentNonTransientReuseUntilFunc()
        {
            if (!RawParent.IsEmpty)
                for (var p = RawParent; !p.IsEmpty; p = p.RawParent)
                {
                    if (p.FactoryType == FactoryType.Wrapper && p.GetActualServiceType().IsFunc())
                        return DryIoc.Reuse.Transient;

                    if (p.FactoryType != FactoryType.Wrapper && p.Reuse != DryIoc.Reuse.Transient)
                        return p.Reuse;
                }

            if (!PreResolveParent.IsEmpty)
                for (var p = PreResolveParent; !p.IsEmpty; p = p.ParentOrWrapper)
                {
                    if (p.FactoryType == FactoryType.Wrapper && p.GetActualServiceType().IsFunc())
                        return DryIoc.Reuse.Transient;

                    if (p.FactoryType != FactoryType.Wrapper && p.Reuse != DryIoc.Reuse.Transient)
                        return p.Reuse;
                }

            return DryIoc.Reuse.Transient;
        }

        /// <summary>Serializable request info stripped off run-time info.</summary>
        public RequestInfo RequestInfo
        {
            get
            {
                if (IsEmpty)
                    return PreResolveParent;

                var parentRequestInfo = RawParent.IsEmpty ? PreResolveParent : RawParent.RequestInfo;
                if (Factory == null)
                    return parentRequestInfo.Push(_serviceInfo);

                return parentRequestInfo.Push(_serviceInfo,
                    Factory.FactoryID, Factory.FactoryType, Factory.ImplementationType, _reuse);
            }
        }

        // todo: v3: remove
        /// <summary>Obsolete: use <see cref="RequestInfo"/> instead.</summary>
        public RequestInfo ToRequestInfo()
        {
            return RequestInfo;
        }

        /// <summary>If request corresponds to dependency injected into parameter,
        /// then method calls <paramref name="parameter"/> handling and returns its result.
        /// If request corresponds to property or field, then method calls respective handler.
        /// If request does not correspond to dependency, then calls <paramref name="root"/> handler.</summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="root">(optional) handler for resolution call or root.</param>
        /// <param name="parameter">(optional) handler for parameter dependency</param>
        /// <param name="property">(optional) handler for property dependency</param>
        /// <param name="field">(optional) handler for field dependency</param>
        /// <returns>Result of applied handler or default <typeparamref name="TResult"/>.</returns>
        public TResult Is<TResult>(
            Func<TResult> root = null,
            Func<ParameterInfo, TResult> parameter = null,
            Func<PropertyInfo, TResult> property = null,
            Func<FieldInfo, TResult> field = null)
        {
            var serviceInfo = _serviceInfo;
            if (serviceInfo is ParameterServiceInfo)
            {
                if (parameter != null)
                    return parameter(((ParameterServiceInfo)serviceInfo).Parameter);
            }
            else if (serviceInfo is PropertyOrFieldServiceInfo)
            {
                var propertyOrFieldServiceInfo = (PropertyOrFieldServiceInfo)serviceInfo;
                var propertyInfo = propertyOrFieldServiceInfo.Member as PropertyInfo;
                if (propertyInfo != null)
                {
                    if (property != null)
                        return property(propertyInfo);
                }
                else if (field != null)
                    return field((FieldInfo)propertyOrFieldServiceInfo.Member);
            }
            else if (root != null)
                return root();

            return default(TResult);
        }

        /// <summary>Enumerates all request stack parents.
        /// Last returned will <see cref="IsEmpty"/> empty parent.</summary>
        /// <returns>Unfolding parents.</returns>
        public IEnumerable<Request> Enumerate()
        {
            for (var r = this; !r.IsEmpty; r = r.RawParent)
                yield return r;
        }

        /// <summary>Prints current request info only (no parents printed) to provided builder.</summary>
        /// <param name="s">Builder to print too.</param>
        /// <returns>(optional) Builder to appended info to, or new builder if not specified.</returns>
        public StringBuilder PrintCurrent(StringBuilder s = null)
        {
            s = s ?? new StringBuilder();

            if (IsEmpty)
                return s.Append("{empty}");

            if (Factory != null)
            {
                if (_reuse != DryIoc.Reuse.Transient)
                    s.Append(Reuse is SingletonReuse ? "singleton" : "scoped").Append(' ');

                var factoryType = Factory.FactoryType;
                if (factoryType != FactoryType.Service)
                    s.Append(factoryType.ToString().ToLower()).Append(' ');

                var implType = Factory.ImplementationType;
                if (implType != null && implType != ServiceType)
                    s.Print(implType).Append(": ");
            }

            s.Append(_serviceInfo);

            if (FuncArgs != null)
                s.AppendFormat(" with {0} arg(s) ", FuncArgs.Key.Count(k => k == false));

            return s;
        }

        /// <summary>Prints full stack of requests starting from current one using <see cref="PrintCurrent"/>.</summary>
        /// <param name="recursiveFactoryID">Flag specifying that in case of found recursion/repetition of requests,
        /// mark repeated requests.</param>
        /// <returns>Builder with appended request stack info.</returns>
        public StringBuilder Print(int recursiveFactoryID = -1)
        {
            if (IsEmpty)
                return new StringBuilder("<empty>");

            var s = PrintCurrent(new StringBuilder());

            s = recursiveFactoryID == -1 ? s : s.Append(" <--recursive");
            foreach (var r in RawParent.Enumerate())
            {
                s = r.PrintCurrent(s.AppendLine().Append("  in "));
                if (r.FactoryID == recursiveFactoryID)
                    s = s.Append(" <--recursive");
            }

            if (!PreResolveParent.IsEmpty)
                s = s.AppendLine().Append("  in ").Append(PreResolveParent);

            return s;
        }

        /// <summary>Print while request stack info to string using <seealso cref="Print"/>.</summary>
        /// <returns>String with info.</returns>
        public override string ToString()
        {
            return Print().ToString();
        }

        #region Implementation

        private Request(RequestContext requestContext, Request parent, IServiceInfo serviceInfo,
            Factory factory, IReuse reuse,
            KV<bool[], ParameterExpression[]> funcArgs, RequestFlags flags)
        {
            _requestContext = requestContext;
            RawParent = parent;
            _serviceInfo = serviceInfo;
            Factory = factory;
            _reuse = reuse;
            FuncArgs = funcArgs;
            _flags = flags;
        }

        private IServiceInfo _serviceInfo;
        private readonly IReuse _reuse;

        private readonly RequestFlags _flags;

        private readonly RequestContext _requestContext;

        internal sealed class RequestContext
        {
            public readonly IContainer Container;
            public readonly IScopeAccess Scopes;
            public readonly IScope Scope;
            public readonly RequestInfo PreResolveParent;

            // Mutable updatable part
            public bool ContainsNestedResolutionCall;
            public int DependencyCount;

            public RequestContext(IContainer container, IScopeAccess scopes, IScope scope, RequestInfo preResolveParent)
            {
                Container = container;
                Scopes = scopes;
                Scope = scope;
                PreResolveParent = preResolveParent;
            }

            public RequestContext With(IContainer newContainer)
            {
                return new RequestContext(newContainer, Scopes, Scope, PreResolveParent);
            }

            public void IncrementDependencyCount()
            {
                Interlocked.Increment(ref DependencyCount);
            }
        }

        #endregion
    }

    /// <summary>Type of services supported by Container.</summary>
    internal enum FactoryType
    {
        /// <summary>(default) Defines normal service factory</summary>
        Service,
        /// <summary>Defines decorator factory</summary>
        Decorator,
        /// <summary>Defines wrapper factory.</summary>
        Wrapper
    };

    /// <summary>Base class to store optional <see cref="Factory"/> settings.</summary>
    internal abstract class Setup
    {
        /// <summary>Factory type is required to be specified by concrete setups as in
        /// <see cref="ServiceSetup"/>, <see cref="DecoratorSetup"/>, <see cref="WrapperSetup"/>.</summary>
        public abstract FactoryType FactoryType { get; }

        /// <summary>Predicate to check if factory could be used for resolved request.</summary>
        public virtual Func<Request, bool> Condition { get; private set; }

        /// <summary>Arbitrary metadata object associated with Factory/Implementation.</summary>
        public virtual object Metadata { get { return null; } }

        /// <summary>Indicates that injected expression should be:
        /// <c><![CDATA[r.Resolver.Resolve<IDependency>(...)]]></c>
        /// instead of: <c><![CDATA[new Dependency(...)]]></c></summary>
        public bool AsResolutionCall { get { return (_settings & Settings.AsResolutionCall) != 0; } }

        internal Setup WithAsResolutionCall()
        {
            var copy = (Setup)MemberwiseClone();
            copy._settings |= Settings.AsResolutionCall;
            return copy;
        }

        /// <summary>Marks service (not a wrapper or decorator) registration that is expected to be resolved via Resolve call.</summary>
        public bool AsResolutionRoot { get { return (_settings & Settings.AsResolutionRoot) != 0; } }

        /// <summary>In addition to <see cref="AsResolutionCall"/> opens scope.</summary>
        public bool OpenResolutionScope { get { return (_settings & Settings.OpenResolutionScope) != 0; } }

        /// <summary>Prevents disposal of reused instance if it is disposable.</summary>
        public bool PreventDisposal { get { return (_settings & Settings.PreventDisposal) != 0; } }

        /// <summary>Stores reused instance as WeakReference.</summary>
        public bool WeaklyReferenced { get { return (_settings & Settings.WeaklyReferenced) != 0; } }

        /// <summary>Allows registering transient disposable.</summary>
        public bool AllowDisposableTransient { get { return (_settings & Settings.AllowDisposableTransient) != 0; } }

        /// <summary>Turns On tracking of disposable transient dependency in parent scope or in open scope if resolved directly.</summary>
        public bool TrackDisposableTransient { get { return (_settings & Settings.TrackDisposableTransient) != 0; } }

        /// <summary>Instructs to use parent reuse. Applied only if <see cref="Factory.Reuse"/> is not specified.</summary>
        public bool UseParentReuse { get { return (_settings & Settings.UseParentReuse) != 0; } }

        /// <summary>Returns true if passed meta key and value match the setup metadata.</summary>
        /// <param name="metadataKey">Required metadata key</param> <param name="metadata">Required metadata or the value if key passed.</param>
        /// <returns>Check result.</returns>
        public bool MatchesMetadata(string metadataKey, object metadata)
        {
            if (metadataKey == null)
                return Equals(metadata, Metadata);

            object metaValue;
            var metaDict = Metadata as IDictionary<string, object>;
            return metaDict != null
                   && metaDict.TryGetValue(metadataKey, out metaValue)
                   && Equals(metadata, metaValue);
        }

        /// <summary>Default setup for service factories.</summary>
        public static readonly Setup Default = new ServiceSetup();

        /// <summary>Sets the base settings.</summary>
        /// <param name="condition"></param>
        /// <param name="openResolutionScope"></param> <param name="asResolutionCall"></param>
        /// <param name="asResolutionRoot"></param> <param name="preventDisposal"></param>
        /// <param name="weaklyReferenced"></param> <param name="allowDisposableTransient"></param>
        /// <param name="trackDisposableTransient"></param> <param name="useParentReuse"></param>
        private Setup(Func<Request, bool> condition = null,
            bool openResolutionScope = false, bool asResolutionCall = false,
            bool asResolutionRoot = false, bool preventDisposal = false, bool weaklyReferenced = false,
            bool allowDisposableTransient = false, bool trackDisposableTransient = false,
            bool useParentReuse = false)
        {
            Condition = condition;

            if (asResolutionCall)
                _settings |= Settings.AsResolutionCall;
            if (openResolutionScope)
            {
                _settings |= Settings.OpenResolutionScope;
                _settings |= Settings.AsResolutionCall;
            }
            if (preventDisposal)
                _settings |= Settings.PreventDisposal;
            if (weaklyReferenced)
                _settings |= Settings.WeaklyReferenced;
            if (allowDisposableTransient)
                _settings |= Settings.AllowDisposableTransient;
            if (trackDisposableTransient)
            {
                _settings |= Settings.TrackDisposableTransient;
                _settings |= Settings.AllowDisposableTransient;
            }
            if (asResolutionRoot)
                _settings |= Settings.AsResolutionRoot;
            if (useParentReuse)
                _settings |= Settings.UseParentReuse;
        }

        [Flags]
        private enum Settings
        {
            AsResolutionCall = 1 << 1,
            OpenResolutionScope = 1 << 2,
            PreventDisposal = 1 << 3,
            WeaklyReferenced = 1 << 4,
            AllowDisposableTransient = 1 << 5,
            TrackDisposableTransient = 1 << 6,
            AsResolutionRoot = 1 << 7,
            UseParentReuse = 1 << 8
        }

        private Settings _settings; // note: it is mutable because of setting the AsResolutionCall

        /// <summary>Constructs setup object out of specified settings. If all settings are default then <see cref="Setup.Default"/> setup will be returned.</summary>
        /// <param name="metadataOrFuncOfMetadata">(optional) Metadata object or Func returning metadata object.</param> 
        /// <param name="condition">(optional)</param>
        /// <param name="openResolutionScope">(optional) Same as <paramref name="asResolutionCall"/> but in addition opens new scope.</param>
        /// <param name="asResolutionCall">(optional) If true dependency expression will be "r.Resolve(...)" instead of inline expression.</param>
        /// <param name="asResolutionRoot">(optional) Marks service (not a wrapper or decorator) registration that is expected to be resolved via Resolve call.</param>
        /// <param name="preventDisposal">(optional) Prevents disposal of reused instance if it is disposable.</param>
        /// <param name="weaklyReferenced">(optional) Stores reused instance as WeakReference.</param>
        /// <param name="allowDisposableTransient">(optional) Allows registering transient disposable.</param>
        /// <param name="trackDisposableTransient">(optional) Turns On tracking of disposable transient dependency in parent scope or in open scope if resolved directly.</param>
        /// <param name="useParentReuse">(optional) Instructs to use parent reuse. Applied only if <see cref="Factory.Reuse"/> is not specified.</param>
        /// <returns>New setup object or <see cref="Setup.Default"/>.</returns>
        public static Setup With(
            object metadataOrFuncOfMetadata = null, Func<RequestInfo, bool> condition = null,
            bool openResolutionScope = false, bool asResolutionCall = false, bool asResolutionRoot = false,
            bool preventDisposal = false, bool weaklyReferenced = false,
            bool allowDisposableTransient = false, bool trackDisposableTransient = false,
            bool useParentReuse = false)
        {
            var requestCondition = condition == null ? null : new Func<Request, bool>(r => condition(r.RequestInfo));
            return With(metadataOrFuncOfMetadata, requestCondition,
                openResolutionScope, asResolutionCall, asResolutionRoot,
                preventDisposal, weaklyReferenced, allowDisposableTransient, trackDisposableTransient,
                useParentReuse);
        }

        /// <summary>Constructs setup object out of specified settings. If all settings are default then <see cref="Setup.Default"/> setup will be returned.</summary>
        /// <param name="metadataOrFuncOfMetadata">Metadata object or Func returning metadata object.</param> 
        /// <param name="condition"></param>
        /// <param name="openResolutionScope">(optional) Same as <paramref name="asResolutionCall"/> but in addition opens new scope.</param>
        /// <param name="asResolutionCall">(optional) If true dependency expression will be "r.Resolve(...)" instead of inline expression.</param>
        /// <param name="asResolutionRoot">(optional) Marks service (not a wrapper or decorator) registration that is expected to be resolved via Resolve call.</param>
        /// <param name="preventDisposal">(optional) Prevents disposal of reused instance if it is disposable.</param>
        /// <param name="weaklyReferenced">(optional) Stores reused instance as WeakReference.</param>
        /// <param name="allowDisposableTransient">(optional) Allows registering transient disposable.</param>
        /// <param name="trackDisposableTransient">(optional) Turns On tracking of disposable transient dependency in parent scope or in open scope if resolved directly.</param>
        /// <param name="useParentReuse">(optional) Instructs to use parent reuse. Applied only if <see cref="Factory.Reuse"/> is not specified.</param>
        /// <returns>New setup object or <see cref="Setup.Default"/>.</returns>
        public static Setup With(
            object metadataOrFuncOfMetadata, Func<Request, bool> condition,
            bool openResolutionScope = false, bool asResolutionCall = false, bool asResolutionRoot = false,
            bool preventDisposal = false, bool weaklyReferenced = false,
            bool allowDisposableTransient = false, bool trackDisposableTransient = false,
            bool useParentReuse = false)
        {
            if (metadataOrFuncOfMetadata == null && condition == null &&
                openResolutionScope == false && asResolutionCall == false && asResolutionRoot == false &&
                preventDisposal == false && weaklyReferenced == false &&
                allowDisposableTransient == false && trackDisposableTransient == false &&
                useParentReuse == false)
                return Default;

            return new ServiceSetup(condition,
                metadataOrFuncOfMetadata, openResolutionScope, asResolutionCall, asResolutionRoot,
                preventDisposal, weaklyReferenced, allowDisposableTransient, trackDisposableTransient,
                useParentReuse);
        }

        /// <summary>Default setup which will look for wrapped service type as single generic parameter.</summary>
        public static readonly Setup Wrapper = new WrapperSetup();

        /// <summary>Returns generic wrapper setup.</summary>
        /// <param name="condition">(optional)</param>
        /// <param name="wrappedServiceTypeArgIndex">Default is -1 for generic wrapper with single type argument. Need to be set for multiple type arguments.</param>
        /// <param name="alwaysWrapsRequiredServiceType">Need to be set when generic wrapper type arguments should be ignored.</param>
        /// <param name="unwrap">(optional) Delegate returning wrapped type from wrapper type. <b>Overwrites other options.</b></param>
        /// <param name="openResolutionScope">(optional) Opens the new scope.</param>
        /// <param name="asResolutionCall">(optional) Injects decorator as resolution call.</param>
        /// <param name="preventDisposal">(optional) Prevents disposal of reused instance if it is disposable.</param>
        /// <returns>New setup or default <see cref="Setup.Wrapper"/>.</returns>
        public static Setup WrapperWith(Func<Request, bool> condition,
            int wrappedServiceTypeArgIndex = -1, bool alwaysWrapsRequiredServiceType = false, Func<Type, Type> unwrap = null,
            bool openResolutionScope = false, bool asResolutionCall = false, bool preventDisposal = false)
        {
            if (wrappedServiceTypeArgIndex == -1 && !alwaysWrapsRequiredServiceType && unwrap == null
                && !openResolutionScope && !preventDisposal && condition == null)
                return Wrapper;

            return new WrapperSetup(wrappedServiceTypeArgIndex, alwaysWrapsRequiredServiceType, unwrap,
                condition, openResolutionScope, asResolutionCall, preventDisposal);
        }

        /// <summary>Returns generic wrapper setup.</summary>
        /// <param name="wrappedServiceTypeArgIndex">Default is -1 for generic wrapper with single type argument. Need to be set for multiple type arguments.</param>
        /// <param name="alwaysWrapsRequiredServiceType">Need to be set when generic wrapper type arguments should be ignored.</param>
        /// <param name="unwrap">(optional) Delegate returning wrapped type from wrapper type. <b>Overwrites other options.</b></param>
        /// <param name="openResolutionScope">(optional) Opens the new scope.</param>
        /// <param name="asResolutionCall">(optional) Injects decorator as resolution call.</param>
        /// <param name="preventDisposal">(optional) Prevents disposal of reused instance if it is disposable.</param>
        /// <param name="condition">(optional)</param>
        /// <returns>New setup or default <see cref="Setup.Wrapper"/>.</returns>
        public static Setup WrapperWith(int wrappedServiceTypeArgIndex = -1,
            bool alwaysWrapsRequiredServiceType = false, Func<Type, Type> unwrap = null,
            bool openResolutionScope = false, bool asResolutionCall = false, bool preventDisposal = false,
            Func<RequestInfo, bool> condition = null)
        {
            return WrapperWith(condition == null ? null : new Func<Request, bool>(r => condition(r.RequestInfo)),
                wrappedServiceTypeArgIndex, alwaysWrapsRequiredServiceType, unwrap,
                openResolutionScope, asResolutionCall, preventDisposal);
        }

        /// <summary>Default decorator setup: decorator is applied to service type it registered with.</summary>
        public static readonly Setup Decorator = new DecoratorSetup();

        /// <summary>Creates setup with optional condition.</summary>
        /// <param name="condition">(optional) Applied to decorated service to find that service is the decorator target.</param>
        /// <param name="order">(optional) If provided specifies relative decorator position in decorators chain.</param>
        /// <param name="useDecorateeReuse">If provided specifies relative decorator position in decorators chain.
        /// Greater number means further from decoratee - specify negative number to stay closer.
        /// Decorators without order (Order is 0) or with equal order are applied in registration order
        /// - first registered are closer decoratee.</param>
        /// <returns>New setup with condition or <see cref="Decorator"/>.</returns>
        public static Setup DecoratorWith(Func<RequestInfo, bool> condition = null, int order = 0,
            bool useDecorateeReuse = false)
        {
            var requestCondition = condition == null ? null : new Func<Request, bool>(r => condition(r.RequestInfo));
            return DecoratorWith(requestCondition, order, useDecorateeReuse);
        }

        /// <summary>Creates setup with optional condition.</summary>
        /// <param name="condition">(optional) Applied to decorated service to find that service is the decorator target.</param>
        /// <param name="order">(optional) If provided specifies relative decorator position in decorators chain.</param>
        /// <param name="useDecorateeReuse">If provided specifies relative decorator position in decorators chain.
        /// Greater number means further from decoratee - specify negative number to stay closer.
        /// Decorators without order (Order is 0) or with equal order are applied in registration order
        /// - first registered are closer decoratee.</param>
        /// <returns>New setup with condition or <see cref="Decorator"/>.</returns>
        public static Setup DecoratorWith(Func<Request, bool> condition, int order, bool useDecorateeReuse)
        {
            if (condition == null && order == 0 && !useDecorateeReuse)
                return Decorator;
            return new DecoratorSetup(condition, order, useDecorateeReuse);
        }

        /// <summary>Service setup.</summary>
        internal sealed class ServiceSetup : Setup
        {
            /// <inheritdoc />
            public override FactoryType FactoryType { get { return FactoryType.Service; } }

            /// <summary>Evaluates metadata if it specified as Func of object, and replaces Func with its result!.
            /// Otherwise just returns metadata object.</summary>
            /// <remarks>Invocation of Func metadata is Not thread-safe. Please take care of that inside the Func.</remarks>
            public override object Metadata
            {
                get
                {
                    return _metadataOrFuncOfMetadata is Func<object>
                        ? (_metadataOrFuncOfMetadata = ((Func<object>)_metadataOrFuncOfMetadata).Invoke())
                        : _metadataOrFuncOfMetadata;
                }
            }

            /// <summary>All settings are set to defaults.</summary>
            public ServiceSetup() { }

            /// <summary>Specify an individual settings</summary>
            public ServiceSetup(Func<Request, bool> condition, object metadataOrFuncOfMetadata,
                bool openResolutionScope, bool asResolutionCall, bool asResolutionRoot,
                bool preventDisposal, bool weaklyReferenced,
                bool allowDisposableTransient, bool trackDisposableTransient,
                bool useParentReuse)
                : base(condition, openResolutionScope, asResolutionCall, asResolutionRoot,
                    preventDisposal, weaklyReferenced, allowDisposableTransient, trackDisposableTransient,
                    useParentReuse)
            {
                _metadataOrFuncOfMetadata = metadataOrFuncOfMetadata;
            }

            private object _metadataOrFuncOfMetadata;
        }

        /// <summary>Setup applied for wrappers.</summary>
        internal sealed class WrapperSetup : Setup
        {
            /// <summary>Returns <see cref="DryIoc.FactoryType.Wrapper"/> type.</summary>
            public override FactoryType FactoryType { get { return FactoryType.Wrapper; } }

            /// <summary>Delegate to get wrapped type from provided wrapper type.
            /// If wrapper is generic, then wrapped type is usually a generic parameter.</summary>
            public readonly int WrappedServiceTypeArgIndex;

            /// <summary>Per name.</summary>
            public readonly bool AlwaysWrapsRequiredServiceType;

            /// <summary>Delegate returning wrapped type from wrapper type. Overwrites other options.</summary>
            public readonly Func<Type, Type> Unwrap;

            /// <summary>Default setup</summary>
            /// <param name="wrappedServiceTypeArgIndex">Default is -1 for generic wrapper with single type argument. 
            /// Need to be set for multiple type arguments.</param>
            public WrapperSetup(int wrappedServiceTypeArgIndex = -1)
            {
                WrappedServiceTypeArgIndex = wrappedServiceTypeArgIndex;
            }

            /// <summary>Constructs wrapper setup from optional wrapped type selector and reuse wrapper factory.</summary>
            /// <param name="wrappedServiceTypeArgIndex">Default is -1 for generic wrapper with single type argument. Need to be set for multiple type arguments.</param>
            /// <param name="alwaysWrapsRequiredServiceType">Need to be set when generic wrapper type arguments should be ignored.</param>
            /// <param name="unwrap">Delegate returning wrapped type from wrapper type.  Overwrites other options.</param>
            /// <param name="openResolutionScope">Opens the new scope.</param><param name="asResolutionCall"></param>
            /// <param name="preventDisposal">Prevents disposal of reused instance if it is disposable.</param>
            /// <param name="condition">Predicate to check if factory could be used for resolved request.</param>
            public WrapperSetup(int wrappedServiceTypeArgIndex, bool alwaysWrapsRequiredServiceType, Func<Type, Type> unwrap,
                Func<Request, bool> condition, bool openResolutionScope, bool asResolutionCall, bool preventDisposal)
                : base(condition, openResolutionScope: openResolutionScope, asResolutionCall: asResolutionCall, preventDisposal: preventDisposal)
            {
                WrappedServiceTypeArgIndex = wrappedServiceTypeArgIndex;
                AlwaysWrapsRequiredServiceType = alwaysWrapsRequiredServiceType;
                Unwrap = unwrap;
            }

            internal void ThrowIfInvalidRegistration(Type serviceType)
            {
                if (AlwaysWrapsRequiredServiceType || Unwrap != null)
                    return;

                if (!serviceType.IsGeneric())
                    return;

                var typeArgCount = serviceType.GetGenericParamsAndArgs().Length;
                var typeArgIndex = WrappedServiceTypeArgIndex;
                Throw.If(typeArgCount > 1 && typeArgIndex == -1,
                    Error.GenericWrapperWithMultipleTypeArgsShouldSpecifyArgIndex, serviceType);

                var index = typeArgIndex != -1 ? typeArgIndex : 0;
                Throw.If(index > typeArgCount - 1,
                    Error.GenericWrapperTypeArgIndexOutOfBounds, serviceType, index);
            }

            /// <summary>Unwraps service type or returns its.</summary>
            /// <param name="serviceType"></param> <returns>Wrapped type or self.</returns>
            public Type GetWrappedTypeOrNullIfWrapsRequired(Type serviceType)
            {
                if (Unwrap != null)
                    return Unwrap(serviceType);

                if (AlwaysWrapsRequiredServiceType || !serviceType.IsGeneric())
                    return null;

                var typeArgs = serviceType.GetGenericParamsAndArgs();
                var typeArgIndex = WrappedServiceTypeArgIndex;
                serviceType.ThrowIf(typeArgs.Length > 1 && typeArgIndex == -1,
                    Error.GenericWrapperWithMultipleTypeArgsShouldSpecifyArgIndex);

                typeArgIndex = typeArgIndex != -1 ? typeArgIndex : 0;
                serviceType.ThrowIf(typeArgIndex > typeArgs.Length - 1,
                    Error.GenericWrapperTypeArgIndexOutOfBounds, typeArgIndex);

                return typeArgs[typeArgIndex];
            }
        }

        /// <summary>Setup applied to decorators.</summary>
        internal sealed class DecoratorSetup : Setup
        {
            /// <summary>Returns Decorator factory type.</summary>
            public override FactoryType FactoryType { get { return FactoryType.Decorator; } }

            /// <summary>If provided specifies relative decorator position in decorators chain.
            /// Greater number means further from decoratee - specify negative number to stay closer.
            /// Decorators without order (Order is 0) or with equal order are applied in registration order
            /// - first registered are closer decoratee.</summary>
            public readonly int Order;

            /// <summary>Instructs to use decorated service reuse. Decorated service may be decorator itself.</summary>
            public readonly bool UseDecorateeReuse;

            /// <summary>Default setup.</summary>
            public DecoratorSetup() { }

            /// <summary>Creates decorator setup with optional condition.</summary>
            /// <param name="condition">(optional) Applied to decorated service to find that service is the decorator target.</param>
            /// <param name="order">(optional) If provided specifies relative decorator position in decorators chain.
            /// Greater number means further from decoratee - specify negative number to stay closer.
            /// Decorators without order (Order is 0) or with equal order are applied in registration order
            /// - first registered are closer decoratee.</param>
            /// <param name="useDecorateeReuse">(optional) Instructs to use decorated service reuse.
            /// Decorated service may be decorator itself.</param>
            public DecoratorSetup(Func<Request, bool> condition, int order, bool useDecorateeReuse)
                : base(condition)
            {
                Order = order;
                UseDecorateeReuse = useDecorateeReuse;
            }
        }
    }

    /// <summary>Facility for creating concrete factories from some template/prototype. Example:
    /// creating closed-generic type reflection factory from registered open-generic prototype factory.</summary>
    internal interface IConcreteFactoryGenerator
    {
        /// <summary>Generated factories so far, identified by the service type and key pair.</summary>
        ImTreeMap<KV<Type, object>, ReflectionFactory> GeneratedFactories { get; }

        /// <summary>Returns factory per request. May track already generated factories and return one without regenerating.</summary>
        /// <param name="request">Request to resolve.</param> 
        /// <param name="ifErrorReturnDefault">If set to true - returns null if unable to generate, 
        /// otherwise error result depends on <see cref="Request.IfUnresolved"/>.</param> 
        /// <returns>Returns new factory per request.</returns>
        Factory GetGeneratedFactory(Request request, bool ifErrorReturnDefault = false);
    }

    /// <summary>Base class for different ways to instantiate service:
    /// <list type="bullet">
    /// <item>Through reflection - <see cref="ReflectionFactory"/></item>
    /// <item>Using custom delegate - <see cref="DelegateFactory"/></item>
    /// <item>Using custom expression - <see cref="ExpressionFactory"/></item>
    /// </list>
    /// For all of the types Factory should provide result as <see cref="Expression"/> and <see cref="FactoryDelegate"/>.
    /// Factories are supposed to be immutable and stateless.
    /// Each created factory has an unique ID set in <see cref="FactoryID"/>.</summary>
    internal abstract class Factory
    {
        /// <summary>Get next factory ID in a atomic way.</summary><returns>The ID.</returns>
        public static int GetNextID()
        {
            return Interlocked.Increment(ref _lastFactoryID);
        }

        /// <summary>Unique factory id generated from static seed.</summary>
        public int FactoryID { get; internal set; }

        /// <summary>Reuse policy for created services.</summary>
        public virtual IReuse Reuse { get { return _reuse; } }

        /// <summary>Setup may contain different/non-default factory settings.</summary>
        public virtual Setup Setup
        {
            get { return _setup; }
            internal set { _setup = value ?? Setup.Default; }
        }

        /// <summary>Checks that condition is met for request or there is no condition setup.
        /// Additionally check for reuse scope availability.</summary>
        /// <param name="request">Request to check against.</param>
        /// <returns>True if condition met or no condition setup.</returns>
        public bool CheckCondition(Request request)
        {
            return (Setup.Condition == null || Setup.Condition(request))
                   && HasMatchingReuseScope(request);
        }

        /// <summary>Shortcut for <see cref="DryIoc.Setup.FactoryType"/>.</summary>
        public FactoryType FactoryType
        {
            get { return Setup.FactoryType; }
        }

        /// <summary>Non-abstract closed implementation type. May be null if not known beforehand, e.g. in <see cref="DelegateFactory"/>.</summary>
        public virtual Type ImplementationType { get { return null; } }

        /// <summary>Allow inheritors to define lazy implementation type</summary>
        public virtual bool CanAccessImplementationType { get { return true; } }

        /// <summary>Indicates that Factory is factory provider and
        /// consumer should call <see cref="IConcreteFactoryGenerator.GetGeneratedFactory"/>  to get concrete factory.</summary>
        public virtual IConcreteFactoryGenerator FactoryGenerator { get { return null; } }

        /// <summary>Settings <b>(if any)</b> to select Constructor/FactoryMethod, Parameters, Properties and Fields.</summary>
        public virtual Made Made { get { return Made.Default; } }

        /// <summary>Initializes reuse and setup. Sets the <see cref="FactoryID"/></summary>
        /// <param name="reuse">(optional)</param> <param name="setup">(optional)</param>
        protected Factory(IReuse reuse = null, Setup setup = null)
        {
            FactoryID = GetNextID();
            _reuse = reuse;
            _setup = setup ?? Setup.Default;
        }

        /// <summary>Returns true if for factory Reuse exists matching resolution or current Scope.</summary>
        /// <param name="request"></param> <returns>True if matching Scope exists.</returns>
        public bool HasMatchingReuseScope(Request request)
        {
            if (!request.Rules.ImplicitCheckForReuseMatchingScope)
                return true;
            var reuse = Reuse as IReuseV3;
            return reuse == null || reuse.CanApply(request);
        }

        /// <summary>The main factory method to create service expression, e.g. "new Client(new Service())".
        /// If <paramref name="request"/> has <see cref="Request.FuncArgs"/> specified, they could be used in expression.</summary>
        /// <param name="request">Service request.</param>
        /// <returns>Created expression.</returns>
        public abstract Expression CreateExpressionOrDefault(Request request);

        /// <summary>Allows derived factories to override or reuse caching policy used by
        /// GetExpressionOrDefault. By default only service setup and no  user passed arguments may be cached.</summary>
        /// <param name="request">Context.</param> <returns>True if factory expression could be cached.</returns>
        protected virtual bool IsFactoryExpressionCacheable(Request request)
        {
            return Setup.FactoryType == FactoryType.Service
                   && !request.TracksTransientDisposable
                   && request.FuncArgs == null
                   && !Setup.AsResolutionCall
                   && !request.IsResolutionRoot
                   && Setup.Condition == null &&
                   !IsScopeDependent(request);
        }

        private bool IsScopeDependent(Request request)
        {
            return Setup.UseParentReuse
                   || request.Reuse is ResolutionScopeReuse
                   || (request.Reuse is CurrentScopeReuse && ((CurrentScopeReuse)request.Reuse).Name != null);
        }

        private bool ShouldBeInjectedAsResolutionCall(Request request)
        {
            return
                // prevents recursion on already split graph
                !request.IsResolutionCall &&
                // explicit aka user requested split
                (Setup.AsResolutionCall ||
                 // implicit split only when not inside Func with arguments, 
                 // cause for now arguments are not propagated through resolve call
                 (request.ShouldSplitObjectGraph() ||
                  Setup.UseParentReuse || request.Reuse is ResolutionScopeReuse) &&
                 !request.IsWrappedInFuncWithArgs()) &&
                request.GetActualServiceType() != typeof(void);
        }

        /// <summary>Returns service expression: either by creating it with <see cref="CreateExpressionOrDefault"/> or taking expression from cache.
        /// Before returning method may transform the expression  by applying <see cref="Reuse"/>, or/and decorators if found any.</summary>
        /// <param name="request">Request for service.</param> <returns>Service expression.</returns>
        public virtual Expression GetExpressionOrDefault(Request request)
        {
            request = request.WithResolvedFactory(this);

            if (ShouldBeInjectedAsResolutionCall(request))
                return Resolver.CreateResolutionExpression(request, Setup.OpenResolutionScope);

            var container = request.Container;

            // First look for decorators
            if (FactoryType != FactoryType.Decorator)
            {
                var decoratorExpr = container.GetDecoratorExpressionOrDefault(request);
                if (decoratorExpr != null)
                    return decoratorExpr;
            }

            // Then optimize for already resolved singleton object, otherwise goes normal ApplyReuse route
            if (request.Rules.EagerCachingSingletonForFasterAccess &&
                request.Reuse is SingletonReuse &&
                !Setup.PreventDisposal && !Setup.WeaklyReferenced)
            {
                var singletons = (SingletonScope)request.SingletonScope;
                var singletonID = singletons.IndexOf(FactoryID);
                if (singletonID > 0)
                {
                    var value = singletons.GetOrDefault(singletonID);
                    if (value != null)
                        return Expression.Constant(value, request.ServiceType);
                }
            }

            // Then check the expression cache
            var isCacheable = IsFactoryExpressionCacheable(request);
            if (isCacheable)
            {
                var cachedExpr = container.GetCachedFactoryExpressionOrDefault(FactoryID);
                if (cachedExpr != null)
                    return cachedExpr;
            }

            // Then create new expression
            var serviceExpr = CreateExpressionOrDefault(request);
            if (serviceExpr != null)
            {
                // can be checked only after expression is created
                if (request.ContainsNestedResolutionCall)
                    isCacheable = false;

                if (request.Reuse != DryIoc.Reuse.Transient &&
                    request.GetActualServiceType() != typeof(void))
                {
                    var originalServiceExprType = serviceExpr.Type;

                    serviceExpr = ApplyReuse(serviceExpr, request.Reuse, request.TracksTransientDisposable, request);

                    if (serviceExpr.NodeType == ExpressionType.Constant)
                        isCacheable = false;

                    if (serviceExpr.Type != originalServiceExprType)
                        serviceExpr = Expression.Convert(serviceExpr, originalServiceExprType);
                }

                if (isCacheable)
                {
                    container.CacheFactoryExpression(FactoryID, serviceExpr);
                }
            }
            // Otherwise throw
            else if (request.IfUnresolved == IfUnresolved.Throw)
            {
                Container.ThrowUnableToResolve(request);
            }

            return serviceExpr;
        }

        // todo: remove trackTransientDisposable param as it is available from Request param.
        /// <summary>Applies reuse to created expression.  Actually wraps passed expression in scoped access
        /// and produces another expression.</summary>
        /// <param name="serviceExpr">Raw service creation (or receiving) expression.</param>
        /// <param name="reuse">Reuse - may be different from <see cref="Reuse"/> if set <see cref="Rules.DefaultReuseInsteadOfTransient"/>.</param>
        /// <param name="tracksTransientDisposable">Specifies that reuse is to track transient disposable.</param>
        /// <param name="request">Context.</param>
        /// <returns>Scoped expression or originally passed expression.</returns>
        protected virtual Expression ApplyReuse(Expression serviceExpr, IReuse reuse, bool tracksTransientDisposable, Request request)
        {
            // optimization for already activated singleton
            if (serviceExpr.NodeType == ExpressionType.Constant &&
                reuse is SingletonReuse && request.Rules.EagerCachingSingletonForFasterAccess &&
                !Setup.PreventDisposal && !Setup.WeaklyReferenced)
                return serviceExpr;

            // Optimize: eagerly create singleton during the construction of object graph,
            // but only for root singleton and not for singleton dependency inside singleton, because of double compilation work
            if (reuse is SingletonReuse &&
                request.Rules.EagerCachingSingletonForFasterAccess &&
                // except: For decorators and wrappers, when tracking transient disposable and for lazy consumption in Func
                FactoryType == FactoryType.Service &&
                !tracksTransientDisposable &&
                !request.IsWrappedInFunc())
            {
                var factoryDelegate = Container.CompileToDelegate(serviceExpr);
                if (Setup.PreventDisposal)
                {
                    var factory = factoryDelegate;
                    factoryDelegate = (_, cs, rs) => new[] { factory(null, cs, rs) };
                }

                if (Setup.WeaklyReferenced)
                {
                    var factory = factoryDelegate;
                    factoryDelegate = (_, cs, rs) => new WeakReference(factory(null, cs, rs));
                }

                var singletonScope = request.SingletonScope;
                var singletonId = singletonScope.GetScopedItemIdOrSelf(FactoryID);
                var singleton = singletonScope.GetOrAdd(singletonId, () =>
                    factoryDelegate(null, request.ContainerWeakRef, request.Scope));

                serviceExpr = Expression.Constant(singleton);
            }
            else
            {
                if (Setup.PreventDisposal)
                    serviceExpr = Expression.NewArrayInit(typeof(object), serviceExpr);

                if (Setup.WeaklyReferenced)
                    serviceExpr = Expression.New(typeof(WeakReference).GetConstructorOrNull(args: typeof(object)), serviceExpr);

                var reuseV3 = reuse as IReuseV3;
                if (reuseV3 != null)
                {
                    serviceExpr = reuseV3.Apply(request, tracksTransientDisposable, serviceExpr);
                }
                else
                {
                    var scopeExpr = reuse.GetScopeExpression(request);

                    // For transient disposable we don't care to bind to specific ID, because it should be created each time.
                    var scopedId = tracksTransientDisposable ? -1 : reuse.GetScopedItemIdOrSelf(FactoryID, request);
                    serviceExpr = Expression.Call(scopeExpr, "GetOrAdd", ArrayTools.Empty<Type>(),
                        Expression.Constant(scopedId),
                        Expression.Lambda<CreateScopedValue>(serviceExpr, ArrayTools.Empty<ParameterExpression>()));
                }
            }

            // Unwrap WeakReference and/or array preventing disposal
            if (Setup.WeaklyReferenced)
                serviceExpr = Expression.Call(typeof(ThrowInGeneratedCode), "ThrowNewErrorIfNull",
                    ArrayTools.Empty<Type>(),
                    Expression.Property(Expression.Convert(serviceExpr, typeof(WeakReference)), "Target"),
                    Expression.Constant(Error.Messages[Error.WeakRefReuseWrapperGCed]));

            if (Setup.PreventDisposal)
                serviceExpr = Expression.ArrayIndex(
                    Expression.Convert(serviceExpr, typeof(object[])),
                    Expression.Constant(0, typeof(int)));

            return serviceExpr;
        }

        /// <summary>Creates factory delegate from service expression and returns it.
        /// to compile delegate from expression but could be overridden by concrete factory type: e.g. <see cref="DelegateFactory"/></summary>
        /// <param name="request">Service request.</param>
        /// <returns>Factory delegate created from service expression.</returns>
        public virtual FactoryDelegate GetDelegateOrDefault(Request request)
        {
            var expression = GetExpressionOrDefault(request);
            if (expression == null)
                return null;
            return Container.CompileToDelegate(expression);
        }

        internal virtual bool ThrowIfInvalidRegistration(Type serviceType, object serviceKey, bool isStaticallyChecked, Rules containerRules)
        {
            if (!isStaticallyChecked)
                serviceType.ThrowIfNull();

            var setup = Setup;
            if (setup.FactoryType == FactoryType.Wrapper)
            {
                ((Setup.WrapperSetup)setup).ThrowIfInvalidRegistration(serviceType);
            }
            else
            {
                // Warn about registering disposable transient
                var reuse = Reuse ?? containerRules.DefaultReuseInsteadOfTransient;
                if (reuse != DryIoc.Reuse.Transient)
                    return true;

                if (setup.AllowDisposableTransient ||
                    !containerRules.ThrowOnRegisteringDisposableTransient)
                    return true;

                if (setup.UseParentReuse ||
                    setup.FactoryType == FactoryType.Decorator && ((Setup.DecoratorSetup)setup).UseDecorateeReuse)
                    return true;

                var knownImplOrServiceType = CanAccessImplementationType ? ImplementationType : serviceType;
                if (knownImplOrServiceType.IsAssignableTo(typeof(IDisposable)))
                    Throw.It(Error.RegisteredDisposableTransientWontBeDisposedByContainer,
                        serviceType, serviceKey ?? "{no key}", this);
            }

            return true;
        }

        /// <summary>Returns nice string representation of factory.</summary>
        /// <returns>String representation.</returns>
        public override string ToString()
        {
            var s = new StringBuilder().Append("{ID=").Append(FactoryID);
            if (ImplementationType != null)
                s.Append(", ImplType=").Print(ImplementationType);
            if (Reuse != null)
                s.Append(", Reuse=").Print(Reuse);
            if (Setup.FactoryType != Setup.Default.FactoryType)
                s.Append(", FactoryType=").Append(Setup.FactoryType);
            if (Setup.Metadata != null)
                s.Append(", Metadata=").Print(Setup.Metadata, quote: "\"");
            if (Setup.Condition != null)
                s.Append(", HasCondition");

            if (Setup.OpenResolutionScope)
                s.Append(", OpensResolutionScope");
            else if (Setup.AsResolutionCall)
                s.Append(", AsResolutionScope");

            return s.Append("}").ToString();
        }

        #region Implementation

        private static int _lastFactoryID;
        private IReuse _reuse;
        private Setup _setup;

        #endregion
    }

    /// <summary>Declares delegate to get single factory method or constructor for resolved request.</summary>
    /// <param name="request">Request to resolve.</param>
    /// <returns>Factory method wrapper over constructor or method.</returns>
    internal delegate FactoryMethod FactoryMethodSelector(Request request);

    /// <summary>Specifies how to get parameter info for injected parameter and resolved request</summary>
    /// <remarks>Request is for parameter method owner not for parameter itself.</remarks>
    /// <param name="request">Request for parameter method/constructor owner.</param>
    /// <returns>Service info describing how to inject parameter.</returns>
    internal delegate Func<ParameterInfo, ParameterServiceInfo> ParameterSelector(Request request);

    /// <summary>Specifies what properties or fields to inject and how.</summary>
    /// <param name="request">Request for property/field owner.</param>
    /// <returns>Corresponding service info for each property/field to be injected.</returns>
    internal delegate IEnumerable<PropertyOrFieldServiceInfo> PropertiesAndFieldsSelector(Request request);

    /// <summary>DSL for specifying <see cref="ParameterSelector"/> injection rules.</summary>
    internal static class Parameters
    {
        /// <summary>Returns default service info wrapper for each parameter info.</summary>
        public static ParameterSelector Of = request => ParameterServiceInfo.Of;

        /// <summary>Returns service info which considers each parameter as optional.</summary>
        public static ParameterSelector IfUnresolvedReturnDefault =
            request => pi => ParameterServiceInfo.Of(pi).WithDetails(ServiceDetails.IfUnresolvedReturnDefault, request);

        /// <summary>Combines source selector with other. Other is used as fallback when source returns null.</summary>
        /// <param name="source">Source selector.</param> <param name="other">Specific other selector to add.</param>
        /// <returns>Combined result selector.</returns>
        public static ParameterSelector OverrideWith(this ParameterSelector source, ParameterSelector other)
        {
            return source == null || source == Of ? other ?? Of
                : other == null || other == Of ? source
                    : request => parameterInfo =>
                    {
                        // try other selctor first
                        var otherSelector = other(request);
                        if (otherSelector != null)
                        {
                            var parameterServiceInfo = otherSelector(parameterInfo);
                            if (parameterServiceInfo != null)
                                return parameterServiceInfo;
                        }

                        // fallback to source selector if other is failed
                        var sourceSelector = source(request);
                        if (sourceSelector != null)
                            return sourceSelector(parameterInfo);

                        return null;
                    };
        }

        // todo: v3: remove because it is replace by OverrideWith method
        /// <summary>Obsolete: use <see cref="OverrideWith"/>.</summary>
        public static ParameterSelector And(this ParameterSelector source, ParameterSelector other)
        {
            return source == null || source == Of ? other ?? Of
                : other == null || other == Of ? source
                    : request => other(request) ?? source(request);
        }

        /// <summary>Overrides source parameter rules with specific parameter details. If it is not your parameter just return null.</summary>
        /// <param name="source">Original parameters rules</param>
        /// <param name="getDetailsOrNull">Should return specific details or null.</param>
        /// <returns>New parameters rules.</returns>
        public static ParameterSelector Details(this ParameterSelector source, Func<Request, ParameterInfo, ServiceDetails> getDetailsOrNull)
        {
            getDetailsOrNull.ThrowIfNull();
            return request => parameter =>
            {
                var details = getDetailsOrNull(request, parameter);
                if (details != null)
                    return ParameterServiceInfo.Of(parameter).WithDetails(details, request);

                // for default source selector, return null to enable fallback to any non-default selector
                // defined outside, usually by OverrideWith
                if (source == Of)
                    return null;

                return source(request)(parameter);
            };
        }

        /// <summary>Adds to <paramref name="source"/> selector service info for parameter identified by <paramref name="name"/>.</summary>
        /// <param name="source">Original parameters rules.</param> <param name="name">Name to identify parameter.</param>
        /// <param name="requiredServiceType">(optional)</param> <param name="serviceKey">(optional)</param>
        /// <param name="ifUnresolved">(optional) By default throws exception if unresolved.</param>
        /// <param name="defaultValue">(optional) Specifies default value to use when unresolved.</param>
        /// <param name="metadataKey">(optional) Required metadata key</param> <param name="metadata">Required metadata or value.</param>
        /// <returns>New parameters rules.</returns>
        public static ParameterSelector Name(this ParameterSelector source, string name,
            Type requiredServiceType = null, object serviceKey = null,
            IfUnresolved ifUnresolved = IfUnresolved.Throw, object defaultValue = null,
            string metadataKey = null, object metadata = null)
        {
            return source.Details((r, p) => !p.Name.Equals(name) ? null
                : ServiceDetails.Of(requiredServiceType, serviceKey, ifUnresolved, defaultValue, metadataKey, metadata));
        }

        /// <summary>Specify parameter by name and set custom value to it.</summary>
        /// <param name="source">Original parameters rules.</param> <param name="name">Parameter name.</param>
        /// <param name="getCustomValue">Custom value provider.</param>
        /// <returns>New parameters rules.</returns>
        public static ParameterSelector Name(this ParameterSelector source, string name, Func<Request, object> getCustomValue)
        {
            return source.Details((r, p) => p.Name.Equals(name) ? ServiceDetails.Of(getCustomValue(r)) : null);
        }

        // todo: add overload with input ParameterInfo so that doing convention as in #443 would be more easy.
        /// <summary>Adds to <paramref name="source"/> selector service info for parameter identified by type <typeparamref name="T"/>.</summary>
        /// <typeparam name="T">Type of parameter.</typeparam> <param name="source">Source selector.</param>
        /// <param name="requiredServiceType">(optional)</param> <param name="serviceKey">(optional)</param>
        /// <param name="ifUnresolved">(optional) By default throws exception if unresolved.</param>
        /// <param name="defaultValue">(optional) Specifies default value to use when unresolved.</param>
        /// <param name="metadataKey">(optional) Required metadata key</param> <param name="metadata">Required metadata or value.</param>
        /// <returns>Combined selector.</returns>
        public static ParameterSelector Type<T>(this ParameterSelector source,
            Type requiredServiceType = null, object serviceKey = null,
            IfUnresolved ifUnresolved = IfUnresolved.Throw, object defaultValue = null,
            string metadataKey = null, object metadata = null)
        {
            return source.Details((r, p) =>
                !typeof(T).IsAssignableTo(p.ParameterType)
                    ? null
                    : ServiceDetails.Of(requiredServiceType, serviceKey, ifUnresolved, defaultValue, metadataKey, metadata));
        }

        /// <summary>Specify parameter by type and set custom value to it.</summary>
        /// <typeparam name="T">Parameter type.</typeparam>
        /// <param name="source">Original parameters rules.</param>
        /// <param name="getCustomValue">Custom value provider.</param>
        /// <returns>New parameters rules.</returns>
        public static ParameterSelector Type<T>(this ParameterSelector source, Func<Request, T> getCustomValue)
        {
            Throw.If(ContainerTools.IsSupportedInjectedCustomValueType(typeof(T)) == false,
                Error.RegisteringWithNotSupportedDepedendencyCustomValueType, "parameter", typeof(T));
            return source.Details((r, p) => p.ParameterType == typeof(T) ? ServiceDetails.Of(getCustomValue(r)) : null);
        }
    }

    /// <summary>DSL for specifying <see cref="PropertiesAndFieldsSelector"/> injection rules.</summary>
    internal static partial class PropertiesAndFields
    {
        /// <summary>Say to not resolve any properties or fields.</summary>
        public static PropertiesAndFieldsSelector Of = request => null;

        /// <summary>Public assignable instance members of any type except object, string, primitives types, and arrays of those.</summary>
        public static PropertiesAndFieldsSelector Auto = All(false, false);

        /// <summary>Should return service info for input member (property or field).</summary>
        /// <param name="member">Input member.</param> <param name="request">Request to provide context.</param> <returns>Service info.</returns>
        internal delegate PropertyOrFieldServiceInfo GetInfo(MemberInfo member, Request request);

        /// <summary>Generates selector property and field selector with settings specified by parameters.
        /// If all parameters are omitted the return all public not primitive members.</summary>
        /// <param name="withNonPublic">(optional) Specifies to include non public members. Will include by default.</param>
        /// <param name="withPrimitive">(optional) Specifies to include members of primitive types. Will include by default.</param>
        /// <param name="withFields">(optional) Specifies to include fields as well as properties. Will include by default.</param>
        /// <param name="ifUnresolved">(optional) Defines ifUnresolved behavior for resolved members.</param>
        /// <param name="withInfo">(optional) Return service info for a member or null to skip member resolution.</param>
        /// <returns>Result selector composed using provided settings.</returns>
        public static PropertiesAndFieldsSelector All(
            bool withNonPublic = true, bool withPrimitive = true, bool withFields = true,
            IfUnresolved ifUnresolved = IfUnresolved.ReturnDefault,
            GetInfo withInfo = null)
        {
            GetInfo getInfo = (m, r) => withInfo != null ? withInfo(m, r) :
                PropertyOrFieldServiceInfo.Of(m).WithDetails(ServiceDetails.Of(ifUnresolved: ifUnresolved), r);
            return r =>
            {
                var properties = r.ImplementationType.GetMembers(_ => _.DeclaredProperties, includeBase: true)
                    .Where(p => p.IsInjectable(withNonPublic, withPrimitive))
                    .Select(m => getInfo(m, r));
                return !withFields ? properties :
                    properties.Concat(r.ImplementationType.GetMembers(_ => _.DeclaredFields, includeBase: true)
                        .Where(f => f.IsInjectable(withNonPublic, withPrimitive))
                        .Select(m => getInfo(m, r)));
            };
        }

        /// <summary>Combines source properties and fields with other. Other will override the source condition.</summary>
        /// <param name="source">Source selector.</param> <param name="other">Specific other selector to add.</param>
        /// <returns>Combined result selector.</returns>
        public static PropertiesAndFieldsSelector OverrideWith(
            this PropertiesAndFieldsSelector source, PropertiesAndFieldsSelector other)
        {
            return source == null || source == Of ? (other ?? Of)
                : other == null || other == Of ? source
                    : r =>
                    {
                        var sourceMembers = source(r).ToArrayOrSelf();
                        var otherMembers = other(r).ToArrayOrSelf();
                        return sourceMembers == null || sourceMembers.Length == 0 ? otherMembers
                            : otherMembers == null || otherMembers.Length == 0 ? sourceMembers
                                : otherMembers.Append(
                                    sourceMembers.Match(s => s != null &&
                                                             otherMembers.All(o => o == null || !s.Member.Name.Equals(o.Member.Name))));
                    };
        }

        // todo: v3: remove
        /// <summary>Obsolete: renamed to <see cref="OverrideWith"/>.</summary>
        public static PropertiesAndFieldsSelector And(
            this PropertiesAndFieldsSelector source, PropertiesAndFieldsSelector other)
        {
            return source == null || source == Of ? (other ?? Of)
                : other == null || other == Of ? source
                    : r =>
                    {
                        var sourceMembers = source(r).ToArrayOrSelf();
                        var otherMembers = other(r).ToArrayOrSelf();
                        return sourceMembers == null || sourceMembers.Length == 0 ? otherMembers
                            : otherMembers == null || otherMembers.Length == 0 ? sourceMembers
                                : otherMembers.Append(
                                    sourceMembers.Match(s => s != null &&
                                                             otherMembers.All(o => o == null || !s.Member.Name.Equals(o.Member.Name))));
                    };
        }

        /// <summary>Specifies service details (key, if-unresolved policy, required type) for property/field with the name.</summary>
        /// <param name="source">Original member selector.</param> <param name="name">Member name.</param> <param name="getDetails">Details.</param>
        /// <returns>New selector.</returns>
        public static PropertiesAndFieldsSelector Details(this PropertiesAndFieldsSelector source, string name, Func<Request, ServiceDetails> getDetails)
        {
            name.ThrowIfNull();
            getDetails.ThrowIfNull();
            return source.OverrideWith(request =>
            {
                var implementationType = request.ImplementationType;

                var property = implementationType.GetPropertyOrNull(name);
                if (property != null && property.IsInjectable(true, true))
                {
                    var details = getDetails(request);
                    return details == null ? null
                        : new[] { PropertyOrFieldServiceInfo.Of(property).WithDetails(details, request) };
                }

                var field = implementationType.GetFieldOrNull(name);
                if (field != null && field.IsInjectable(true, true))
                {
                    var details = getDetails(request);
                    return details == null ? null
                        : new[] { PropertyOrFieldServiceInfo.Of(field).WithDetails(details, request) };
                }

                return Throw.For<IEnumerable<PropertyOrFieldServiceInfo>>(Error.NotFoundSpecifiedWritablePropertyOrField, name, request);
            });
        }

        /// <summary>Adds to <paramref name="source"/> selector service info for property/field identified by <paramref name="name"/>.</summary>
        /// <param name="source">Source selector.</param> <param name="name">Name to identify member.</param>
        /// <param name="requiredServiceType">(optional)</param> <param name="serviceKey">(optional)</param>
        /// <param name="ifUnresolved">(optional) By default returns default value if unresolved.</param>
        /// <param name="defaultValue">(optional) Specifies default value to use when unresolved.</param>
        /// <param name="metadataKey">(optional) Required metadata key</param> <param name="metadata">Required metadata or value.</param>
        /// <returns>Combined selector.</returns>
        public static PropertiesAndFieldsSelector Name(this PropertiesAndFieldsSelector source, string name,
            Type requiredServiceType = null, object serviceKey = null,
            IfUnresolved ifUnresolved = IfUnresolved.ReturnDefault, object defaultValue = null,
            string metadataKey = null, object metadata = null)
        {
            return source.Details(name, r => ServiceDetails.Of(
                requiredServiceType, serviceKey, ifUnresolved, defaultValue, metadataKey, metadata));
        }

        /// <summary>Specifies custom value for property/field with specific name.</summary>
        /// <param name="source">Original property/field list.</param>
        /// <param name="name">Target member name.</param> <param name="getCustomValue">Custom value provider.</param>
        /// <returns>Return new combined selector.</returns>
        public static PropertiesAndFieldsSelector Name(this PropertiesAndFieldsSelector source, string name, Func<Request, object> getCustomValue)
        {
            return source.Details(name, r => ServiceDetails.Of(getCustomValue(r)));
        }

        /// <summary>Returns true if property matches flags provided.</summary>
        /// <param name="property">Property to match</param>
        /// <param name="withNonPublic">Says to include non public properties.</param>
        /// <param name="withPrimitive">Says to include properties of primitive type.</param>
        /// <returns>True if property is matched and false otherwise.</returns>
        public static bool IsInjectable(this PropertyInfo property, bool withNonPublic = false, bool withPrimitive = false)
        {
            return property.CanWrite
                   && !property.IsStatic()
                   && !property.IsIndexer() // first checks that property is assignable in general and not an indexer
                   && (withNonPublic || property.GetSetMethodOrNull() != null)
                   && (withPrimitive || !property.PropertyType.IsPrimitive(orArrayOfPrimitives: true));
        }

        /// <summary>Returns true if field matches flags provided.</summary>
        /// <param name="field">Field to match.</param>
        /// <param name="withNonPublic">Says to include non public fields.</param>
        /// <param name="withPrimitive">Says to include fields of primitive type.</param>
        /// <returns>True if property is matched and false otherwise.</returns>
        public static bool IsInjectable(this FieldInfo field, bool withNonPublic = false, bool withPrimitive = false)
        {
            return !field.IsInitOnly && !field.IsBackingField()
                   && (withNonPublic || field.IsPublic)
                   && (withPrimitive || !field.FieldType.IsPrimitive(orArrayOfPrimitives: true));
        }
    }

    /// <summary>Reflects on <see cref="ImplementationType"/> constructor parameters and members,
    /// creates expression for each reflected dependency, and composes result service expression.</summary>
    internal sealed class ReflectionFactory : Factory
    {
        /// <summary>Non-abstract service implementation type. May be open generic.</summary>
        public override Type ImplementationType
        {
            get
            {
                if (_implementationType == null && _implementationTypeProvider != null)
                    SetKnownImplementationType(_implementationTypeProvider(), Made);
                return _implementationType;
            }
        }

        /// <summary>False for lazy implementation type, to prevent its early materialization.</summary>
        public override bool CanAccessImplementationType
        {
            get { return _implementationType != null || _implementationTypeProvider == null; }
        }

        /// <summary>Provides closed-generic factory for registered open-generic variant.</summary>
        public override IConcreteFactoryGenerator FactoryGenerator { get { return _factoryGenerator; } }

        /// <summary>Injection rules set for Constructor/FactoryMethod, Parameters, Properties and Fields.</summary>
        public override Made Made { get { return _made; } }

        /// <summary>Creates factory providing implementation type, optional reuse and setup.</summary>
        /// <param name="implementationType">(optional) Optional if Made.FactoryMethod is present Non-abstract close or open generic type.</param>
        /// <param name="reuse">(optional)</param> <param name="made">(optional)</param> <param name="setup">(optional)</param>
        public ReflectionFactory(Type implementationType = null, IReuse reuse = null, Made made = null, Setup setup = null)
            : base(reuse, setup)
        {
            _made = made ?? Made.Default;
            SetKnownImplementationType(implementationType, _made);
        }

        /// <summary>Creates factory providing implementation type, optional reuse and setup.</summary>
        /// <param name="implementationTypeProvider">Provider of non-abstract close or open generic type.</param>
        /// <param name="reuse">(optional)</param> <param name="made">(optional)</param> <param name="setup">(optional)</param>
        public ReflectionFactory(Func<Type> implementationTypeProvider, IReuse reuse = null, Made made = null, Setup setup = null)
            : base(reuse, setup)
        {
            _made = made ?? Made.Default;
            _implementationTypeProvider = implementationTypeProvider.ThrowIfNull();
        }

        /// <summary>Add to base rules: do not cache if Made is context based.</summary>
        /// <param name="request">Context.</param> <returns>True if factory expression could be cached.</returns>
        protected override bool IsFactoryExpressionCacheable(Request request)
        {
            // todo: review Made and may be move to IsContextDependent
            return base.IsFactoryExpressionCacheable(request)
                   && (Made == Made.Default
                       // Property injection.
                       || (Made.FactoryMethod == null
                           && Made.Parameters == null
                           && (Made.PropertiesAndFields == PropertiesAndFields.Auto ||
                               Made.PropertiesAndFields == PropertiesAndFields.Of))
                       // No caching for context dependent Made which is:
                       // - We don't know the result returned by factory method - it depends on request
                       // - or even if we do know the result type, some dependency is using custom value which depends on request
                       || (Made.FactoryMethodKnownResultType != null && !Made.HasCustomDependencyValue));
        }

        /// <summary>Creates service expression, so for registered implementation type "Service",
        /// you will get "new Service()". If there is <see cref="Reuse"/> specified, then expression will
        /// contain call to <see cref="Scope"/> returned by reuse.</summary>
        /// <param name="request">Request for service to resolve.</param> <returns>Created expression.</returns>
        public override Expression CreateExpressionOrDefault(Request request)
        {
            var factoryMethod = GetFactoryMethod(request);

            var container = request.Container;

            // If factory method is instance method, then resolve factory instance first.
            Expression factoryExpr = null;
            if (factoryMethod.FactoryServiceInfo != null)
            {
                var factoryRequest = request.Push(factoryMethod.FactoryServiceInfo);
                var factoryFactory = container.ResolveFactory(factoryRequest);
                if (factoryFactory == null)
                    return null;
                factoryExpr = factoryFactory.GetExpressionOrDefault(factoryRequest);
                if (factoryExpr == null)
                    return null;
            }

            var containerRules = container.Rules;
            var allParamsAreConstants = true;

            Expression[] paramExprs = null;
            var ctorOrMethod = factoryMethod.ConstructorOrMethodOrMember as MethodBase;
            if (ctorOrMethod != null)
            {
                var parameters = ctorOrMethod.GetParameters();
                if (parameters.Length != 0)
                {
                    paramExprs = new Expression[parameters.Length];

                    var selectorSelector =
                        containerRules.OverrideRegistrationMade
                            ? Made.Parameters.OverrideWith(containerRules.Parameters)
                            : containerRules.Parameters.OverrideWith(Made.Parameters);

                    var parameterSelector = selectorSelector(request);

                    var funcArgs = request.FuncArgs;
                    var funcArgsUsedMask = 0;

                    for (var i = 0; i < parameters.Length; i++)
                    {
                        var param = parameters[i];
                        Expression paramExpr = null;

                        if (funcArgs != null)
                        {
                            for (var fa = 0; fa < funcArgs.Value.Length && paramExpr == null; ++fa)
                            {
                                var funcArg = funcArgs.Value[fa];
                                if ((funcArgsUsedMask & 1 << fa) == 0 &&              // not yet used func argument
                                    funcArg.Type.IsAssignableTo(param.ParameterType)) // and it assignable to parameter
                                {
                                    paramExpr = funcArg;
                                    funcArgsUsedMask |= 1 << fa;  // mark that argument was used
                                    funcArgs.Key[fa] = true;      // mark that argument was used globally for Func<..> resolver.
                                }
                            }
                        }

                        // If parameter expression still null (no Func argument to substitute), try to resolve it
                        if (paramExpr == null)
                        {
                            var paramInfo = parameterSelector(param) ?? ParameterServiceInfo.Of(param);
                            var paramRequest = request.Push(paramInfo);

                            if (paramInfo.Details.HasCustomValue)
                            {
                                var customValue = paramInfo.Details.CustomValue;
                                if (customValue != null)
                                    customValue.ThrowIfNotOf(paramRequest.ServiceType, Error.InjectedCustomValueIsOfDifferentType, paramRequest);
                                paramExpr = container.GetOrAddStateItemExpression(customValue, paramRequest.ServiceType);
                            }
                            else
                            {
                                var paramFactory = container.ResolveFactory(paramRequest);
                                paramExpr = paramFactory == null ? null : paramFactory.GetExpressionOrDefault(paramRequest);
                                // Meant that parent Or parameter itself allows default value,
                                // otherwise we did not get null but exception
                                if (paramExpr == null)
                                {
                                    // Check if parameter itself (without propagated parent details)
                                    // does not allow default, then stop checking the rest of parameters.
                                    if (paramInfo.Details.IfUnresolved == IfUnresolved.Throw)
                                        return null;

                                    var defaultValue = paramInfo.Details.DefaultValue;
                                    paramExpr = defaultValue != null
                                        ? container.GetOrAddStateItemExpression(defaultValue)
                                        : paramRequest.ServiceType.GetDefaultValueExpression();
                                }
                            }
                        }

                        if (paramExpr.NodeType != ExpressionType.Constant &&
                            !(paramExpr.NodeType == ExpressionType.Convert &&
                              ((UnaryExpression)paramExpr).Operand.NodeType == ExpressionType.Constant))
                            allParamsAreConstants = false;

                        paramExprs[i] = paramExpr;
                    }
                }
            }

            return CreateServiceExpression(factoryMethod.ConstructorOrMethodOrMember, factoryExpr, paramExprs, request,
                allParamsAreConstants);
        }

        internal override bool ThrowIfInvalidRegistration(Type serviceType, object serviceKey, bool isStaticallyChecked, Rules containerRules)
        {
            base.ThrowIfInvalidRegistration(serviceType, serviceKey, isStaticallyChecked, containerRules);

            if (!CanAccessImplementationType)
                return true;

            var implType = ImplementationType;
            if (Made.FactoryMethod == null && containerRules.FactoryMethod == null)
            {
                var ctors = implType.GetPublicInstanceConstructors().ToArrayOrSelf();
                if (ctors.Length == 1)
                    _knownSingleCtor = ctors[0];
                else if (ctors.Length == 0)
                    Throw.It(Error.UnableToSelectSinglePublicConstructorFromNone, implType);
                else
                    Throw.It(Error.UnableToSelectSinglePublicConstructorFromMultiple, implType, ctors);
            }

            if (isStaticallyChecked || implType == null)
                return true;

            if (!implType.IsGenericDefinition())
            {
                if (implType.IsOpenGeneric())
                    Throw.It(Error.RegisteringNotAGenericTypedefImplType,
                        implType, implType.GetGenericDefinitionOrNull());

                else if (implType != serviceType && serviceType != typeof(object) &&
                         implType.GetImplementedTypes().IndexOf(t => t == serviceType) == -1)
                    Throw.It(Error.RegisteringImplementationNotAssignableToServiceType, implType, serviceType);
            }
            else if (implType != serviceType)
            {
                if (serviceType.IsGenericDefinition())
                    ThrowIfImplementationAndServiceTypeParamsDontMatch(implType, serviceType);

                else if (implType.IsGeneric() && serviceType.IsOpenGeneric())
                    Throw.It(Error.RegisteringNotAGenericTypedefServiceType,
                        serviceType, serviceType.GetGenericTypeDefinition());

                else if (!serviceType.IsGeneric())
                    Throw.It(Error.RegisteringOpenGenericImplWithNonGenericService, implType, serviceType);

                else if (implType.GetImplementedServiceTypes().IndexOf(serviceType.GetGenericTypeDefinition()) == -1)
                    Throw.It(Error.RegisteringImplementationNotAssignableToServiceType, implType, serviceType);
            }

            return true;
        }

        private static void ThrowIfImplementationAndServiceTypeParamsDontMatch(Type implType, Type serviceType)
        {
            var implTypeParams = implType.GetGenericParamsAndArgs();
            var implementedTypes = implType.GetImplementedTypes();

            var implementedTypeFound = false;
            var containsAllTypeParams = false;
            for (var i = 0; !containsAllTypeParams && i < implementedTypes.Length; ++i)
            {
                var implementedType = implementedTypes[i];
                implementedTypeFound = implementedType.GetGenericDefinitionOrNull() == serviceType;
                containsAllTypeParams = implementedTypeFound
                                        && implementedType.ContainsAllGenericTypeParameters(implTypeParams);
            }

            if (!implementedTypeFound)
                Throw.It(Error.RegisteringImplementationNotAssignableToServiceType, implType, serviceType);

            if (!containsAllTypeParams)
                Throw.It(Error.RegisteringOpenGenericServiceWithMissingTypeArgs,
                    implType, serviceType,
                    implementedTypes.Where(t => t.GetGenericDefinitionOrNull() == serviceType));
        }

        #region Implementation

        private Type _implementationType; // non-readonly to be set by lazy type provider
        private readonly Func<Type> _implementationTypeProvider;
        private readonly Made _made;
        private ClosedGenericFactoryGenerator _factoryGenerator;
        private ConstructorInfo _knownSingleCtor;

        private sealed class ClosedGenericFactoryGenerator : IConcreteFactoryGenerator
        {
            public ImTreeMap<KV<Type, object>, ReflectionFactory> GeneratedFactories
            {
                get { return _generatedFactories.Value; }
            }

            public ClosedGenericFactoryGenerator(ReflectionFactory openGenericFactory)
            {
                _openGenericFactory = openGenericFactory;
            }

            public Factory GetGeneratedFactory(Request request, bool ifErrorReturnDefault = false)
            {
                var serviceType = request.GetActualServiceType();

                var generatedFactoryKey = new KV<Type, object>(serviceType, request.ServiceKey);

                var generatedFactories = _generatedFactories.Value;
                if (!generatedFactories.IsEmpty)
                {
                    var generatedFactory = generatedFactories.GetValueOrDefault(generatedFactoryKey);
                    if (generatedFactory != null)
                        return generatedFactory;
                }

                var openFactory = _openGenericFactory;
                request = request.WithResolvedFactory(openFactory,
                    skipRecursiveDependencyCheck: ifErrorReturnDefault, skipCaptiveDependencyCheck: ifErrorReturnDefault);

                var implType = openFactory._implementationType;

                var closedTypeArgs = implType == null || implType == serviceType.GetGenericDefinitionOrNull()
                    ? serviceType.GetGenericParamsAndArgs()
                    : implType.IsGenericParameter ? new[] { serviceType }
                        : GetClosedTypeArgsOrNullForOpenGenericType(implType, serviceType, request, ifErrorReturnDefault);

                if (closedTypeArgs == null)
                    return null;

                var made = openFactory.Made;
                if (made.FactoryMethod != null)
                {
                    var factoryMethod = made.FactoryMethod(request);
                    if (factoryMethod == null)
                        return ifErrorReturnDefault ? null
                            : Throw.For<Factory>(Error.GotNullFactoryWhenResolvingService, request);

                    var checkMatchingType = implType != null && implType.IsGenericParameter;
                    var closedFactoryMethod = GetClosedFactoryMethodOrDefault(
                        factoryMethod, closedTypeArgs, request, checkMatchingType);

                    // may be null only for IfUnresolved.ReturnDefault or check for matching type is failed
                    if (closedFactoryMethod == null)
                        return null;

                    made = Made.Of(closedFactoryMethod, made.Parameters, made.PropertiesAndFields);
                }

                Type closedImplType = null;
                if (implType != null)
                {
                    if (implType.IsGenericParameter)
                        closedImplType = closedTypeArgs[0];
                    else
                        closedImplType = Throw.IfThrows<ArgumentException, Type>(
                            () => implType.MakeGenericType(closedTypeArgs),
                            !ifErrorReturnDefault && request.IfUnresolved == IfUnresolved.Throw,
                            Error.NoMatchedGenericParamConstraints, implType, request);

                    if (closedImplType == null)
                        return null;
                }

                var closedGenericFactory = new ReflectionFactory(closedImplType, openFactory.Reuse, made, openFactory.Setup);

                // Storing generated factory ID to service type/key mapping
                // to find/remove generated factories when needed
                _generatedFactories.Swap(_ => _.AddOrUpdate(generatedFactoryKey, closedGenericFactory));
                return closedGenericFactory;
            }

            private readonly ReflectionFactory _openGenericFactory;
            private readonly Ref<ImTreeMap<KV<Type, object>, ReflectionFactory>>
                _generatedFactories = Ref.Of(ImTreeMap<KV<Type, object>, ReflectionFactory>.Empty);
        }

        private void SetKnownImplementationType(Type implType, Made made)
        {
            var knownImplType = implType;

            var factoryMethodResultType = Made.FactoryMethodKnownResultType;
            if (implType == null ||
                implType == typeof(object) ||
                implType.IsAbstract())
            {
                if (made.FactoryMethod == null)
                {
                    if (implType == null)
                        Throw.It(Error.RegisteringNullImplementationTypeAndNoFactoryMethod);
                    if (implType.IsAbstract())
                        Throw.It(Error.RegisteringAbstractImplementationTypeAndNoFactoryMethod, implType);
                }

                knownImplType = null; // Ensure that we do not have abstract implementation type

                // Using non-abstract factory method result type is safe for conditions and diagnostics
                if (factoryMethodResultType != null &&
                    factoryMethodResultType != typeof(object) &&
                    !factoryMethodResultType.IsAbstract())
                    knownImplType = factoryMethodResultType;
            }
            else if (factoryMethodResultType != null
                     && factoryMethodResultType != implType)
            {
                implType.ThrowIfNotImplementedBy(factoryMethodResultType,
                    Error.RegisteredFactoryMethodResultTypesIsNotAssignableToImplementationType);
            }

            var openGenericImplType = knownImplType ?? implType;
            if (openGenericImplType == typeof(object) || // for open-generic T implementation
                openGenericImplType != null && (         // for open-generic X<T> implementation
                    openGenericImplType.IsGenericDefinition() ||
                    openGenericImplType.IsGenericParameter))
                _factoryGenerator = new ClosedGenericFactoryGenerator(this);

            _implementationType = knownImplType;
        }

        private Expression CreateServiceExpression(MemberInfo ctorOrMethodOrMember, Expression factoryExpr, Expression[] paramExprs,
            Request request, bool allParamsAreConstants)
        {
            var rules = request.Rules;

            var ctor = ctorOrMethodOrMember as ConstructorInfo;
            if (ctor != null)
            {
                // optimize singleton creation bypassing Expression.New 
                if (allParamsAreConstants && ctor.IsPublic &&
                    rules.PropertiesAndFields == null && Made.PropertiesAndFields == null)
                {
                    if (request.Reuse is SingletonReuse &&
                        request.Rules.EagerCachingSingletonForFasterAccess &&
                        FactoryType == FactoryType.Service &&
                        !Setup.PreventDisposal && !Setup.WeaklyReferenced &&
                        !request.TracksTransientDisposable &&
                        !request.IsWrappedInFunc())
                    {
                        var activateSingleton = ActivateSingleton(ctor.DeclaringType, paramExprs);

                        var singletonScope = request.SingletonScope;
                        var singletonId = singletonScope.GetScopedItemIdOrSelf(FactoryID);
                        var singleton = singletonScope.GetOrAdd(singletonId, activateSingleton);

                        return Expression.Constant(singleton);
                    }
                }

                var newServiceExpr = Expression.New(ctor, paramExprs);

                if (rules.PropertiesAndFields == null && Made.PropertiesAndFields == null)
                    return newServiceExpr;

                var selector = rules.OverrideRegistrationMade
                    ? Made.PropertiesAndFields.OverrideWith(rules.PropertiesAndFields)
                    : rules.PropertiesAndFields.OverrideWith(Made.PropertiesAndFields);

                var propertiesAndFields = selector(request);
                if (propertiesAndFields == null)
                    return newServiceExpr;

                return InitPropertiesAndFields(newServiceExpr, request, propertiesAndFields);
            }

            var method = ctorOrMethodOrMember as MethodInfo;
            var serviceExpr = method != null
                ? (Expression)Expression.Call(factoryExpr, method, paramExprs)
                : (ctorOrMethodOrMember is PropertyInfo
                    ? Expression.Property(factoryExpr, (PropertyInfo)ctorOrMethodOrMember)
                    : Expression.Field(factoryExpr, (FieldInfo)ctorOrMethodOrMember));

            var returnType = ctorOrMethodOrMember.GetReturnTypeOrDefault().ThrowIfNull();
            if (!returnType.IsAssignableTo(request.ServiceType))
                return Throw.IfThrows<InvalidOperationException, Expression>(
                    () => Expression.Convert(serviceExpr, request.ServiceType),
                    request.IfUnresolved == IfUnresolved.Throw,
                    Error.ServiceIsNotAssignableFromFactoryMethod, request.ServiceType, ctorOrMethodOrMember, request);

            return serviceExpr;
        }

        private static CreateScopedValue ActivateSingleton(Type singletonType, IList<Expression> argExprs)
        {
            object[] args = null;
            if (argExprs == null || argExprs.Count == 0)
            {
                args = ArrayTools.Empty<object>();
            }
            else if (argExprs.Count == 1)
            {
                var argExpr = argExprs[0];
                if (argExpr.NodeType == ExpressionType.Convert)
                    argExpr = ((UnaryExpression)argExpr).Operand;

                var constExpr = argExpr as ConstantExpression;
                if (constExpr != null)
                    args = new[] { constExpr.Value };
            }
            else
            {
                var constantArgs = new object[argExprs.Count];
                int i = constantArgs.Length - 1;
                for (; i >= 0; --i)
                {
                    var argExpr = argExprs[i];
                    if (argExpr.NodeType == ExpressionType.Convert)
                        argExpr = ((UnaryExpression)argExpr).Operand;

                    var constExpr = argExpr as ConstantExpression;
                    if (constExpr == null)
                        break;
                    constantArgs[i] = constExpr.Value;
                }

                if (i == -1) // all args are constants
                    args = constantArgs;
            }

            if (args == null)
                return null;

#pragma warning disable PC001
            return () => Activator.CreateInstance(singletonType, args);
#pragma warning restore PC001

        }

        private FactoryMethod GetFactoryMethod(Request request)
        {
            var implType = request.ImplementationType;
            var factoryMethodSelector = Made.FactoryMethod ?? request.Rules.FactoryMethod;
            if (factoryMethodSelector == null)
            {
                // there is a guarantee of single constructor, which was checked on factory registration
                var ctor = _knownSingleCtor ?? implType.GetPublicInstanceConstructors().First();
                return FactoryMethod.Of(ctor);
            }

            var factoryMethod = factoryMethodSelector(request);
            if (factoryMethod != null && !(factoryMethod.ConstructorOrMethodOrMember is ConstructorInfo))
            {
                var member = factoryMethod.ConstructorOrMethodOrMember;
                var isStaticMember = member.IsStatic();

                Throw.If(isStaticMember && factoryMethod.FactoryServiceInfo != null,
                    Error.FactoryObjProvidedButMethodIsStatic, factoryMethod.FactoryServiceInfo, factoryMethod, request);

                Throw.If(!isStaticMember && factoryMethod.FactoryServiceInfo == null,
                    Error.FactoryObjIsNullInFactoryMethod, factoryMethod, request);
            }

            return factoryMethod.ThrowIfNull(Error.UnableToGetConstructorFromSelector, implType, request);
        }

        private Expression InitPropertiesAndFields(NewExpression newServiceExpr,
            Request request, IEnumerable<PropertyOrFieldServiceInfo> members)
        {
            var container = request.Container;
            var bindings = new List<MemberBinding>();
            foreach (var member in members)
                if (member != null)
                {
                    Expression memberExpr;
                    var memberRequest = request.Push(member);
                    if (member.Details.HasCustomValue)
                    {
                        var customValue = member.Details.CustomValue;
                        if (customValue != null)
                            customValue.ThrowIfNotOf(memberRequest.ServiceType, Error.InjectedCustomValueIsOfDifferentType, memberRequest);
                        memberExpr = container.GetOrAddStateItemExpression(customValue, memberRequest.ServiceType);
                    }
                    else
                    {
                        var memberFactory = container.ResolveFactory(memberRequest);
                        memberExpr = memberFactory == null ? null : memberFactory.GetExpressionOrDefault(memberRequest);
                        if (memberExpr == null && request.IfUnresolved == IfUnresolved.ReturnDefault)
                            return null;
                    }

                    if (memberExpr != null)
                        bindings.Add(Expression.Bind(member.Member, memberExpr));
                }

            return bindings.Count == 0 ? (Expression)newServiceExpr : Expression.MemberInit(newServiceExpr, bindings);
        }

        private static Type[] GetClosedTypeArgsOrNullForOpenGenericType(
            Type openImplType, Type closedServiceType, Request request, bool ifErrorReturnDefault)
        {
            var serviceTypeArgs = closedServiceType.GetGenericParamsAndArgs();
            var serviceTypeGenericDef = closedServiceType.GetGenericTypeDefinition();

            var implTypeParams = openImplType.GetGenericParamsAndArgs();
            var implTypeArgs = new Type[implTypeParams.Length];

            var implementedTypes = openImplType.GetImplementedTypes();

            var matchFound = false;
            for (var i = 0; !matchFound && i < implementedTypes.Length; ++i)
            {
                var implementedType = implementedTypes[i];
                if (implementedType.IsOpenGeneric() &&
                    implementedType.GetGenericDefinitionOrNull() == serviceTypeGenericDef)
                {
                    matchFound = MatchServiceWithImplementedTypeParams(
                        implTypeArgs, implTypeParams, implementedType.GetGenericParamsAndArgs(), serviceTypeArgs);
                }
            }

            if (!matchFound)
                return ifErrorReturnDefault || request.IfUnresolved == IfUnresolved.ReturnDefault ? null
                    : Throw.For<Type[]>(Error.NoMatchedImplementedTypesWithServiceType,
                        openImplType, implementedTypes, request);

            MatchOpenGenericConstraints(implTypeParams, implTypeArgs);

            var notMatchedIndex = Array.IndexOf(implTypeArgs, null);
            if (notMatchedIndex != -1)
                return ifErrorReturnDefault || request.IfUnresolved == IfUnresolved.ReturnDefault ? null
                    : Throw.For<Type[]>(Error.NotFoundOpenGenericImplTypeArgInService,
                        openImplType, implTypeParams[notMatchedIndex], request);

            return implTypeArgs;
        }

        private static void MatchOpenGenericConstraints(Type[] implTypeParams, Type[] implTypeArgs)
        {
            for (var i = 0; i < implTypeParams.Length; i++)
            {
                var implTypeArg = implTypeArgs[i];
                if (implTypeArg == null) continue; // skip yet unknown type arg

                var implTypeParam = implTypeParams[i];
                var implTypeParamConstraints = implTypeParam.GetGenericParamConstraints();
                if (implTypeParamConstraints.IsNullOrEmpty()) continue; // skip case with no constraints

                var constraintMatchFound = false;
                for (var j = 0; !constraintMatchFound && j < implTypeParamConstraints.Length; ++j)
                {
                    var implTypeParamConstraint = implTypeParamConstraints[j];
                    if (implTypeParamConstraint != implTypeArg &&
                        implTypeParamConstraint.IsOpenGeneric())
                    {
                        // match type parameters inside constraint
                        var implTypeArgArgs = implTypeArg.IsGeneric()
                            ? implTypeArg.GetGenericParamsAndArgs()
                            : new[] { implTypeArg };

                        var implTypeParamConstraintParams = implTypeParamConstraint.GetGenericParamsAndArgs();
                        constraintMatchFound = MatchServiceWithImplementedTypeParams(
                            implTypeArgs, implTypeParams, implTypeParamConstraintParams, implTypeArgArgs);
                    }
                }
            }
        }

        private static bool MatchServiceWithImplementedTypeParams(
            Type[] resultImplArgs, Type[] implParams, Type[] serviceParams, Type[] serviceArgs,
            int resultCount = 0)
        {
            for (var i = 0; i < serviceParams.Length; i++)
            {
                var serviceArg = serviceArgs[i];
                var implementedParam = serviceParams[i];
                if (implementedParam.IsGenericParameter)
                {
                    var paramIndex = implParams.IndexOf(implementedParam);
                    if (paramIndex != -1)
                    {
                        if (resultImplArgs[paramIndex] == null)
                        {
                            resultImplArgs[paramIndex] = serviceArg;
                            if (++resultCount == resultImplArgs.Length)
                                return true;
                        }
                        else if (resultImplArgs[paramIndex] != serviceArg)
                            return false; // more than one service type arg is matching with single impl type param
                    }
                }
                else if (implementedParam != serviceArg)
                {
                    if (!implementedParam.IsOpenGeneric() ||
                        implementedParam.GetGenericDefinitionOrNull() != serviceArg.GetGenericDefinitionOrNull())
                        return false; // type param and arg are of different types

                    if (!MatchServiceWithImplementedTypeParams(resultImplArgs, implParams,
                        implementedParam.GetGenericParamsAndArgs(), serviceArg.GetGenericParamsAndArgs()))
                        return false; // nested match failed due either one of above reasons.
                }
            }

            return true;
        }

        private static FactoryMethod GetClosedFactoryMethodOrDefault(
            FactoryMethod factoryMethod, Type[] serviceTypeArgs, Request request,
            bool shouldReturnOnError = false)
        {
            var factoryMember = factoryMethod.ConstructorOrMethodOrMember;
            var factoryInfo = factoryMethod.FactoryServiceInfo;

            var factoryResultType = factoryMember.GetReturnTypeOrDefault();
            var implTypeParams = factoryResultType.IsGenericParameter
                ? new[] { factoryResultType }
                : factoryResultType.GetGenericParamsAndArgs();

            // Get method declaring type, and if its open-generic,
            // then close it first. It is required to get actual method.
            var factoryImplType = factoryMember.DeclaringType.ThrowIfNull();
            if (factoryImplType.IsOpenGeneric())
            {
                var factoryImplTypeParams = factoryImplType.GetGenericParamsAndArgs();
                var resultFactoryImplTypeArgs = new Type[factoryImplTypeParams.Length];

                var isFactoryImplTypeClosed = MatchServiceWithImplementedTypeParams(
                    resultFactoryImplTypeArgs, factoryImplTypeParams,
                    implTypeParams, serviceTypeArgs);

                if (!isFactoryImplTypeClosed)
                    return shouldReturnOnError || request.IfUnresolved == IfUnresolved.ReturnDefault ? null
                        : Throw.For<FactoryMethod>(Error.NoMatchedFactoryMethodDeclaringTypeWithServiceTypeArgs,
                            factoryImplType, new StringBuilder().Print(serviceTypeArgs, itemSeparator: ", "), request);

                // For instance factory match its service type from the implementation factory type.
                if (factoryInfo != null)
                {
                    // Open-generic service type is always normalized as generic type definition
                    var factoryServiceType = factoryInfo.ServiceType;

                    // Look for service type equivalent within factory implementation type base classes and interfaces,
                    // because we need identical type arguments to match.
                    if (factoryServiceType != factoryImplType)
                        factoryServiceType = factoryImplType.GetImplementedTypes()
                            .FindFirst(t => t.IsGeneric() && t.GetGenericTypeDefinition() == factoryServiceType)
                            .ThrowIfNull();

                    var factoryServiceTypeParams = factoryServiceType.GetGenericParamsAndArgs();
                    var resultFactoryServiceTypeArgs = new Type[factoryServiceTypeParams.Length];

                    var isFactoryServiceTypeClosed = MatchServiceWithImplementedTypeParams(
                        resultFactoryServiceTypeArgs, factoryServiceTypeParams,
                        factoryImplTypeParams, resultFactoryImplTypeArgs);

                    // Replace factory info with close factory service type
                    if (isFactoryServiceTypeClosed)
                    {
                        MatchOpenGenericConstraints(factoryImplTypeParams, resultFactoryImplTypeArgs);

                        factoryServiceType = factoryServiceType.GetGenericTypeDefinition().ThrowIfNull();
                        var closedFactoryServiceType = Throw.IfThrows<ArgumentException, Type>(
                            () => factoryServiceType.MakeGenericType(resultFactoryServiceTypeArgs),
                            !shouldReturnOnError && request.IfUnresolved == IfUnresolved.Throw,
                            Error.NoMatchedGenericParamConstraints, factoryServiceType, request);

                        if (closedFactoryServiceType == null)
                            return null;

                        // Copy factory info with closed factory type
                        factoryInfo = ServiceInfo.Of(closedFactoryServiceType)
                            .WithDetails(factoryInfo.Details, request);
                    }
                }

                MatchOpenGenericConstraints(factoryImplTypeParams, resultFactoryImplTypeArgs);

                // Close the factory type implementation
                // and get factory member to use from it.
                var closedFactoryImplType = Throw.IfThrows<ArgumentException, Type>(
                    () => factoryImplType.MakeGenericType(resultFactoryImplTypeArgs),
                    !shouldReturnOnError && request.IfUnresolved == IfUnresolved.Throw,
                    Error.NoMatchedGenericParamConstraints, factoryImplType, request);

                if (closedFactoryImplType == null)
                    return null;

                // Find corresponding member again, now from closed type
                var factoryMethodBase = factoryMember as MethodBase;
                if (factoryMethodBase != null)
                {
                    var factoryMethodParameters = factoryMethodBase.GetParameters();
                    var targetMethods = closedFactoryImplType.GetMembers(t => t.DeclaredMethods, includeBase: true)
                        .Match(m => m.Name == factoryMember.Name && m.GetParameters().Length == factoryMethodParameters.Length)
                        .ToArrayOrSelf();

                    if (targetMethods.Length == 1)
                        factoryMember = targetMethods[0];
                    else // Fallback to MethodHandle only if methods have similar signatures
                    {
                        var methodHandleProperty = typeof(MethodBase).GetPropertyOrNull("MethodHandle")
                            .ThrowIfNull(Error.OpenGenericFactoryMethodDeclaringTypeIsNotSupportedOnThisPlatform,
                                factoryImplType, closedFactoryImplType, factoryMethodBase.Name);
                        factoryMember = MethodBase.GetMethodFromHandle(
                            (RuntimeMethodHandle)methodHandleProperty.GetValue(factoryMethodBase, ArrayTools.Empty<object>()),
                            closedFactoryImplType.TypeHandle);
                    }
                }
                else if (factoryMember is FieldInfo)
                {
                    factoryMember = closedFactoryImplType.GetMembers(t => t.DeclaredFields, includeBase: true)
                        .Single(f => f.Name == factoryMember.Name);
                }
                else if (factoryMember is PropertyInfo)
                {
                    factoryMember = closedFactoryImplType.GetMembers(t => t.DeclaredProperties, includeBase: true)
                        .Single(f => f.Name == factoryMember.Name);
                }
            }

            // If factory method is actual method and still open-generic after closing its declaring type,
            // then match remaining method type parameters and make closed method
            var openFactoryMethod = factoryMember as MethodInfo;
            if (openFactoryMethod != null && openFactoryMethod.ContainsGenericParameters)
            {
                var methodTypeParams = openFactoryMethod.GetGenericArguments();
                var resultMethodTypeArgs = new Type[methodTypeParams.Length];

                var isMethodClosed = MatchServiceWithImplementedTypeParams(
                    resultMethodTypeArgs, methodTypeParams, implTypeParams, serviceTypeArgs);

                if (!isMethodClosed)
                    return shouldReturnOnError || request.IfUnresolved == IfUnresolved.ReturnDefault ? null
                        : Throw.For<FactoryMethod>(Error.NoMatchedFactoryMethodWithServiceTypeArgs,
                            openFactoryMethod, new StringBuilder().Print(serviceTypeArgs, itemSeparator: ", "),
                            request);

                MatchOpenGenericConstraints(methodTypeParams, resultMethodTypeArgs);

                factoryMember = Throw.IfThrows<ArgumentException, MethodInfo>(
                    () => openFactoryMethod.MakeGenericMethod(resultMethodTypeArgs),
                    !shouldReturnOnError && request.IfUnresolved == IfUnresolved.Throw,
                    Error.NoMatchedGenericParamConstraints, factoryImplType, request);

                if (factoryMember == null)
                    return null;
            }

            return FactoryMethod.Of(factoryMember, factoryInfo);
        }


        #endregion
    }

    /// <summary>Creates service expression using client provided expression factory delegate.</summary>
    internal sealed class ExpressionFactory : Factory
    {
        /// <summary>Wraps provided delegate into factory.</summary>
        /// <param name="getServiceExpression">Delegate that will be used internally to create service expression.</param>
        /// <param name="reuse">(optional) Reuse.</param> <param name="setup">(optional) Setup.</param>
        public ExpressionFactory(Func<Request, Expression> getServiceExpression, IReuse reuse = null, Setup setup = null)
            : base(reuse, setup)
        {
            _getServiceExpression = getServiceExpression.ThrowIfNull();
        }

        /// <summary>Creates service expression using wrapped delegate.</summary>
        /// <param name="request">Request to resolve.</param> <returns>Expression returned by stored delegate.</returns>
        public override Expression CreateExpressionOrDefault(Request request)
        {
            return _getServiceExpression(request);
        }

        private readonly Func<Request, Expression> _getServiceExpression;
    }

    // todo: v3: Remove
    /// <summary>Obsolete: replaced with UsedInstanceFactory.</summary>
    internal sealed class InstanceFactory : Factory
    {
        private object _instance;

        /// <summary>Instance type, or null for null instance.</summary>
        public override Type ImplementationType
        {
            get { return _instance == null ? null : _instance.GetType(); }
        }

        /// <summary>Creates factory wrapping provided instance.</summary>
        /// <param name="instance">Instance to register.</param>
        /// <param name="reuse"></param> <param name="setup"></param>
        public InstanceFactory(object instance, IReuse reuse, Setup setup) : base(reuse, setup)
        {
            _instance = instance;
        }

        /// <summary>Replaces current instance with new one.</summary> <param name="newInstance"></param>
        public void ReplaceInstance(object newInstance)
        {
            Interlocked.Exchange(ref _instance, newInstance);
        }

        /// <summary>The method should not be really called. That's why it returns exception throwing expression.</summary>
        /// <param name="request">Context</param> <returns>Expression throwing exception.</returns>
        public override Expression CreateExpressionOrDefault(Request request)
        {
            return Expression.Constant(_instance);
        }

        /// <summary>Puts instance directly to available scope.</summary>
        protected override Expression ApplyReuse(Expression _, IReuse reuse, bool tracksTransientDisposableIgnored, Request request)
        {
            var scopedReuse = reuse as CurrentScopeReuse;
            var scope = scopedReuse != null
                ? request.Scopes.GetCurrentNamedScope(scopedReuse.Name, throwIfNotFound: true)
                : request.SingletonScope;

            var scopedId = scope.GetScopedItemIdOrSelf(FactoryID);
            var instance = _instance;
            scope.GetOrAdd(scopedId, () => instance);

            var instanceType = instance == null || instance.GetType().IsValueType() ? typeof(object) : instance.GetType();
            var instanceExpr = Expression.Constant(instance, instanceType);

            var reuseV3 = (reuse as IReuseV3).ThrowIfNull();
            var serviceExpr = reuseV3.Apply(request, tracksTransientDisposableIgnored, instanceExpr);

            // Unwrap WeakReference and/or array preventing disposal
            if (Setup.WeaklyReferenced)
                serviceExpr = Expression.Call(typeof(ThrowInGeneratedCode), "ThrowNewErrorIfNull",
                    ArrayTools.Empty<Type>(),
                    Expression.Property(Expression.Convert(serviceExpr, typeof(WeakReference)), "Target"),
                    Expression.Constant(Error.Messages[Error.WeakRefReuseWrapperGCed]));

            if (Setup.PreventDisposal)
                serviceExpr = Expression.ArrayIndex(
                    Expression.Convert(serviceExpr, typeof(object[])),
                    Expression.Constant(0, typeof(int)));

            return Expression.Convert(serviceExpr, request.ServiceType);
        }
    }

    /// <summary>This factory is the thin wrapper for user provided delegate
    /// and where possible it uses delegate directly: without converting it to expression.</summary>
    internal sealed class DelegateFactory : Factory
    {
        /// <summary>Non-abstract closed implementation type.</summary>
        public override Type ImplementationType { get { return _knownImplementationType; } }

        /// <summary>Creates factory by providing:</summary>
        /// <param name="factoryDelegate">Specified service creation delegate.</param>
        /// <param name="reuse">(optional) Reuse behavior for created service.</param>
        /// <param name="setup">(optional) Additional settings.</param>
        /// <param name="knownImplementationType">(optional) Implementation type if known, e.g. when registering existing instance.</param>
        public DelegateFactory(Func<IResolver, object> factoryDelegate,
            IReuse reuse = null, Setup setup = null, Type knownImplementationType = null)
            : base(reuse, setup)
        {
            _factoryDelegate = factoryDelegate.ThrowIfNull();
            _knownImplementationType = knownImplementationType;
        }

        /// <summary>Create expression by wrapping call to stored delegate with provided request.</summary>
        /// <param name="request">Request to resolve. It will be stored in resolution state to be passed to delegate on actual resolve.</param>
        /// <returns>Created delegate call expression.</returns>
        public override Expression CreateExpressionOrDefault(Request request)
        {
            var factoryDelegateExpr = request.Container.GetOrAddStateItemExpression(_factoryDelegate);
            var resolverExpr = Container.GetResolverExpr(request);
            var invokeExpr = Expression.Invoke(factoryDelegateExpr, resolverExpr);
            return Expression.Convert(invokeExpr, request.GetActualServiceType());
        }

        /// <summary>If possible returns delegate directly, without creating expression trees, just wrapped in <see cref="FactoryDelegate"/>.
        /// If decorator found for request then factory fall-backs to expression creation.</summary>
        /// <param name="request">Request to resolve.</param>
        /// <returns>Factory delegate directly calling wrapped delegate, or invoking expression if decorated.</returns>
        public override FactoryDelegate GetDelegateOrDefault(Request request)
        {
            request = request.WithResolvedFactory(this);

            if (FactoryType == FactoryType.Service &&
                request.Container.GetDecoratorExpressionOrDefault(request) != null)
                return base.GetDelegateOrDefault(request); // use expression creation

            if (request.Reuse != DryIoc.Reuse.Transient)
                return base.GetDelegateOrDefault(request); // use expression creation

            return (state, r, scope) => _factoryDelegate(r.Resolver);
        }

        private readonly Func<IResolver, object> _factoryDelegate;
        private readonly Type _knownImplementationType;
    }

    /// <summary>Should return value stored in scope.</summary>
    internal delegate object CreateScopedValue();

    /// <summary>Lazy object storage that will create object with provided factory on first access,
    /// then will be returning the same object for subsequent access.</summary>
    internal interface IScope : IDisposable
    {
        /// <summary>Parent scope in scope stack. Null for root scope.</summary>
        IScope Parent { get; }

        /// <summary>Optional name object associated with scope.</summary>
        object Name { get; }

        /// <summary>Creates, stores, and returns stored object.</summary>
        /// <param name="id">Unique ID to find created object in subsequent calls.</param>
        /// <param name="createValue">Delegate to create object. It will be used immediately, and reference to delegate will not be stored.</param>
        /// <returns>Created and stored object.</returns>
        /// <remarks>Scope does not store <paramref name="createValue"/> (no memory leak here),
        /// it stores only result of <paramref name="createValue"/> call.</remarks>
        object GetOrAdd(int id, CreateScopedValue createValue);

        /// <summary>Sets (replaces) value at specified id, or adds value if no existing id found.</summary>
        /// <param name="id">To set value at.</param> <param name="item">Value to set.</param>
        void SetOrAdd(int id, object item);

        /// <summary>Creates id/index for new item to be stored in scope.
        /// If separate index is not supported then just returns back passed <paramref name="externalId"/>.</summary>
        /// <param name="externalId">Id to be mapped to new item id/index</param>
        /// <returns>New it/index or just passed <paramref name="externalId"/></returns>
        int GetScopedItemIdOrSelf(int externalId);
    }

    /// <summary>Scope implementation which will dispose stored <see cref="IDisposable"/> items on its own dispose.
    /// Locking is used internally to ensure that object factory called only once.</summary>
    [Janitor.SkipWeaving]
    internal sealed class Scope : IScope
    {
        /// <summary>Parent scope in scope stack. Null for root scope.</summary>
        public IScope Parent { get; private set; }

        /// <summary>Optional name object associated with scope.</summary>
        public object Name { get; private set; }

        /// <summary>Create scope with optional parent and name.</summary>
        /// <param name="parent">Parent in scope stack.</param> <param name="name">Associated name object.</param>
        public Scope(IScope parent = null, object name = null)
        {
            Parent = parent;
            Name = name;
            _items = ImTreeMapIntToObj.Empty;
        }

        /// <summary>Just returns back <paramref name="externalId"/> without any changes.</summary>
        /// <param name="externalId">Id will be returned back.</param> <returns><paramref name="externalId"/>.</returns>
        public int GetScopedItemIdOrSelf(int externalId)
        {
            return externalId;
        }

        /// <summary><see cref="IScope.GetOrAdd"/> for description.
        /// Will throw <see cref="ContainerException"/> if scope is disposed.</summary>
        /// <param name="id">Unique ID to find created object in subsequent calls.</param>
        /// <param name="createValue">Delegate to create object. It will be used immediately, and reference to delegate will Not be stored.</param>
        /// <returns>Created and stored object.</returns>
        /// <exception cref="ContainerException">if scope is disposed.</exception>
        public object GetOrAdd(int id, CreateScopedValue createValue)
        {
            return _items.GetValueOrDefault(id) ?? TryGetOrAdd(id, createValue);
        }

        private object TryGetOrAdd(int id, CreateScopedValue createValue)
        {
            if (_disposed == 1)
                Throw.It(Error.ScopeIsDisposed);

            if (id == -1) // disposable transient
            {
                var transient = createValue();
                TrackDisposable(transient);
                return transient;
            }

            object item;
            lock (_locker)
            {
                item = _items.GetValueOrDefault(id);
                if (item != null)
                    return item;

                item = createValue();
                TrackDisposable(item);
            }

            var items = _items;
            var newItems = items.AddOrUpdate(id, item);

            // if _items were not changed so far then use them, otherwise (if changed) do ref swap;
            if (Interlocked.CompareExchange(ref _items, newItems, items) != items)
                Ref.Swap(ref _items, _ => _.AddOrUpdate(id, item));
            return item;
        }

        /// <summary>Sets (replaces) value at specified id, or adds value if no existing id found.</summary>
        /// <param name="id">To set value at.</param> <param name="item">Value to set.</param>
        public void SetOrAdd(int id, object item)
        {
            Throw.If(_disposed == 1, Error.ScopeIsDisposed);
            Ref.Swap(ref _items, items => items.AddOrUpdate(id, item));
        }

        /// <summary>Disposes all stored <see cref="IDisposable"/> objects and nullifies object storage.</summary>
        /// <remarks>If item disposal throws exception, then it won't be propagated outside,
        /// so the rest of the items could be disposed.</remarks>
        public void Dispose()
        {
            if (Interlocked.CompareExchange(ref _disposed, 1, 0) == 1)
                return;

            var disposables = _disposables;
            if (!disposables.IsEmpty)
                foreach (var disposable in disposables.Enumerate())
                    ScopedDisposableHandling.DisposeItem(disposable.Value);

            _disposables = ImTreeMapIntToObj.Empty;
            _items = ImTreeMapIntToObj.Empty;
        }

        /// <summary>Prints scope info (name and parent) to string for debug purposes.</summary>
        /// <returns>String representation.</returns>
        public override string ToString()
        {
            return "{Name=" + (Name ?? "<no-name>")
                   + (Parent == null ? string.Empty : ", Parent=" + Parent)
                   + "}";
        }

        #region Implementation

        private ImTreeMapIntToObj _items;
        private ImTreeMapIntToObj _disposables = ImTreeMapIntToObj.Empty;
        private int _nextDisposablelID = int.MaxValue;
        private int _disposed;

        // Sync root is required to create object only once. The same reason as for Lazy<T>.
        private readonly object _locker = new object();

        private void TrackDisposable(object item)
        {
            if (ScopedDisposableHandling.TryUnwrapDisposable(item) != null)
            {
                // Decrement here is because dispose should happen in reverse resolution order
                // By adding items with decreasing IDs we get rid off ordering on Dispose.
                var disposableID = Interlocked.Decrement(ref _nextDisposablelID);
                Ref.Swap(ref _disposables, d => d.AddOrUpdate(disposableID, item));
            }
        }

        #endregion
    }

    internal static class ScopedDisposableHandling
    {
        public static IDisposable TryUnwrapDisposable(object item)
        {
            var disposable = item as IDisposable;
            if (disposable != null)
                return disposable;

            // Unwrap WeakReference if item wrapped in it.
            var weakRefItem = item as WeakReference;
            if (weakRefItem != null)
                return weakRefItem.Target as IDisposable;

            return null;
        }

        public static void DisposeItem(object item)
        {
            var disposable = TryUnwrapDisposable(item);
            if (disposable != null)
            {
                try { disposable.Dispose(); }
                catch (Exception)
                {
                    // NOTE: Ignoring disposing exception, they not so important for program to proceed.
                }
            }
        }
    }

    /// <summary>Different from <see cref="Scope"/> so that uses single array of items for fast access.
    /// The array structure is:
    /// items[0] is reserved for storing object[][] buckets.
    /// items[1-BucketSize] are used for storing actual singletons up to (BucketSize-1) index
    /// Buckets structure is variable number of object[BucketSize] buckets used to storing items with index >= BucketSize.
    /// The structure allows very fast access to up to <see cref="BucketSize"/> singletons - it just array access: items[itemIndex]
    /// For further indexes it is a fast O(1) access: ((object[][])items[i])[i / BucketSize - 1][i % BucketSize]
    /// </summary>
    [Janitor.SkipWeaving]
    internal sealed class SingletonScope : IScope
    {
        /// <summary>Parent scope in scope stack. Null for root scope.</summary>
        public IScope Parent { get; private set; }

        /// <summary>Optional name object associated with scope.</summary>
        public object Name { get; private set; }

        /// <summary>Amount of items in item array.</summary>
        public static readonly int BucketSize = 32;

        /// <summary>Returns true if scope was disposed.</summary>
        public bool IsDisposed { get { return _disposed == 1; } }

        /// <summary>Creates scope.</summary>
        /// <param name="parent">Parent in scope stack.</param> <param name="name">Associated name object.</param>
        public SingletonScope(IScope parent = null, object name = null)
        {
            Parent = parent;
            Name = name;
            Items = new object[BucketSize];
            _factoryIdToIndexMap = ImTreeMapIntToObj.Empty;
            _lastItemIndex = 0;
        }

        internal int IndexOf(int factoryId)
        {
            var indexObj = _factoryIdToIndexMap.GetValueOrDefault(factoryId);
            return indexObj == null ? -1 : (int)indexObj;
        }

        internal object GetOrDefault(int index)
        {
            if (index < BucketSize)
                return Items[index];

            var buckets = Items[0] as object[][];
            if (buckets == null)
                return null;

            var bucketIndex = (index / BucketSize) - 1; // bucket indices start with 0
            if (bucketIndex >= buckets.Length)
                return null;

            var bucket = buckets[bucketIndex];
            if (bucket == null)
                return null;

            return bucket[index % BucketSize];
        }

        /// <summary>Adds mapping between provide id and index for new stored item. Returns index.</summary>
        /// <param name="externalId">External id mapped to internal index.</param>
        /// <returns>Already mapped index, or newly created.</returns>
        public int GetScopedItemIdOrSelf(int externalId)
        {
            return (int)(_factoryIdToIndexMap.GetValueOrDefault(externalId) ?? MapExternalId(externalId));
        }

        private object MapExternalId(int externalId)
        {
            object index = null;
            Ref.Swap(ref _factoryIdToIndexMap, map =>
            {
                index = map.GetValueOrDefault(externalId);
                return index == null
                    ? map.AddOrUpdate(externalId, index = Interlocked.Increment(ref _lastItemIndex))
                    : map;
            });

            return index;
        }

        /// <summary><see cref="IScope.GetOrAdd"/> for description.
        /// Will throw <see cref="ContainerException"/> if scope is disposed.</summary>
        /// <param name="id">Unique ID to find created object in subsequent calls.</param>
        /// <param name="createValue">Delegate to create object. It will be used immediately, and reference to delegate will Not be stored.</param>
        /// <returns>Created and stored object.</returns>
        /// <exception cref="ContainerException">if scope is disposed.</exception>
        public object GetOrAdd(int id, CreateScopedValue createValue)
        {
            return id < BucketSize && id >= 0 // it could be -1 for disposable transients
                ? (Items[id] ?? GetOrAddItem(Items, id, createValue))
                : GetOrAddItem(id, createValue);
        }

        /// <summary>Sets (replaces) value at specified id, or adds value if no existing id found.</summary>
        /// <param name="id">To set value at.</param> <param name="item">Value to set.</param>
        public void SetOrAdd(int id, object item)
        {
            Throw.If(_disposed == 1, Error.ScopeIsDisposed);
            if (id < BucketSize)
                Items[id] = item;
            else
            {
                var bucket = GetOrAddBucket(id);
                var indexInBucket = id % BucketSize;
                bucket[indexInBucket] = item;
            }

            TrackDisposable(item);
        }

        /// <summary>Disposes all stored <see cref="IDisposable"/> objects and nullifies object storage.</summary>
        /// <remarks>If item disposal throws exception, then it won't be propagated outside, so the rest of the items could be disposed.</remarks>
        public void Dispose()
        {
            if (Interlocked.CompareExchange(ref _disposed, 1, 0) == 1)
                return;

            var disposables = _disposables;
            if (!disposables.IsEmpty)
                foreach (var disposable in disposables.Enumerate())
                    ScopedDisposableHandling.DisposeItem(disposable.Value);

            _disposables = ImTreeMapIntToObj.Empty;
            _factoryIdToIndexMap = ImTreeMapIntToObj.Empty;
            Items = ArrayTools.Empty<object>();
        }

        #region Implementation

        private readonly object _syncRoot = new object();

        private ImTreeMapIntToObj _factoryIdToIndexMap;
        private int _lastItemIndex;
        private int _disposed;

        /// <summary>value at 0 index is reserved for [][] structure to accommodate more values</summary>
        internal object[] Items;

        private ImTreeMapIntToObj _disposables = ImTreeMapIntToObj.Empty;
        private int _nextDisposablelID = int.MaxValue;

        private void TrackDisposable(object item)
        {
            if (ScopedDisposableHandling.TryUnwrapDisposable(item) != null)
            {
                // Decrement here is because dispose should happen in reverse resolution order
                // By adding items with decreasing IDs we get rid off ordering on Dispose.
                var disposableID = Interlocked.Decrement(ref _nextDisposablelID);
                Ref.Swap(ref _disposables, d => d.AddOrUpdate(disposableID, item));
            }
        }

        private object GetOrAddItem(int index, CreateScopedValue createValue)
        {
            if (index == -1) // disposable transient
            {
                var transient = createValue();
                TrackDisposable(transient);
                return transient;
            }

            var bucket = GetOrAddBucket(index);
            index = index % BucketSize;
            return GetOrAddItem(bucket, index, createValue);
        }

        private object GetOrAddItem(object[] bucket, int index, CreateScopedValue createValue)
        {
            var value = bucket[index];
            if (value != null)
                return value;

            lock (_syncRoot)
            {
                value = bucket[index];
                if (value == null)
                {
                    value = createValue();
                    TrackDisposable(value);
                    bucket[index] = value;
                }
            }

            return value;
        }

        // find if bucket already created starting from 0
        // if not - create new buckets array and copy old buckets into it
        private object[] GetOrAddBucket(int index)
        {
            var bucketIndex = (index / BucketSize) - 1;
            var buckets = Items[0] as object[][];
            if (buckets == null ||
                buckets.Length < bucketIndex + 1 ||
                buckets[bucketIndex] == null)
            {
                Ref.Swap(ref Items[0], value =>
                {
                    if (value == null)
                    {
                        var newBuckets = new object[bucketIndex + 1][];
                        newBuckets[bucketIndex] = new object[BucketSize];
                        return newBuckets;
                    }

                    var oldBuckets = (object[][])value;
                    if (oldBuckets.Length < bucketIndex + 1)
                    {
                        var newBuckets = new object[bucketIndex + 1][];
                        Array.Copy(oldBuckets, 0, newBuckets, 0, oldBuckets.Length);
                        newBuckets[bucketIndex] = new object[BucketSize];
                        return newBuckets;
                    }

                    if (oldBuckets[bucketIndex] == null)
                        oldBuckets[bucketIndex] = new object[BucketSize];

                    return value;
                });
            }

            var bucket = ((object[][])Items[0])[bucketIndex];
            return bucket;
        }

        #endregion
    }

    /// <summary>Delegate to get new scope from old/existing current scope.</summary>
    /// <param name="oldScope">Old/existing scope to change.</param>
    /// <returns>New scope or old if do not want to change current scope.</returns>
    internal delegate IScope SetCurrentScopeHandler(IScope oldScope);

    /// <summary>Provides ambient current scope and optionally scope storage for container,
    /// examples are HttpContext storage, Execution context, Thread local.</summary>
    internal interface IScopeContext : IDisposable
    {
        /// <summary>Name associated with context root scope - so the reuse may find scope context.</summary>
        string RootScopeName { get; }

        /// <summary>Returns current scope or null if no ambient scope available at the moment.</summary>
        /// <returns>Current scope or null.</returns>
        IScope GetCurrentOrDefault();

        /// <summary>Changes current scope using provided delegate. Delegate receives current scope as input and
        /// should return new current scope.</summary>
        /// <param name="setCurrentScope">Delegate to change the scope.</param>
        /// <remarks>Important: <paramref name="setCurrentScope"/> may be called multiple times in concurrent environment.
        /// Make it predictable by removing any side effects.</remarks>
        /// <returns>New current scope. So it is convenient to use method in "using (var newScope = ctx.SetCurrent(...))".</returns>
        IScope SetCurrent(SetCurrentScopeHandler setCurrentScope);
    }

    /// <summary>Tracks one current scope per thread, so the current scope in different tread would be different or null,
    /// if not yet tracked. Context actually stores scope references internally, so it should be disposed to free them.</summary>
    [Janitor.SkipWeaving]
    internal sealed class ThreadScopeContext : IScopeContext, IDisposable
    {
        /// <summary>Provides static name for context. It is OK because its constant.</summary>
        public static readonly string ScopeContextName = "ThreadScopeContext";

        /// <summary>Key to identify context.</summary>
        public string RootScopeName { get { return ScopeContextName; } }

        /// <summary>Returns current scope in calling Thread or null, if no scope tracked.</summary>
        /// <returns>Found scope or null.</returns>
        public IScope GetCurrentOrDefault()
        {
            return _scopes.GetValueOrDefault(Portable.GetCurrentManagedThreadID()) as IScope;
        }

        /// <summary>Change current scope for the calling Thread.</summary>
        /// <param name="setCurrentScope">Delegate to change the scope given current one (or null).</param>
        /// <remarks>Important: <paramref name="setCurrentScope"/> may be called multiple times in concurrent environment.
        /// Make it predictable by removing any side effects.</remarks>
        public IScope SetCurrent(SetCurrentScopeHandler setCurrentScope)
        {
            var threadId = Portable.GetCurrentManagedThreadID();
            IScope newScope = null;
            Ref.Swap(ref _scopes, scopes =>
                scopes.AddOrUpdate(threadId, newScope = setCurrentScope(scopes.GetValueOrDefault(threadId) as IScope)));
            return newScope;
        }

        /// <summary>Disposed all stored/tracked scopes and empties internal scope storage.</summary>
        public void Dispose()
        {
            if (!_scopes.IsEmpty)
                foreach (var scope in _scopes.Enumerate().Where(scope => scope.Value is IDisposable))
                    ((IDisposable)scope.Value).Dispose();
            _scopes = ImTreeMapIntToObj.Empty;
        }

        private ImTreeMapIntToObj _scopes = ImTreeMapIntToObj.Empty;
    }

    // todo: v3: remove
    /// <summary>Obsolete: until v3 replaced by <see cref="IReuseV3"/>.</summary>
    internal interface IReuse
    {
        /// <summary>Relative to other reuses lifespan value.</summary>
        int Lifespan { get; }

        /// <summary>Locates or creates scope to store reused service objects.</summary>
        /// <param name="request">Request to get context information or for example store something in resolution state.</param>
        /// <returns>Located scope.</returns>
        IScope GetScopeOrDefault(Request request);

        // todo: v3: remove
        /// <summary>ObsoIReuseV3.ApplyApply"/> instead.</summary>
        Expression GetScopeExpression(Request request);

        /// <summary>Returns special id/index to lookup scoped item, or original passed factory id otherwise.</summary>
        /// <param name="factoryID">Id to map to item id/index.</param> <param name="request">Context to get access to scope.</param>
        /// <returns>id/index or source factory id.</returns>
        int GetScopedItemIdOrSelf(int factoryID, Request request);
    }

    // todo: v3: rename to IReuse
    // todo: v3: add object[] Names property
    /// <summary>Simplified scope agnostic reuse abstraction. More easy to implement,
    ///  and more powerful as can be based on other storage beside reuse.</summary>
    internal interface IReuseV3 : IConvertibleToExpression
    {
        /// <summary>Relative to other reuses lifespan value.</summary>
        int Lifespan { get; }

        // todo: v3: add. It also may be interpreted as object[] Names for matching with multiple scopes
        // object Name { get; }

        // todo: v3: remove trackTransientDisposable param as it is available from Request param.
        /// <summary>Returns composed expression.</summary>
        /// <param name="request">info</param>
        /// <param name="trackTransientDisposable">Indicates that item should be tracked.</param>
        /// <param name="createItemExpr">Service creation expression</param>
        /// <returns>Subject</returns>
        Expression Apply(Request request, bool trackTransientDisposable, Expression createItemExpr);

        /// <summary>Returns true if reuse can be applied: may check if scope or other reused item storage is present.</summary>
        /// <param name="request">Service request.</param> <returns>Check result.</returns>
        bool CanApply(Request request);
    }

    /// <summary>Returns container bound scope for storing singleton objects.</summary>
    internal sealed class SingletonReuse : IReuse, IReuseV3
    {
        /// <summary>Relative to other reuses lifespan value.</summary>
        public int Lifespan { get { return 1000; } }

        /// <summary>Returns expression call to GetOrAddItem.</summary>
        public Expression Apply(Request request, bool trackTransientDisposable, Expression createItemExpr)
        {
            var itemId = trackTransientDisposable ? -1
                : request.SingletonScope.GetScopedItemIdOrSelf(request.FactoryID);
            return Expression.Call(Container.SingletonScopeExpr, "GetOrAdd", ArrayTools.Empty<Type>(),
                Expression.Constant(itemId), Expression.Lambda<CreateScopedValue>(createItemExpr));
        }

        /// <summary>Returns true because singleton is always available.</summary>
        /// <param name="request">_</param> <returns>True.</returns>
        public bool CanApply(Request request)
        {
            return true;
        }

        private readonly Lazy<Expression> _singletonReuseExpr = new Lazy<Expression>(() =>
            Expression.Field(null, typeof(Reuse).GetFieldOrNull("Singleton")));

        /// <inheritdoc />
        public Expression ToExpression(Func<object, Expression> fallbackConverter)
        {
            return _singletonReuseExpr.Value;
        }

        #region Obsolete

        /// <summary>Returns container bound Singleton scope.</summary>
        /// <param name="request">Request to get context information or for example store something in resolution state.</param>
        /// <returns>Container singleton scope.</returns>
        public IScope GetScopeOrDefault(Request request)
        {
            return request.SingletonScope;
        }

        /// <inheritdoc />
        public Expression GetScopeExpression(Request request)
        {
            return Throw.For<Expression>(Error.Of("Obsolete"));
        }

        /// <summary>Returns index of new item in singleton scope.</summary>
        /// <param name="factoryID">Factory id to map to new item index.</param>
        /// <param name="request">Context to get singleton scope from.</param>
        /// <returns>Index in scope.</returns>
        public int GetScopedItemIdOrSelf(int factoryID, Request request)
        {
            return request.SingletonScope.GetScopedItemIdOrSelf(factoryID);
        }

        #endregion

        /// <summary>Pretty prints reuse name and lifespan</summary> <returns>Printed string.</returns>
        public override string ToString()
        {
            return GetType().Name + " {Lifespan=" + Lifespan + "}";
        }
    }

    /// <summary>Returns container bound current scope created by <see cref="Container.OpenScope"/> method.</summary>
    /// <remarks>It is the same as Singleton scope if container was not created by <see cref="Container.OpenScope"/>.</remarks>
    internal sealed class CurrentScopeReuse : IReuse, IReuseV3
    {
        // todo: v3: move to IReuse interface + plus add ability to be an array of names
        /// <summary>Name to find current scope or parent with equal name.</summary>
        public readonly object Name;

        /// <summary>Relative to other reuses lifespan value.</summary>
        public int Lifespan { get { return 100; } }

        /// <summary>Creates reuse optionally specifying its name.</summary>
        /// <param name="name">(optional) Used to find matching current scope or parent.</param>
        public CurrentScopeReuse(object name = null)
        {
            Name = name;
        }

        internal bool ScopedOrSingleton;
        internal CurrentScopeReuse(bool scopedOrSingleton)
        {
            ScopedOrSingleton = scopedOrSingleton;
        }

        internal static object GetScopedOrSingleton(IScopeAccess scopes, IScope singleton,
            int itemId, CreateScopedValue createValue)
        {
            var scope = scopes.GetCurrentNamedScope(null, throwIfNotFound: false);
            if (scope != null)
                return scope.GetOrAdd(itemId, createValue);
            var singetonId = itemId == -1 ? -1 : singleton.GetScopedItemIdOrSelf(itemId);
            return singleton.GetOrAdd(singetonId, createValue);
        }

        private static readonly MethodInfo _getScopedOrSingletonMethod =
            typeof(CurrentScopeReuse).GetSingleMethodOrNull("GetScopedOrSingleton", includeNonPublic: true);

        /// <summary>Returns item from current scope with specified name.</summary>
        /// <param name="scopes">Container scopes to select from.</param>
        /// <param name="scopeName">scope name to look up.</param>
        /// <param name="throwIfNoScopeFound">Specifies to throw if scope with the <paramref name="scopeName"/> is not found.</param>
        /// <param name="itemId">Scoped item ID for lookup.</param>
        /// <param name="createValue">Delegate for creating the item.</param>
        /// <returns>Reused item.</returns>
        public static object GetOrAddItemOrDefault(IScopeAccess scopes, object scopeName,
            bool throwIfNoScopeFound, int itemId, CreateScopedValue createValue)
        {
            var scope = scopes.GetCurrentNamedScope(scopeName, throwIfNoScopeFound);
            return scope == null ? null : scope.GetOrAdd(itemId, createValue);
        }

        private static readonly MethodInfo _getOrAddOrDefaultMethod =
            typeof(CurrentScopeReuse).GetSingleMethodOrNull("GetOrAddItemOrDefault");

        /// <summary>Returns expression call to <see cref="GetOrAddItemOrDefault"/>.</summary>
        public Expression Apply(Request request, bool trackTransientDisposable, Expression createItemExpr)
        {
            var scopesExpr = Container.GetScopesExpr(request);
            var itemId = trackTransientDisposable ? -1 : request.FactoryID;

            if (ScopedOrSingleton)
                return Expression.Call(_getScopedOrSingletonMethod,
                    scopesExpr, Container.SingletonScopeExpr,
                    Expression.Constant(itemId),
                    Expression.Lambda<CreateScopedValue>(createItemExpr));

            // todo: add the ValueType check to GetOrAddStateItemExpression
            var scopeNameExpr = request.Container.GetOrAddStateItemExpression(Name);
            if (Name != null && Name.GetType().IsValueType())
                scopeNameExpr = Expression.Convert(scopeNameExpr, typeof(object));

            return Expression.Call(_getOrAddOrDefaultMethod,
                scopesExpr, scopeNameExpr,
                Expression.Constant(request.IfUnresolved == IfUnresolved.Throw),
                Expression.Constant(itemId),
                Expression.Lambda<CreateScopedValue>(createItemExpr));
        }

        /// <summary>Returns true if scope is open and the name is matching with reuse <see cref="Name"/>.</summary>
        /// <param name="request">Service request.</param> <returns>Check result.</returns>
        public bool CanApply(Request request)
        {
            if (ScopedOrSingleton)
                return true;

            // A special case with ambient scope context, 
            // where scope can be switched for already resolved singleton. 
            // So it may be no valid initially but only afterwards
            return (request.Container.ScopeContext != null && request.IsWrappedInFunc()) ||
                   request.Scopes.GetCurrentNamedScope(Name, false) != null;
        }

        private readonly Lazy<Expression> _inCurrentScopeReuseExpr = new Lazy<Expression>(() =>
            Expression.Field(null, typeof(Reuse).GetFieldOrNull("InCurrentScope")));

        private readonly Lazy<Expression> _scopedOrSingletonExpr = new Lazy<Expression>(() =>
            Expression.Field(null, typeof(Reuse).GetFieldOrNull("ScopedOrSingleton")));

        /// <inheritdoc />
        public Expression ToExpression(Func<object, Expression> fallbackConverter)
        {
            return Name == null && !ScopedOrSingleton ? _inCurrentScopeReuseExpr.Value
                : ScopedOrSingleton ? _scopedOrSingletonExpr.Value
                    : Expression.Call(typeof(Reuse), "InCurrentNamedScope", ArrayTools.Empty<Type>(), fallbackConverter(Name));
        }

        #region Obsolete

        /// <summary>Returns container current scope or if <see cref="Name"/> specified: current scope or its parent with corresponding name.</summary>
        /// <param name="request">Request to get context information or for example store something in resolution state.</param>
        /// <returns>Found current scope or its parent.</returns>
        /// <exception cref="ContainerException">with the code <see cref="Error.NoMatchedScopeFound"/> if <see cref="Name"/> specified but
        /// no matching scope or its parent found.</exception>
        public IScope GetScopeOrDefault(Request request)
        {
            return request.Scopes.GetCurrentNamedScope(Name, false);
        }

        /// <inheritdoc />
        public Expression GetScopeExpression(Request request)
        {
            return Throw.For<Expression>(Error.Of("Obsolete"));
        }

        /// <summary>Asks the scope to convert factory ID into internal representation and returns it.
        /// If scope is not available then return passed factory ID.</summary>
        /// <param name="factoryID">Input factory ID.</param> <param name="request">Used to get scope back.</param>
        /// <returns>Scope mapping of factory ID or passed factory ID without changes if scope is not available.</returns>
        public int GetScopedItemIdOrSelf(int factoryID, Request request)
        {
            return Throw.For<int>(Error.Of("Obsolete"));
        }

        #endregion

        /// <summary>Pretty prints reuse to string.</summary> <returns>Reuse string.</returns>
        public override string ToString()
        {
            var s = new StringBuilder(GetType().Name + " {");
            if (Name != null)
                s.Append("Name=").Print(Name, "\"").Append(", ");
            return s.Append("Lifespan=").Append(Lifespan).Append("}").ToString();
        }
    }

    /// <summary>Represents services created once per resolution root (when some of Resolve methods called).</summary>
    /// <remarks>Scope is created only if accessed to not waste memory.</remarks>
    internal sealed class ResolutionScopeReuse : IReuse, IReuseV3
    {
        /// <summary>Relative to other reuses lifespan value.</summary>
        public int Lifespan { get { return 0; } }

        /// <summary>Indicates consumer with assignable service type that defines resolution scope.</summary>
        public readonly Type AssignableFromServiceType;

        /// <summary>Indicates service key of the consumer that defines resolution scope.</summary>
        public readonly object ServiceKey;

        /// <summary>When set indicates to find the outermost matching consumer with resolution scope,
        /// otherwise nearest consumer scope will be used.</summary>
        public readonly bool Outermost;

        /// <summary>Creates new resolution scope reuse with specified type and key.</summary>
        /// <param name="assignableFromServiceType">(optional)</param> <param name="serviceKey">(optional)</param>
        /// <param name="outermost">(optional)</param>
        public ResolutionScopeReuse(Type assignableFromServiceType = null, object serviceKey = null, bool outermost = false)
        {
            AssignableFromServiceType = assignableFromServiceType;
            ServiceKey = serviceKey;
            Outermost = outermost;
        }

        /// <inheritdoc />
        public Expression Apply(Request request, bool trackTransientDisposable, Expression createItemExpr)
        {
            var scopeExpr = GetScopeExpression(request);

            // For transient disposable we don't care to bind to specific ID, because it should be created each time.
            var scopedId = trackTransientDisposable ? -1 : request.FactoryID;
            return Expression.Call(scopeExpr, "GetOrAdd", ArrayTools.Empty<Type>(),
                Expression.Constant(scopedId),
                Expression.Lambda<CreateScopedValue>(createItemExpr, ArrayTools.Empty<ParameterExpression>()));
        }

        /// <inheritdoc />
        public bool CanApply(Request request)
        {
            return GetScopeOrDefault(request) != null;
        }

        private readonly Lazy<Expression> _inResolutionScopeReuseExpr = new Lazy<Expression>(() =>
            Expression.Field(null, typeof(Reuse).GetFieldOrNull("InCurrentScope")));

        /// <inheritdoc />
        public Expression ToExpression(Func<object, Expression> fallbackConverter)
        {
            if (AssignableFromServiceType == null && ServiceKey == null && Outermost == false)
                return _inResolutionScopeReuseExpr.Value;

            return Expression.Call(typeof(Reuse), "InResolutionScopeOf", ArrayTools.Empty<Type>(),
                Expression.Constant(AssignableFromServiceType, typeof(Type)),
                fallbackConverter(ServiceKey),
                Expression.Constant(Outermost, typeof(bool)));
        }

        #region Obsolete

        /// <summary>Creates or returns already created resolution root scope.</summary>
        /// <param name="request">Request to get context information or for example store something in resolution state.</param>
        /// <returns>Created or existing scope.</returns>
        public IScope GetScopeOrDefault(Request request)
        {
            var scope = request.Scope;
            if (scope == null)
            {
                var parent = request.Enumerate().Last();
                request.Scopes.GetOrCreateResolutionScope(ref scope, parent.GetActualServiceType(), parent.ServiceKey);
            }

            return request.Scopes.GetMatchingResolutionScope(scope,
                AssignableFromServiceType, ServiceKey, Outermost, throwIfNotFound: false);
        }

        /// <summary>Returns <see cref="IScopeAccess.GetMatchingResolutionScope"/> method call expression.</summary>
        /// <param name="request">Request to get context information or for example store something in resolution state.</param>
        /// <returns>Method call expression returning existing or newly created resolution scope.</returns>
        public Expression GetScopeExpression(Request request)
        {
            var scopesExpr = Container.GetScopesExpr(request);
            return Expression.Call(scopesExpr, "GetMatchingResolutionScope", ArrayTools.Empty<Type>(),
                Container.GetResolutionScopeExpression(request),
                Expression.Constant(AssignableFromServiceType, typeof(Type)),
                request.Container.GetOrAddStateItemExpression(ServiceKey, typeof(object)),
                Expression.Constant(Outermost, typeof(bool)),
                Expression.Constant(request.IfUnresolved == IfUnresolved.Throw, typeof(bool)));
        }

        /// <summary>Just returns back passed id without changes.</summary>
        /// <param name="factoryID">Id to return back.</param> <param name="request">Ignored.</param>
        /// <returns><paramref name="factoryID"/></returns>
        public int GetScopedItemIdOrSelf(int factoryID, Request request)
        {
            return factoryID;
        }

        #endregion

        /// <summary>Pretty prints reuse name and lifespan</summary> <returns>Printed string.</returns>
        public override string ToString()
        {
            var s = new StringBuilder().Append(GetType().Name)
                .Append(" {Name={").Print(AssignableFromServiceType)
                .Append(", ").Print(ServiceKey, "\"")
                .Append("}}");
            return s.ToString();
        }
    }

    /// <summary>Specifies pre-defined reuse behaviors supported by container:
    /// used when registering services into container with <see cref="Registrator"/> methods.</summary>
    internal static class Reuse
    {
        /// <summary>Synonym for absence of reuse.</summary>
        public static readonly IReuse Transient = new TransientReuse();

        /// <summary>Specifies to store single service instance per <see cref="Container"/>.</summary>
        public static readonly IReuse Singleton = new SingletonReuse();

        /// <summary>Specifies to store single service instance per resolution root created by <see cref="Resolver"/> methods.</summary>
        public static readonly IReuse InResolutionScope = new ResolutionScopeReuse();

        /// <summary>Specifies to store single service instance per current/open scope created with <see cref="Container.OpenScope"/>.</summary>
        public static readonly IReuse InCurrentScope = new CurrentScopeReuse();

        /// <summary>The same as <see cref="InCurrentScope"/> but if no open scope available will fallback to <see cref="Reuse.Singleton"/></summary>
        /// <remarks>The <see cref="Error.DependencyHasShorterReuseLifespan"/> is applied the same way as for <see cref="InCurrentScope"/> reuse.</remarks>
        public static readonly IReuse ScopedOrSingleton = new CurrentScopeReuse(scopedOrSingleton: true);

        /// <summary>Returns current scope reuse with specific name to match with scope.
        /// If name is not specified then function returns <see cref="InCurrentScope"/>.</summary>
        /// <param name="name">(optional) Name to match with scope.</param>
        /// <returns>Created current scope reuse.</returns>
        public static IReuse InCurrentNamedScope(object name = null)
        {
            return name == null ? InCurrentScope : new CurrentScopeReuse(name);
        }

        /// <summary>Creates reuse to search for <paramref name="assignableFromServiceType"/> and <paramref name="serviceKey"/>
        /// in existing resolution scope hierarchy. If parameters are not specified or null, then <see cref="InResolutionScope"/> will be returned.</summary>
        /// <param name="assignableFromServiceType">(optional) To search for scope with service type assignable to type specified in parameter.</param>
        /// <param name="serviceKey">(optional) Search for specified key.</param>
        /// <param name="outermost">If true - commands to look for outermost match instead of nearest.</param>
        /// <returns>New reuse with specified parameters or <see cref="InResolutionScope"/> if nothing specified.</returns>
        public static IReuse InResolutionScopeOf(Type assignableFromServiceType = null, object serviceKey = null, bool outermost = false)
        {
            return assignableFromServiceType == null && serviceKey == null ? InResolutionScope
                : new ResolutionScopeReuse(assignableFromServiceType, serviceKey, outermost);
        }

        /// <summary>Creates reuse to search for <typeparamref name="TAssignableFromServiceType"/> and <paramref name="serviceKey"/>
        /// in existing resolution scope hierarchy.</summary>
        /// <typeparam name="TAssignableFromServiceType">To search for scope with service type assignable to type specified in parameter.</typeparam>
        /// <param name="serviceKey">(optional) Search for specified key.</param>
        /// <param name="outermost">If true - commands to look for outermost match instead of nearest.</param>
        /// <returns>New reuse with specified parameters.</returns>
        public static IReuse InResolutionScopeOf<TAssignableFromServiceType>(object serviceKey = null, bool outermost = false)
        {
            return InResolutionScopeOf(typeof(TAssignableFromServiceType), serviceKey, outermost);
        }

        /// <summary>Ensuring single service instance per Thread.</summary>
        public static readonly IReuse InThread = InCurrentNamedScope(ThreadScopeContext.ScopeContextName);

        /// <summary>Special name that by convention recognized by <see cref="InWebRequest"/>.</summary>
        public static readonly string WebRequestScopeName = "WebRequestScopeName";

        /// <summary>Web request is just convention for reuse in <see cref="InCurrentNamedScope"/> with special name <see cref="WebRequestScopeName"/>.</summary>
        public static readonly IReuse InWebRequest = InCurrentNamedScope(WebRequestScopeName);

        #region Implementation

        /// <summary>No-reuse</summary>
        private sealed class TransientReuse : IReuse, IReuseV3
        {
            /// <summary>0 means no reused lifespan</summary>
            public int Lifespan { get { return 0; } }

            /// <summary>returns source expression without modification</summary>
            public Expression Apply(Request request, bool trackTransientDisposable, Expression createItemExpr)
            {
                return createItemExpr;
            }

            public bool CanApply(Request request)
            {
                return true;
            }

            private readonly Lazy<Expression> _transientReuseExpr = new Lazy<Expression>(() =>
                Expression.Field(null, typeof(Reuse).GetFieldOrNull("Transient")));

            public Expression ToExpression(Func<object, Expression> fallbackConverter)
            {
                return _transientReuseExpr.Value;
            }

            #region Obsolete

            public IScope GetScopeOrDefault(Request request)
            {
                throw new NotImplementedException();
            }

            public Expression GetScopeExpression(Request request)
            {
                throw new NotImplementedException();
            }

            public int GetScopedItemIdOrSelf(int factoryID, Request request)
            {
                throw new NotImplementedException();
            }

            #endregion
        }

        #endregion
    }

    /// <summary>Policy to handle unresolved service.</summary>
    internal enum IfUnresolved
    {
        /// <summary>Specifies to throw exception if no service found.</summary>
        Throw,
        /// <summary>Specifies to return default value instead of throwing error.</summary>
        ReturnDefault
    }

    /// <summary>Dependency request path information.</summary>
    internal sealed class RequestInfo
    {
        /// <summary>Represents empty info.</summary>
        public static readonly RequestInfo Empty = new RequestInfo();

        /// <summary>Wraps the resolved service lookup details.</summary>
        public readonly IServiceInfo ServiceInfo;

        /// <summary>Returns true for an empty request.</summary>
        public bool IsEmpty { get { return ServiceInfo == null; } }

        /// <summary>Returns true if request is the first in a chain.</summary>
        public bool IsResolutionRoot { get { return !IsEmpty && ParentOrWrapper.IsEmpty; } }

        /// <summary>Parent request or null for root resolution request.</summary>
        public readonly RequestInfo ParentOrWrapper;

        /// <summary>Returns service parent skipping wrapper if any. To get immediate parent us <see cref="ParentOrWrapper"/>.</summary>
        public RequestInfo Parent
        {
            get
            {
                if (IsEmpty)
                    return Empty;

                var p = ParentOrWrapper;
                while (!p.IsEmpty && p.FactoryType == FactoryType.Wrapper)
                    p = p.ParentOrWrapper;
                return p;
            }
        }

        /// <summary>Requested service type.</summary>
        public Type ServiceType { get { return ServiceInfo == null ? null : ServiceInfo.ServiceType; } }

        /// <summary>Required service type if specified.</summary>
        public Type RequiredServiceType
        {
            get { return ServiceInfo == null ? null : ServiceInfo.Details.RequiredServiceType; }
        }

        /// <summary>Returns <see cref="RequiredServiceType"/> if it is specified and assignable to <see cref="ServiceType"/>,
        /// otherwise returns <see cref="ServiceType"/>.</summary>
        /// <returns>The type to be used for lookup in registry.</returns>
        public Type GetActualServiceType()
        {
            return ServiceInfo.GetActualServiceType();
        }

        /// <summary>Returns known implementation, or otherwise actual service type.</summary> 
        /// <returns>The subject.</returns>
        public Type GetKnownImplementationOrServiceType()
        {
            return ImplementationType ?? GetActualServiceType();
        }

        /// <summary>Policy to deal with unresolved request.</summary>
        public IfUnresolved IfUnresolved
        {
            get { return ServiceInfo == null ? IfUnresolved.Throw : ServiceInfo.Details.IfUnresolved; }
        }

        /// <summary>Optional service key to identify service of the same type.</summary>
        public object ServiceKey
        {
            get { return ServiceInfo == null ? null : ServiceInfo.Details.ServiceKey; }
        }

        /// <summary>Metadata key to find in metadata dictionary in resolved service.</summary>
        public string MetadataKey
        {
            get { return ServiceInfo == null ? null : ServiceInfo.Details.MetadataKey; }
        }

        /// <summary>Metadata or the value (if key specified) to find in resolved service.</summary>
        public object Metadata
        {
            get { return ServiceInfo == null ? null : ServiceInfo.Details.Metadata; }
        }

        /// <summary>Resolved factory ID, used to identify applied decorator.</summary>
        public readonly int FactoryID;

        /// <summary>Type of factory: Service, Wrapper, or Decorator.</summary>
        public readonly FactoryType FactoryType;

        /// <summary>Service implementation type if known.</summary>
        public readonly Type ImplementationType;

        /// <summary>Service reuse.</summary>
        public readonly IReuse Reuse;

        /// <summary>Relative number representing reuse lifespan.</summary>
        public int ReuseLifespan { get { return Reuse == null ? 0 : Reuse.Lifespan; } }

        /// <summary><see cref="RequestFlags"/>.</summary>
        public readonly RequestFlags Flags;

        /// <summary>Simplified version of Push with most common properties.</summary>
        /// <param name="serviceType"></param> <param name="factoryID"></param> <param name="implementationType"></param>
        /// <param name="reuse"></param> <returns>Created info chain to current (parent) info.</returns>
        public RequestInfo Push(Type serviceType, int factoryID, Type implementationType, IReuse reuse)
        {
            return Push(serviceType, null, null, null, null, IfUnresolved.Throw,
                factoryID, FactoryType.Service, implementationType, reuse, default(RequestFlags));
        }

        /// <summary>Creates info by supplying all the properties and chaining it with current (parent) info.</summary>
        /// <param name="serviceType"></param> <param name="requiredServiceType"></param>
        /// <param name="serviceKey"></param> <param name="factoryType"></param> <param name="factoryID"></param>
        /// <param name="implementationType"></param> <param name="reuse"></param><param name="flags"></param>
        /// <returns>Created info chain to current (parent) info.</returns>
        public RequestInfo Push(Type serviceType, Type requiredServiceType, object serviceKey,
            int factoryID, FactoryType factoryType, Type implementationType, IReuse reuse, RequestFlags flags)
        {
            return Push(serviceType, requiredServiceType, serviceKey, null, null, IfUnresolved.Throw,
                factoryID, factoryType, implementationType, reuse, flags);
        }

        /// <summary>Creates info by supplying all the properties and chaining it with current (parent) info.</summary>
        /// <param name="serviceType"></param> <param name="requiredServiceType"></param>
        /// <param name="serviceKey"></param> <param name="ifUnresolved"></param>
        /// <param name="factoryType"></param> <param name="factoryID"></param>
        /// <param name="implementationType"></param> <param name="reuse"></param>
        /// <param name="flags"></param>
        /// <returns>Created info chain to current (parent) info.</returns>
        public RequestInfo Push(Type serviceType, Type requiredServiceType, object serviceKey, IfUnresolved ifUnresolved,
            int factoryID, FactoryType factoryType, Type implementationType, IReuse reuse, RequestFlags flags)
        {
            return Push(serviceType, requiredServiceType, serviceKey, null, null, ifUnresolved,
                factoryID, factoryType, implementationType, reuse, flags);
        }

        /// <summary>Creates info by supplying all the properties and chaining it with current (parent) info.</summary>
        /// <param name="serviceType"></param> <param name="requiredServiceType"></param>
        /// <param name="serviceKey"></param> <param name="metadataKey"></param><param name="metadata"></param>
        /// <param name="ifUnresolved"></param>
        /// <param name="factoryType"></param> <param name="factoryID"></param>
        /// <param name="implementationType"></param> <param name="reuse"></param>
        /// <param name="flags"></param>
        /// <returns>Created info chain to current (parent) info.</returns>
        public RequestInfo Push(Type serviceType, Type requiredServiceType, object serviceKey, string metadataKey, object metadata, IfUnresolved ifUnresolved,
            int factoryID, FactoryType factoryType, Type implementationType, IReuse reuse, RequestFlags flags)
        {
            var info = DryIoc.ServiceInfo.Of(serviceType, requiredServiceType, ifUnresolved, serviceKey, metadataKey, metadata);
            return Push(info, factoryID, factoryType, implementationType, reuse, flags);
        }

        /// <summary>Creates info by supplying all the properties and chaining it with current (parent) info.</summary>
        /// <param name="serviceInfo"></param>
        /// <param name="factoryType">(optional)</param> <param name="factoryID">(optional)</param>
        /// <param name="implementationType">(optional)</param> <param name="reuse">(optional)</param>
        /// <param name="flags">(optional)</param>
        /// <returns>Created info chain to current (parent) info.</returns>
        public RequestInfo Push(IServiceInfo serviceInfo,
            int factoryID = 0, FactoryType factoryType = FactoryType.Service, Type implementationType = null, IReuse reuse = null,
            RequestFlags flags = default(RequestFlags))
        {
            return new RequestInfo(serviceInfo, factoryID, factoryType, implementationType, reuse, flags, this);
        }

        /// <summary>Returns all request until the root - parent is null.</summary>
        /// <returns>Requests from the last to first.</returns>
        public IEnumerable<RequestInfo> Enumerate()
        {
            for (var i = this; !i.IsEmpty; i = i.ParentOrWrapper)
                yield return i;
        }

        /// <summary>Prints request without parents.</summary>
        /// <param name="s">Where to print.</param><returns><paramref name="s"/> with appended info.</returns>
        public StringBuilder PrintCurrent(StringBuilder s)
        {
            if (IsEmpty)
                return s.Append("{empty}");

            if (Reuse != null && Reuse != DryIoc.Reuse.Transient)
                s.Append(Reuse is SingletonReuse ? "singleton" : "scoped").Append(' ');

            if (FactoryType != FactoryType.Service)
                s.Append(FactoryType.ToString().ToLower()).Append(' ');

            if (ImplementationType != null && ImplementationType != ServiceType)
                s.Print(ImplementationType).Append(": ");

            s.Append(ServiceInfo);
            return s;
        }

        /// <summary>Prints request with all its parents.</summary>
        /// <param name="s">Where to print.</param><returns><paramref name="s"/> with appended info.</returns>
        public StringBuilder Print(StringBuilder s)
        {
            s = PrintCurrent(s);
            if (!ParentOrWrapper.IsEmpty)
                ParentOrWrapper.Print(s.AppendLine().Append("  in ")); // recursion
            return s;
        }

        /// <summary>Prints request with all its parents to string.</summary> <returns>The string.</returns>
        public override string ToString()
        {
            return Print(new StringBuilder()).ToString();
        }

        /// <summary>Returns true if request info and passed object are equal, and their parents recursively are equal.</summary>
        /// <param name="obj"></param> <returns></returns>
        public override bool Equals(object obj)
        {
            return Equals(obj as RequestInfo);
        }

        /// <summary>Returns true if request info and passed info are equal, and their parents recursively are equal.</summary>
        /// <param name="other"></param> <returns></returns>
        public bool Equals(RequestInfo other)
        {
            return other != null && EqualsWithoutParent(other)
                   && (ParentOrWrapper == null && other.ParentOrWrapper == null
                       || (ParentOrWrapper != null && ParentOrWrapper.EqualsWithoutParent(other.ParentOrWrapper)));
        }

        /// <summary>Compares info's regarding properties but not their parents.</summary>
        /// <param name="other">Info to compare for equality.</param> <returns></returns>
        public bool EqualsWithoutParent(RequestInfo other)
        {
            return other.ServiceType == ServiceType
                   && other.RequiredServiceType == RequiredServiceType
                   && other.IfUnresolved == IfUnresolved
                   && Equals(other.ServiceKey, ServiceKey)
                   && other.MetadataKey == MetadataKey
                   && Equals(other.Metadata, Metadata)

                   && other.FactoryType == FactoryType
                   && other.ImplementationType == ImplementationType
                   && other.ReuseLifespan == ReuseLifespan;
        }

        /// <summary>Compares info's regarding properties but not their parents.</summary>
        /// <param name="other">Info to compare for equality.</param> <returns></returns>
        public bool EqualsWithoutParent(Request other)
        {
            return other.ServiceType == ServiceType
                   && other.RequiredServiceType == RequiredServiceType
                   && other.IfUnresolved == IfUnresolved
                   && Equals(other.ServiceKey, ServiceKey)
                   && other.MetadataKey == MetadataKey
                   && Equals(other.Metadata, Metadata)

                   && other.FactoryType == FactoryType
                   && other.ImplementationType == ImplementationType
                   && other.ReuseLifespan == ReuseLifespan;
        }

        /// <summary>Returns hash code combined from info fields plus its parent.</summary>
        /// <returns>Combined hash code.</returns>
        public override int GetHashCode()
        {
            var hash = 0;
            for (var i = this; !i.IsEmpty; i = i.ParentOrWrapper)
            {
                var currentHash = i.ServiceType.GetHashCode();
                if (i.RequiredServiceType != null)
                    currentHash = CombineHashCodes(currentHash, i.RequiredServiceType.GetHashCode());

                if (i.ServiceKey != null)
                    currentHash = CombineHashCodes(currentHash, i.ServiceKey.GetHashCode());

                if (i.IfUnresolved != IfUnresolved.Throw)
                    currentHash = CombineHashCodes(currentHash, i.IfUnresolved.GetHashCode());

                if (i.FactoryType != FactoryType.Service)
                    currentHash = CombineHashCodes(currentHash, i.FactoryType.GetHashCode());

                if (i.ImplementationType != null && i.ImplementationType != i.ServiceType)
                    currentHash = CombineHashCodes(currentHash, i.ImplementationType.GetHashCode());

                if (i.ReuseLifespan != 0)
                    currentHash = CombineHashCodes(currentHash, i.ReuseLifespan);

                hash = hash == 0 ? currentHash : CombineHashCodes(hash, currentHash);
            }
            return hash;
        }

        private RequestInfo()
        {
            FactoryID = -1;
        }

        private RequestInfo(IServiceInfo serviceInfo,
            int factoryID, FactoryType factoryType, Type implementationType, IReuse reuse, RequestFlags flags,
            RequestInfo parentOrWrapper)
        {
            ParentOrWrapper = parentOrWrapper;

            ServiceInfo = serviceInfo;

            // Implementation info:
            FactoryID = factoryID;
            FactoryType = factoryType;
            ImplementationType = implementationType;
            Reuse = reuse;

            Flags = flags;
        }

        // Inspired by System.Tuple.CombineHashCodes
        private static int CombineHashCodes(int h1, int h2)
        {
            unchecked
            {
                return (h1 << 5) + h1 ^ h2;
            }
        }
    }

    /// <summary>Declares minimal API for service resolution.
    /// The user friendly convenient methods are implemented as extension methods in <see cref="Resolver"/> class.</summary>
    /// <remarks>Resolve default and keyed is separated because of micro optimization for faster resolution.</remarks>
    internal interface IResolver
    {
        // todo: v3: replace bool @ifUnresolvedReturnDefault with enum type
        /// <summary>Resolves default (non-keyed) service from container and returns created service object.</summary>
        /// <param name="serviceType">Service type to search and to return.</param>
        /// <param name="ifUnresolvedReturnDefault">Says what to do if service is unresolved.</param>
        /// <returns>Created service object or default based on <paramref name="ifUnresolvedReturnDefault"/> provided.</returns>
        object Resolve(Type serviceType, bool ifUnresolvedReturnDefault);

        // todo: v3: remove @scope parameter
        /// <summary>Resolves service from container and returns created service object.</summary>
        /// <param name="serviceType">Service type to search and to return.</param>
        /// <param name="serviceKey">Optional service key used for registering service.</param>
        /// <param name="ifUnresolvedReturnDefault">Says what to do if service is unresolved.</param>
        /// <param name="requiredServiceType">Actual registered service type to use instead of <paramref name="serviceType"/>,
        ///     or wrapped type for generic wrappers.  The type should be assignable to return <paramref name="serviceType"/>.</param>
        /// <param name="preResolveParent">Dependency resolution path info.</param>
        /// <param name="scope">Propagated resolution scope.</param>
        /// <returns>Created service object or default based on <paramref name="ifUnresolvedReturnDefault"/> provided.</returns>
        /// <remarks>
        /// This method covers all possible resolution input parameters comparing to <see cref="Resolve(System.Type,bool)"/>, and
        /// by specifying the same parameters as for <see cref="Resolve(System.Type,bool)"/> should return the same result.
        /// </remarks>
        object Resolve(Type serviceType, object serviceKey, bool ifUnresolvedReturnDefault, Type requiredServiceType,
            RequestInfo preResolveParent, IScope scope);

        // todo: v3: remove unused @compositeParentKey and @compositeParentRequiredType
        /// <summary>Resolves all services registered for specified <paramref name="serviceType"/>, or if not found returns
        /// empty enumerable. If <paramref name="serviceType"/> specified then returns only (single) service registered with
        /// this type. Excludes for result composite parent identified by <paramref name="compositeParentKey"/>.</summary>
        /// <param name="serviceType">Return type of an service item.</param>
        /// <param name="serviceKey">(optional) Resolve only single service registered with the key.</param>
        /// <param name="requiredServiceType">(optional) Actual registered service to search for.</param>
        /// <param name="compositeParentKey">OBSOLETE: Now I can use <paramref name="preResolveParent"/> to identify composite parent.</param>
        /// <param name="compositeParentRequiredType">OBSOLETE: Now I can use <paramref name="preResolveParent"/> to identify composite parent.</param>
        /// <param name="preResolveParent">Dependency resolution path info.</param>
        /// <param name="scope">propagated resolution scope, may be null.</param>
        /// <returns>Enumerable of found services or empty. Does Not throw if no service found.</returns>
        IEnumerable<object> ResolveMany(Type serviceType, object serviceKey, Type requiredServiceType,
            object compositeParentKey, Type compositeParentRequiredType,
            RequestInfo preResolveParent, IScope scope);
    }

    /// <summary>Specifies options to handle situation when registered service is already present in the registry.</summary>
    internal enum IfAlreadyRegistered
    {
        /// <summary>Appends new default registration or throws registration with the same key.</summary>
        AppendNotKeyed,
        /// <summary>Throws if default or registration with the same key is already exist.</summary>
        Throw,
        /// <summary>Keeps old default or keyed registration ignoring new registration: ensures Register-Once semantics.</summary>
        Keep,
        /// <summary>Replaces old registration with new one.</summary>
        Replace,
        /// <summary>Adds the new implementation or null (Made.Of), 
        /// otherwise keeps the previous registration of the same implementation type.</summary>
        AppendNewImplementation
    }

    /// <summary>Define registered service structure.</summary>
    internal struct ServiceRegistrationInfo
    {
        /// <summary>Required service type.</summary>
        public Type ServiceType;

        /// <summary>Is null single default service, or actual service key, or <see cref="DefaultKey"/> for multiple default services.</summary>
        public object OptionalServiceKey;

        /// <summary>Registered factory.</summary>
        public Factory Factory;

        /// <summary>Provides registration order across all factory registrations in container.</summary>
        /// <remarks>May be repeated for factory registered with multiple services.</remarks>
        public int FactoryRegistrationOrder;

        /// <summary>Creates info. Registration order is figured out automatically based on Factory.</summary>
        /// <param name="factory"></param> <param name="serviceType"></param> <param name="optionalServiceKey"></param>
        public ServiceRegistrationInfo(Factory factory, Type serviceType, object optionalServiceKey)
        {
            ServiceType = serviceType;
            OptionalServiceKey = optionalServiceKey;
            Factory = factory;
            FactoryRegistrationOrder = factory.FactoryID;
        }

        /// <summary>Pretty-prints info to string.</summary> <returns>The string.</returns>
        public override string ToString()
        {
            var s = new StringBuilder();

            s.Print(ServiceType);

            if (OptionalServiceKey != null)
                s.Append(" with ServiceKey=").Print(OptionalServiceKey, "\"");

            s.Append(" registered as factory ").Append(Factory);

            return s.ToString();
        }
    }

    /// <summary>Defines operations that for changing registry, and checking if something exist in registry.</summary>
    internal interface IRegistrator
    {
        /// <summary>Returns all registered service factories with their Type and optional Key.</summary>
        /// <returns>Existing registrations.</returns>
        /// <remarks>Decorator and Wrapper types are not included.</remarks>
        IEnumerable<ServiceRegistrationInfo> GetServiceRegistrations();

        /// <summary>Registers factory in registry with specified service type and key for lookup.
        /// Returns true if factory was added to registry, false otherwise. False may be in case of <see cref="IfAlreadyRegistered.Keep"/>
        /// setting and already existing factory</summary>
        /// <param name="factory">To register.</param>
        /// <param name="serviceType">Service type as unique key in registry for lookup.</param>
        /// <param name="serviceKey">Service key as complementary lookup for the same service type.</param>
        /// <param name="ifAlreadyRegistered">Policy how to deal with already registered factory with same service type and key.</param>
        /// <param name="isStaticallyChecked">Confirms that service and implementation types are statically checked by compiler.</param>
        /// <returns>True if factory was added to registry, false otherwise.
        /// False may be in case of <see cref="IfAlreadyRegistered.Keep"/> setting and already existing factory.</returns>
        void Register(Factory factory, Type serviceType, object serviceKey, IfAlreadyRegistered ifAlreadyRegistered, bool isStaticallyChecked);

        /// <summary>Returns true if expected factory is registered with specified service key and type.</summary>
        /// <param name="serviceType">Type to lookup.</param>
        /// <param name="serviceKey">(optional) Identifies registration via service key.
        /// Not provided or <c>null</c> service key means to check the <paramref name="serviceType"/> alone with any service key.</param>
        /// <param name="factoryType">Expected factory type.</param>
        /// <param name="condition">Expected factory condition.</param>
        /// <returns>True if expected factory found in registry.</returns>
        bool IsRegistered(Type serviceType, object serviceKey, FactoryType factoryType, Func<Factory, bool> condition);

        /// <summary>Removes factory with specified service type and key from registry.</summary>
        /// <param name="serviceType">Type to lookup.</param>
        /// <param name="serviceKey">Key to lookup for the same type.</param>
        /// <param name="factoryType">Expected factory type.</param>
        /// <param name="condition">Expected factory condition.</param>
        void Unregister(Type serviceType, object serviceKey, FactoryType factoryType, Func<Factory, bool> condition);
    }

    // todo: v3: Replace with the single Scope and move the rest to extension methods.
    /// <summary>Provides access to scopes.</summary>
    internal interface IScopeAccess
    {
        /// <summary>Current scope.</summary>
        IScope GetCurrentScope();

        /// <summary>Gets current scope matching the <paramref name="name"/>.
        /// If name is null then current scope is returned, or if there is no current scope then exception thrown.</summary>
        /// <param name="name">May be null</param> <returns>Found scope or throws exception.</returns>
        /// <param name="throwIfNotFound">Says to throw if no scope found.</param>
        IScope GetCurrentNamedScope(object name, bool throwIfNotFound);

        /// <summary>Check if scope is not null, then just returns it, otherwise will create and return it.</summary>
        /// <param name="scope">May be null scope.</param>
        /// <param name="serviceType">Marking scope with resolved service type.</param>
        /// <param name="serviceKey">Marking scope with resolved service key.</param>
        /// <returns>Input <paramref name="scope"/> ensuring it is not null.</returns>
        IScope GetOrCreateResolutionScope(ref IScope scope, Type serviceType, object serviceKey);

        /// <summary>Check if scope is not null, then just returns it, otherwise will create and return it.</summary>
        /// <param name="scope">May be null scope.</param>
        /// <param name="serviceType">Marking scope with resolved service type.</param>
        /// <param name="serviceKey">Marking scope with resolved service key.</param>
        /// <returns>Input <paramref name="scope"/> ensuring it is not null.</returns>
        IScope GetOrNewResolutionScope(IScope scope, Type serviceType, object serviceKey);

        /// <summary>If both <paramref name="assignableFromServiceType"/> and <paramref name="serviceKey"/> are null,
        /// then returns input <paramref name="scope"/>.
        /// Otherwise searches scope hierarchy to find first scope with: Type assignable <paramref name="assignableFromServiceType"/> and
        /// Key equal to <paramref name="serviceKey"/>.</summary>
        /// <param name="scope">Scope to start matching with Type and Key specified.</param>
        /// <param name="assignableFromServiceType">Type to match.</param> <param name="serviceKey">Key to match.</param>
        /// <param name="outermost">If true - commands to look for outermost match instead of nearest.</param>
        /// <param name="throwIfNotFound">Says to throw if no scope found.</param>
        IScope GetMatchingResolutionScope(IScope scope, Type assignableFromServiceType, object serviceKey, bool outermost, bool throwIfNotFound);
    }

    /// <summary>Exposes operations required for internal registry access.
    /// That's why most of them are implemented explicitly by <see cref="Container"/>.</summary>
    internal interface IContainer : IRegistrator, IResolver, IDisposable
    {
        // todo: v3: change type from impl. specific to IResolverContext
        /// <summary>Self weak reference, with readable message when container is GCed/Disposed.</summary>
        ContainerWeakRef ContainerWeakRef { get; }

        /// <summary>Rules for defining resolution/registration behavior throughout container.</summary>
        Rules Rules { get; }

        /// <summary>Empty request bound to container. All other requests are created by pushing to empty request.</summary>
        Request EmptyRequest { get; }

        /// <summary>State item objects which may include: singleton instances for fast access, reuses, reuse wrappers, factory delegates, etc.</summary>
        object[] ResolutionStateCache { get; }

        /// <summary>Copies all of container state except Cache and specifies new rules.</summary>
        /// <param name="configure">(optional) Configure rules, if not specified then uses Rules from current container.</param>
        /// <param name="scopeContext">(optional) New scope context, if not specified then uses context from current container.</param>
        /// <returns>New container.</returns>
        IContainer With(Func<Rules, Rules> configure = null, IScopeContext scopeContext = null);

        /// <summary>Produces new container which prevents any further registrations.</summary>
        /// <param name="ignoreInsteadOfThrow">(optional)Controls what to do with registrations: ignore or throw exception.
        /// Throws exception by default.</param>
        /// <returns>New container preserving all current container state but disallowing registrations.</returns>
        IContainer WithNoMoreRegistrationAllowed(bool ignoreInsteadOfThrow = false);

        /// <summary>Returns new container with all expression, delegate, items cache removed/reset.
        /// It will preserve resolved services in Singleton/Current scope.</summary>
        /// <returns>New container with empty cache.</returns>
        IContainer WithoutCache();

        /// <summary>Creates new container with whole state shared with original except singletons.</summary>
        /// <returns>New container with empty Singleton Scope.</returns>
        IContainer WithoutSingletonsAndCache();

        /// <summary>Shares all parts with original container But copies registration, so the new registration
        /// won't be visible in original. Registrations include decorators and wrappers as well.</summary>
        /// <param name="preserveCache">(optional) If set preserves cache if you know what to do.</param>
        /// <returns>New container with copy of all registrations.</returns>
        IContainer WithRegistrationsCopy(bool preserveCache = false);

        /// <summary>Returns scope context associated with container.</summary>
        IScopeContext ScopeContext { get; }

        /// <summary>Creates new container with new opened scope, with shared registrations, singletons and resolutions cache.
        /// If container uses ambient scope context, then this method sets new opened scope as current scope in the context.
        /// In case of previous open scope, new open scope references old one as a parent.
        /// </summary>
        /// <param name="name">(optional) Name for opened scope to allow reuse to identify the scope.</param>
        /// <param name="configure">(optional) Configure rules, if not specified then uses Rules from current container.</param>
        /// <returns>New container with different current scope.</returns>
        /// <example><code lang="cs"><![CDATA[
        /// using (var scoped = container.OpenScope())
        /// {
        ///     var handler = scoped.Resolve<IHandler>();
        ///     handler.Handle(data);
        /// }
        /// ]]></code></example>
        IContainer OpenScope(object name = null, Func<Rules, Rules> configure = null);

        /// <summary>Creates container (facade) that fallbacks to this container for unresolved services.
        /// Facade shares rules with this container, everything else is its own.
        /// It could be used for instance to create Test facade over original container with replacing some services with test ones.</summary>
        /// <remarks>Singletons from container are not reused by facade, to achieve that rather use <see cref="OpenScope"/> with <see cref="Reuse.InCurrentScope"/>.</remarks>
        /// <returns>New facade container.</returns>
        IContainer CreateFacade();

        /// <summary>Searches for requested factory in registry, and then using <see cref="DryIoc.Rules.UnknownServiceResolvers"/>.</summary>
        /// <param name="request">Factory request.</param>
        /// <returns>Found factory, otherwise null if <see cref="Request.IfUnresolved"/> is set to <see cref="IfUnresolved.ReturnDefault"/>.</returns>
        Factory ResolveFactory(Request request);

        /// <summary>Searches for registered service factory and returns it, or null if not found.</summary>
        /// <param name="request">Factory request.</param>
        /// <returns>Found registered factory or null.</returns>
        Factory GetServiceFactoryOrDefault(Request request);

        /// <summary>Finds all registered default and keyed service factories and returns them.
        /// It skips decorators and wrappers.</summary>
        /// <param name="serviceType">Service type to look for, may be open-generic type too.</param>
        /// <param name="bothClosedAndOpenGenerics">(optional) For generic serviceType instructs to look for
        /// both closed and open-generic registrations.</param>
        /// <returns>Enumerable of found pairs.</returns>
        /// <remarks>Returned Key item should not be null - it should be <see cref="DefaultKey.Value"/>.</remarks>
        IEnumerable<KV<object, Factory>> GetAllServiceFactories(Type serviceType, bool bothClosedAndOpenGenerics = false);

        /// <summary>Searches for registered wrapper factory and returns it, or null if not found.</summary>
        /// <param name="serviceType">Service type to look for.</param> <returns>Found wrapper factory or null.</returns>
        Factory GetWrapperFactoryOrDefault(Type serviceType);

        /// <summary>Returns all decorators registered for the service type.</summary> <returns>Decorator factories.</returns>
        Factory[] GetDecoratorFactoriesOrDefault(Type serviceType);

        /// <summary>Creates decorator expression: it could be either Func{TService,TService},
        /// or service expression for replacing decorators.</summary>
        /// <param name="request">Decorated service request.</param>
        /// <returns>Decorator expression.</returns>
        Expression GetDecoratorExpressionOrDefault(Request request);

        /// <summary>For given instance resolves and sets properties and fields.</summary>
        /// <param name="instance">Service instance with properties to resolve and initialize.</param>
        /// <param name="propertiesAndFields">(optional) Function to select properties and fields, overrides all other rules if specified.</param>
        /// <returns>Instance with assigned properties and fields.</returns>
        /// <remarks>Different Rules could be combined together using <see cref="PropertiesAndFields.OverrideWith"/> method.</remarks>
        object InjectPropertiesAndFields(object instance, PropertiesAndFieldsSelector propertiesAndFields);

        /// <summary>If <paramref name="serviceType"/> is generic type then this method checks if the type registered as generic wrapper,
        /// and recursively unwraps and returns its type argument. This type argument is the actual service type we want to find.
        /// Otherwise, method returns the input <paramref name="serviceType"/>.</summary>
        /// <param name="serviceType">Type to unwrap. Method will return early if type is not generic.</param>
        /// <param name="requiredServiceType">Required service type or null if don't care.</param>
        /// <returns>Unwrapped service type in case it corresponds to registered generic wrapper, or input type in all other cases.</returns>
        Type GetWrappedType(Type serviceType, Type requiredServiceType);

        /// <summary>Adds factory expression to cache identified by factory ID (<see cref="Factory.FactoryID"/>).</summary>
        /// <param name="factoryID">Key in cache.</param>
        /// <param name="factoryExpression">Value to cache.</param>
        void CacheFactoryExpression(int factoryID, Expression factoryExpression);

        /// <summary>Searches and returns cached factory expression, or null if not found.</summary>
        /// <param name="factoryID">Factory ID to lookup by.</param> <returns>Found expression or null.</returns>
        Expression GetCachedFactoryExpressionOrDefault(int factoryID);

        /// <summary>Converts known items into custom expression or wraps in <see cref="ConstantExpression"/>.</summary>
        /// <param name="item">Item to convert.</param> 
        /// <param name="itemType">(optional) Type of item, otherwise item <see cref="object.GetType()"/>.</param>
        /// <param name="throwIfStateRequired">(optional) Throws for non-primitive and not-recognized items, 
        /// identifying that result expression require run-time state. For compiled expression it means closure in lambda delegate.</param>
        /// <returns>Returns constant or state access expression for added items.</returns>
        Expression GetOrAddStateItemExpression(object item, Type itemType = null, bool throwIfStateRequired = false);

        // todo: v3: remove with implementation
        /// <summary>Obsolete: Please don't use. Will be removed in V3.</summary>
        int GetOrAddStateItem(object item);
    }

    /// <summary>Resolves all registered services of <typeparamref name="TService"/> type on demand,
    /// when enumerator <see cref="IEnumerator.MoveNext"/> called. If service type is not found, empty returned.</summary>
    /// <typeparam name="TService">Service type to resolve.</typeparam>
    internal sealed class LazyEnumerable<TService> : IEnumerable<TService>
    {
        /// <summary>Exposes internal items enumerable.</summary>
        public readonly IEnumerable<TService> Items;

        /// <summary>Wraps lazy resolved items.</summary> <param name="items">Lazy resolved items.</param>
        public LazyEnumerable(IEnumerable<TService> items)
        {
            Items = items.ThrowIfNull();
        }

        /// <summary>Return items enumerator.</summary> <returns>items enumerator.</returns>
        public IEnumerator<TService> GetEnumerator()
        {
            return Items.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator() { return GetEnumerator(); }
    }

    /// <summary>Wrapper type to box service with associated arbitrary metadata object.</summary>
    /// <typeparam name="T">Service type.</typeparam>
    /// <typeparam name="TMetadata">Arbitrary metadata object type.</typeparam>
    internal sealed class Meta<T, TMetadata>
    {
        /// <summary>Value or object with associated metadata.</summary>
        public readonly T Value;

        /// <summary>Associated metadata object. Could be anything.</summary>
        public readonly TMetadata Metadata;

        /// <summary>Boxes value and its associated metadata together.</summary>
        /// <param name="value">value</param> <param name="metadata">any metadata object</param>
        public Meta(T value, TMetadata metadata)
        {
            Value = value;
            Metadata = metadata;
        }
    }

    /// <summary>Exception that container throws in case of error. Dedicated exception type simplifies
    /// filtering or catching container relevant exceptions from client code.</summary>
    [SuppressMessage("Microsoft.Usage", "CA2237:MarkISerializableTypesWithSerializable",
        Justification = "Not available in PCL.")]
    internal class ContainerException : InvalidOperationException
    {
        /// <summary>Error code of exception, possible values are listed in <see cref="Error"/> class.</summary>
        public readonly int Error;

        /// <summary>Creates exception by wrapping <paramref name="errorCode"/> and its message,
        /// optionally with <paramref name="innerException"/> exception.</summary>
        /// <param name="errorCheck">Type of check</param>
        /// <param name="errorCode">Error code, check <see cref="Error"/> for possible values.</param>
        /// <param name="arg0">(optional) Arguments for formatted message.</param> <param name="arg1"></param> <param name="arg2"></param> <param name="arg3"></param>
        /// <param name="innerException">(optional) Inner exception.</param>
        /// <returns>Created exception.</returns>
        public static ContainerException Of(ErrorCheck errorCheck, int errorCode,
            object arg0, object arg1 = null, object arg2 = null, object arg3 = null,
            Exception innerException = null)
        {
            var messageFormat = GetMessage(errorCheck, errorCode);
            var message = string.Format(messageFormat, Print(arg0), Print(arg1), Print(arg2), Print(arg3));
            return new ContainerException(errorCode, message, innerException);
        }

        /// <summary>Gets error message based on provided args.</summary> <param name="errorCheck"></param> <param name="errorCode"></param>
        /// <returns>message format.</returns>
        protected static string GetMessage(ErrorCheck errorCheck, int errorCode)
        {
            return errorCode == -1 ? Throw.GetDefaultMessage(errorCheck) : DryIoc.Error.Messages[errorCode];
        }

        /// <summary>Prints argument for formatted message.</summary> <param name="arg">To print.</param> <returns>Printed string.</returns>
        protected static string Print(object arg)
        {
            return arg == null ? string.Empty : new StringBuilder().Print(arg).ToString();
        }

        /// <summary>Creates exception with message describing cause and context of error,
        /// and leading/system exception causing it.</summary>
        /// <param name="error">Error code.</param> <param name="message">Error message.</param>
        /// <param name="innerException">Underlying system/leading exception.</param>
        protected ContainerException(int error, string message, Exception innerException)
            : base(message, innerException)
        {
            Error = error;
        }

        /// <summary>Creates exception with message describing cause and context of error.</summary>
        /// <param name="error">Error code.</param> <param name="message">Error message.</param>
        protected ContainerException(int error, string message)
            : this(error, message, null) { }
    }

    /// <summary>Defines error codes and error messages for all DryIoc exceptions (DryIoc extensions may define their own.)</summary>
    internal static class Error
    {
        /// <summary>First error code to identify error range for other possible error code definitions.</summary>
        public static readonly int FirstErrorCode = 0;

        /// <summary>List of error messages indexed with code.</summary>
        public static readonly List<string> Messages = new List<string>(100);

#pragma warning disable 1591 // "Missing XML-comment"
        public static readonly int
            UnableToResolveUnknownService = Of(
                "Unable to resolve {0}" + Environment.NewLine +
                "Where no service registrations found" + Environment.NewLine +
                "  and no dynamic registrations found in {1} Rules.DynamicServiceProviders" + Environment.NewLine +
                "  and nothing in {2} Rules.UnknownServiceResolvers"),

            UnableToResolveFromRegisteredServices = Of(
                "Unable to resolve {0}" + Environment.NewLine +
                "  from {1}" + Environment.NewLine +
                "  with normal and dynamic registrations:" + Environment.NewLine + "{2}"),

            ExpectedSingleDefaultFactory = Of(
                "Expecting single default registration but found many:" + Environment.NewLine + "{0}" + Environment.NewLine +
                "When resolving {1}." + Environment.NewLine +
                "Please identify service with key, or metadata, or use Rules.WithFactorySelector to specify single registered factory."),

            RegisteringImplementationNotAssignableToServiceType = Of(
                "Registering implementation type {0} is not assignable to service type {1}."),
            RegisteredFactoryMethodResultTypesIsNotAssignableToImplementationType = Of(
                "Registered factory method return type {1} should be assignable to implementation type {0} but it is not."),
            RegisteringOpenGenericRequiresFactoryProvider = Of(
                "Unable to register delegate factory for open-generic service {0}." + Environment.NewLine +
                "You need to specify concrete (closed) service type returned by delegate."),
            RegisteringOpenGenericImplWithNonGenericService = Of(
                "Unable to register open-generic implementation {0} with non-generic service {1}."),
            RegisteringOpenGenericServiceWithMissingTypeArgs = Of(
                "Unable to register open-generic implementation {0} because service {1} should specify all type arguments, but specifies only {2}."),
            RegisteringNotAGenericTypedefImplType = Of(
                "Unsupported registration of implementation {0} which is not a generic type definition but contains generic parameters." +
                Environment.NewLine +
                "Consider to register generic type definition {1} instead."),
            RegisteringNotAGenericTypedefServiceType = Of(
                "Unsupported registration of service {0} which is not a generic type definition but contains generic parameters." +
                Environment.NewLine +
                "Consider to register generic type definition {1} instead."),
            RegisteringNullImplementationTypeAndNoFactoryMethod = Of(
                "Registering without implementation type and without FactoryMethod to use instead."),
            RegisteringAbstractImplementationTypeAndNoFactoryMethod = Of(
                "Registering abstract implementation type {0} when it is should be concrete. Also there is not FactoryMethod to use instead."),
            UnableToSelectSinglePublicConstructorFromMultiple = Of(
                "Unable to select single public constructor from implementation type {0}:" + Environment.NewLine + "{1}"),
            UnableToSelectSinglePublicConstructorFromNone = Of(
                "Unable to select single public constructor from implementation type {0} because it does not have one."),
            NoMatchedImplementedTypesWithServiceType = Of(
                "Unable to match service with open-generic {0} implementing {1} when resolving {2}."),
            NoMatchedFactoryMethodDeclaringTypeWithServiceTypeArgs = Of(
                "Unable to match open-generic factory method Declaring type {0} with requested service type arguments <{1}> when resolving {2}."),
            NoMatchedFactoryMethodWithServiceTypeArgs = Of(
                "Unable to match open-generic factory method {0} with requested service type arguments <{1}> when resolving {2}."),
            OpenGenericFactoryMethodDeclaringTypeIsNotSupportedOnThisPlatform = Of(
                "[Specific to this .NET version] Unable to match method or constructor {0} from open-generic declaring type {1} to closed-generic type {2}, " +
                Environment.NewLine +
                "Please give the method an unique name to distinguish it from other overloads."),
            UnableToSelectConstructor = Of(
                "Unable to select single constructor from {0} available in {1}." + Environment.NewLine
                + "Please provide constructor selector when registering service."),
            ResolvingOpenGenericServiceTypeIsNotPossible = Of(
                "Resolving open-generic service type is not possible for type: {0}."),
            RecursiveDependencyDetected = Of(
                "Recursive dependency is detected when resolving" + Environment.NewLine + "{0}."),
            ScopeIsDisposed = Of(
                "Scope is disposed and scoped instances are no longer available."),
            NotFoundOpenGenericImplTypeArgInService = Of(
                "Unable to find for open-generic implementation {0} the type argument {1} when resolving {2}."),
            UnableToGetConstructorFromSelector = Of(
                "Unable to get constructor of {0} using provided constructor selector when resolving {1}."),
            UnableToFindCtorWithAllResolvableArgs = Of(
                "Unable to find constructor with all resolvable parameters when resolving {0}."),
            UnableToFindMatchingCtorForFuncWithArgs = Of(
                "Unable to find constructor with all parameters matching Func signature {0} " + Environment.NewLine
                + "and the rest of parameters resolvable from Container when resolving: {1}."),
            RegedFactoryDlgResultNotOfServiceType = Of(
                "Registered factory delegate returns service {0} is not assignable to {2}."),
            NotFoundSpecifiedWritablePropertyOrField = Of(
                "Unable to find writable property or field \"{0}\" when resolving: {1}."),
            PushingToRequestWithoutFactory = Of(
                "Pushing next info {0} to request not yet resolved to factory: {1}"),
            NoMatchedGenericParamConstraints = Of(
                "Open-generic service does not match with registered open-generic implementation constraints {0} when resolving: {1}."),
            GenericWrapperWithMultipleTypeArgsShouldSpecifyArgIndex = Of(
                "Generic wrapper type {0} should specify what type argument is wrapped, but it does not."),
            GenericWrapperTypeArgIndexOutOfBounds = Of(
                "Registered generic wrapper {0} specified type argument index {1} is out of type argument list."),
            DependencyHasShorterReuseLifespan = Of(
                "Dependency {0} reuse {1} lifespan shorter than its parent's: {2}" + Environment.NewLine +
                "To turn Off this error, specify the rule with new Container(rules => rules.WithoutThrowIfDependencyHasShorterReuseLifespan())."),
            WeakRefReuseWrapperGCed = Of(
                "Reused service wrapped in WeakReference is Garbage Collected and no longer available."),
            ServiceIsNotAssignableFromFactoryMethod = Of(
                "Service of {0} is not assignable from factory method {1} when resolving: {2}."),
            FactoryObjIsNullInFactoryMethod = Of(
                "Unable to use null factory object with *instance* factory method {0} when resolving: {1}."),
            FactoryObjProvidedButMethodIsStatic = Of(
                "Factory instance provided {0} But factory method is static {1} when resolving: {2}."),
            GotNullConstructorFromFactoryMethod = Of(
                "Got null constructor when resolving {0}"),
            UnableToRegisterDuplicateDefault = Of(
                "Service {0} without key is already registered as {1}."),
            UnableToRegisterDuplicateKey = Of(
                "Unable to register service {0} with duplicate key '{1}'" + Environment.NewLine +
                " There is already registered service with the same key: {2}."),
            NoCurrentScope = Of(
                "No current scope available: probably you are registering to, or resolving from outside of scope."),
            ContainerIsDisposed = Of(
                "Container is disposed and should not be used: {0}"),
            NotDirectScopeParent = Of(
                "Unable to OpenScope [{0}] because parent scope [{1}] is not current context scope [{2}]." +
                Environment.NewLine +
                "It is probably other scope was opened in between OR you forgot to Dispose some other scope!"),
            NoMatchedScopeFound = Of(
                "Unable to find matching scope with name {1} starting from the current scope {0}."),
            NoMatchingScopeWhenRegisteringInstance = Of(
                "No matching scope when registering instance [{0}] with {1}." + Environment.NewLine +
                "You could register delegate returning instance instead. That will succeed as long as scope is available at resolution."),
            ResolutionScopeIsNotSupportedForRegisterInstance = Of(
                "ResolutionScope reuse is not supported for registering instance: {0}"),
            NotSupportedMadeExpression = Of(
                "Only expression of method call, property getter, or new statement (with optional property initializer) is supported, but found: {0}."),
            UnexpectedFactoryMemberExpression = Of(
                "Expected property getter, but found {0}."),
            UnexpectedExpressionInsteadOfArgMethod = Of(
                "Expected DryIoc.Arg method call to specify parameter/property/field, but found: {0}."),
            UnexpectedExpressionInsteadOfConstant = Of(
                "Expected constant expression to specify parameter/property/field, but found something else: {0}."),
            InjectedCustomValueIsOfDifferentType = Of(
                "Injected value {0} is not assignable to {2}."),
            StateIsRequiredToUseItem = Of(
                "Runtime state is required to inject (or use) the: {0}. " + Environment.NewLine +
                "The reason is using RegisterDelegate, UseInstance, RegisterInitializer/Disposer, or registering with non-primitive service key, or metadata." + Environment.NewLine +
                "You can convert run-time value to expression via container.With(rules => rules.WithItemToExpressionConverter(YOUR_ITEM_TO_EXPRESSION_CONVERTER))."),
            ArgValueIndexIsProvidedButNoArgValues = Of(
                "Arg.Index of value is used but no values are passed"),
            ArgValueIndexIsOutOfProvidedArgValues = Of(
                "Arg.Index {0} is outside of provided values: {1}"),
            ResolutionNeedsRequiredServiceType = Of(
                "Expecting required service type but it is not specified when resolving: {0}"),
            RegisterMappingNotFoundRegisteredService = Of(
                "When registering mapping, Container is unable to find factory of registered service type {0} and key {1}."),
            RegisteringInstanceNotAssignableToServiceType = Of(
                "Registered instance {0} is not assignable to serviceType {1}."),
            RegisteringWithNotSupportedDepedendencyCustomValueType = Of(
                "Registering {0} dependency with not supported custom value type {1}." + Environment.NewLine +
                "Only DryIoc.DefaultValue, System.Type, .NET primitives types, or array of those are supported."),
            NoMoreRegistrationsAllowed = Of(
                "Container does not allow further registrations." + Environment.NewLine +
                "Attempting to register {0}{1} with implementation factory {2}."),
            NoMoreUnregistrationsAllowed = Of(
                "Container does not allow further registry modification." + Environment.NewLine +
                "Attempting to Unregister {0}{1} with factory type {2}."),
            GotNullFactoryWhenResolvingService = Of(
                "Got null factory method when resolving {0}"),
            RegisteredDisposableTransientWontBeDisposedByContainer = Of(
                "Registered Disposable Transient service {0} with key {1} registered as {2} won't be disposed by container." +
                " DryIoc does not hold reference to resolved transients, and therefore does not control their dispose." +
                " To silence this exception Register<YourService>(setup: Setup.With(allowDisposableTransient: true)) " +
                " or set the rule Container(rules => rules.WithoutThrowOnRegisteringDisposableTransient())." +
                " To enable tracking use Register<YourService>(setup: Setup.With(trackDisposableTransient: true)) " +
                " or set the rule Container(rules => rules.WithTrackingDisposableTransients())"),
            NotPossibleToResolveLazyInsideFuncWithArgs = Of(
                "Unable to resolve Lazy service inside Func<args..> because arguments can't be passed through" +
                " Lazy boundaries: {0}"),
            NotPossibleToResolveLazyEnumerableInsideFuncWithArgs = Of(
                "Unable to resolve LazyEnumerable service inside Func<args..> because arguments can't be passed through" +
                " lazy boundaries: {0}"),
            UnableToUseInstanceForExistingNonInstanceFactory = Of(
                "Unable to use the keyed instance {0} because of existing non-instance keyed registration: {1}"),
            NotFoundMetaCtorWithTwoArgs = Of(
                "Expecting Meta wrapper public constructor with two arguments {0} but not found when resolving: {1}"),
            UnableToSelectFromManyRegistrationsWithMatchingMetadata = Of(
                "Unable to select from multiple registrations matching the Metadata type {0}:" + Environment.NewLine +
                "{1}" + Environment.NewLine +
                "When resolving: {2}"),
            ImplTypeIsNotSpecifiedForAutoCtorSelection = Of(
                "Implementation type is not specified when using automatic constructor selection: {0}");

#pragma warning restore 1591 // "Missing XML-comment"

        /// <summary>Stores new error message and returns error code for it.</summary>
        /// <param name="message">Error message to store.</param> <returns>Error code for message.</returns>
        public static int Of(string message)
        {
            Messages.Add(message);
            return FirstErrorCode + Messages.Count - 1;
        }

        /// <summary>Returns the name for the provided error code.</summary>
        /// <param name="error">error code.</param> <returns>name of error, unique in scope of this <see cref="Error"/> class.</returns>
        public static string NameOf(int error)
        {
            var index = error - FirstErrorCode + 1;
            var field = typeof(Error).GetTypeInfo().DeclaredFields
                .Where(f => f.FieldType == typeof(int))
                .Where((_, i) => i == index)
                .FirstOrDefault();
            return field != null ? field.Name : null;
        }

        static Error()
        {
            Throw.GetMatchedException = ContainerException.Of;
        }
    }

    // todo: V3: move into Throw as a nested enum
    /// <summary>Checked error condition, possible error sources.</summary>
    internal enum ErrorCheck
    {
        /// <summary>Unspecified, just throw.</summary>
        Unspecified,
        /// <summary>Predicate evaluated to false.</summary>
        InvalidCondition,
        /// <summary>Checked object is null.</summary>
        IsNull,
        /// <summary>Checked object is of unexpected type.</summary>
        IsNotOfType,
        /// <summary>Checked type is not assignable to expected type</summary>
        TypeIsNotOfType,
        /// <summary>Invoked operation throws, it is source of inner exception.</summary>
        OperationThrows,
    }

    /// <summary>Enables more clean error message formatting and a bit of code contracts.</summary>
    internal static class Throw
    {
        private static string[] CreateDefaultMessages()
        {
            var messages = new string[(int)ErrorCheck.OperationThrows + 1];
            messages[(int)ErrorCheck.Unspecified] = "The error reason is unspecified, which is bad thing.";
            messages[(int)ErrorCheck.InvalidCondition] = "Argument {0} of type {1} has invalid condition.";
            messages[(int)ErrorCheck.IsNull] = "Argument of type {0} is null.";
            messages[(int)ErrorCheck.IsNotOfType] = "Argument {0} is not of type {1}.";
            messages[(int)ErrorCheck.TypeIsNotOfType] = "Type argument {0} is not assignable from type {1}.";
            messages[(int)ErrorCheck.OperationThrows] = "Invoked operation throws the inner exception {0}.";
            return messages;
        }

        private static readonly string[] _defaultMessages = CreateDefaultMessages();

        /// <summary>Returns the default message specified for <see cref="ErrorCheck"/> code.</summary>
        /// <param name="error">Error code to get message for.</param> <returns>String format message.</returns>
        public static string GetDefaultMessage(ErrorCheck error)
        {
            return _defaultMessages[(int)error];
        }

        /// <summary>Declares mapping between <see cref="ErrorCheck"/> type and <paramref name="error"/> code to specific <see cref="Exception"/>.</summary>
        /// <returns>Returns mapped exception.</returns>
        internal delegate Exception GetMatchedExceptionHandler(ErrorCheck errorCheck, int error, object arg0, object arg1, object arg2, object arg3, Exception inner);

        /// <summary>Returns matched exception for error check and error code.</summary>
        public static GetMatchedExceptionHandler GetMatchedException = ContainerException.Of;

        /// <summary>Throws matched exception if throw condition is true.</summary>
        /// <param name="throwCondition">Condition to be evaluated, throws if result is true, otherwise - does nothing.</param>
        /// <param name="error">Error code to match to exception thrown.</param>
        /// <param name="arg0">Arguments to formatted message.</param> <param name="arg1"></param> <param name="arg2"></param> <param name="arg3"></param>
        public static void If(bool throwCondition, int error = -1, object arg0 = null, object arg1 = null, object arg2 = null, object arg3 = null)
        {
            if (!throwCondition) return;
            throw GetMatchedException(ErrorCheck.InvalidCondition, error, arg0, arg1, arg2, arg3, null);
        }

        /// <summary>Throws matched exception if throw condition is true. Otherwise return source <paramref name="arg0"/>.</summary>
        /// <typeparam name="T">Type of source <paramref name="arg0"/>.</typeparam>
        /// <param name="arg0">In case of exception <paramref name="arg0"/> will be used as first argument in formatted message.</param>
        /// <param name="throwCondition">Condition to be evaluated, throws if result is true, otherwise - does nothing.</param>
        /// <param name="error">Error code to match to exception thrown.</param>
        /// <param name="arg1">Rest of arguments to formatted message.</param> <param name="arg2"></param> <param name="arg3"></param>
        /// <returns><paramref name="arg0"/> if throw condition is false.</returns>
        public static T ThrowIf<T>(this T arg0, bool throwCondition, int error = -1, object arg1 = null, object arg2 = null, object arg3 = null)
        {
            if (!throwCondition) return arg0;
            throw GetMatchedException(ErrorCheck.InvalidCondition, error, arg0, arg1, arg2, arg3, null);
        }

        /// <summary>Throws exception if <paramref name="arg"/> is null, otherwise returns <paramref name="arg"/>.</summary>
        /// <param name="arg">Argument to check for null.</param>
        /// <param name="error">Error code.</param>
        /// <param name="arg0"></param> <param name="arg1"></param> <param name="arg2"></param> <param name="arg3"></param>
        /// <typeparam name="T">Type of argument to check and return.</typeparam>
        /// <returns><paramref name="arg"/> if it is not null.</returns>
        public static T ThrowIfNull<T>(this T arg, int error = -1, object arg0 = null, object arg1 = null, object arg2 = null, object arg3 = null)
            where T : class
        {
            if (arg != null) return arg;
            throw GetMatchedException(ErrorCheck.IsNull, error, arg0 ?? typeof(T), arg1, arg2, arg3, null);
        }

        /// <summary>Throws exception if <paramref name="arg0"/> is not assignable to type specified by <paramref name="arg1"/>,
        /// otherwise just returns <paramref name="arg0"/>.</summary>
        /// <typeparam name="T">Type of argument to check and return if no error.</typeparam>
        /// <param name="arg0">Instance to check if it is assignable to type <paramref name="arg1"/>.</param>
        /// <param name="arg1">Type to check <paramref name="arg0"/> against.</param>
        /// <param name="error">Error code</param>
        /// <param name="arg2"></param> <param name="arg3"></param>
        /// <returns><paramref name="arg0"/> if it assignable to <paramref name="arg1"/>.</returns>
        public static T ThrowIfNotOf<T>(this T arg0, Type arg1, int error = -1, object arg2 = null, object arg3 = null)
            where T : class
        {
            if (arg1.IsTypeOf(arg0)) return arg0;
            throw GetMatchedException(ErrorCheck.IsNotOfType, error, arg0, arg1, arg2, arg3, null);
        }

        /// <summary>Throws if <paramref name="arg0"/> is not assignable from <paramref name="arg1"/>.</summary>
        /// <param name="arg0"></param> <param name="arg1"></param>
        /// <param name="error">Error code</param>
        ///  <param name="arg2"></param> <param name="arg3"></param>
        /// <returns><paramref name="arg0"/> if no exception.</returns>
        public static Type ThrowIfNotImplementedBy(this Type arg0, Type arg1, int error = -1, object arg2 = null, object arg3 = null)
        {
            if (arg1.IsAssignableTo(arg0)) return arg0;
            throw GetMatchedException(ErrorCheck.TypeIsNotOfType, error, arg0, arg1, arg2, arg3, null);
        }

        /// <summary>Invokes <paramref name="operation"/> and in case of <typeparamref name="TEx"/> re-throws it as inner-exception.</summary>
        /// <typeparam name="TEx">Exception to check and handle, and then wrap as inner-exception.</typeparam>
        /// <typeparam name="T">Result of <paramref name="operation"/>.</typeparam>
        /// <param name="operation">To invoke</param>
        /// <param name="throwCondition">Condition to be evaluated, throws if result is true, otherwise - does nothing.</param>
        /// <param name="error">Error code</param>
        /// <param name="arg0"></param>
        /// <param name="arg1"></param>
        /// <param name="arg2"></param>
        /// <param name="arg3"></param>
        /// <returns>Result of <paramref name="operation"/> if no exception.</returns>
        public static T IfThrows<TEx, T>(Func<T> operation, bool throwCondition, int error, object arg0 = null, object arg1 = null, object arg2 = null, object arg3 = null) where TEx : Exception
        {
            try
            {
                return operation();
            }
            catch (TEx ex)
            {
                if (throwCondition)
                    throw GetMatchedException(ErrorCheck.OperationThrows, error, arg0, arg1, arg2, arg3, ex);
                return default(T);
            }
        }

        /// <summary>Just throws the exception with the <paramref name="error"/> code.</summary>
        /// <param name="error">Error code.</param>
        /// <param name="arg0"></param> <param name="arg1"></param> <param name="arg2"></param> <param name="arg3"></param>
        public static object It(int error, object arg0 = null, object arg1 = null, object arg2 = null, object arg3 = null)
        {
            throw GetMatchedException(ErrorCheck.Unspecified, error, arg0, arg1, arg2, arg3, null);
        }

        /// <summary>Throws <paramref name="error"/> instead of returning value of <typeparamref name="T"/>.
        /// Supposed to be used in expression that require some return value.</summary>
        /// <typeparam name="T"></typeparam> <param name="error"></param>
        /// <param name="arg0"></param> <param name="arg1"></param> <param name="arg2"></param> <param name="arg3"></param>
        /// <returns>Does not return, throws instead.</returns>
        public static T For<T>(int error, object arg0 = null, object arg1 = null, object arg2 = null, object arg3 = null)
        {
            throw GetMatchedException(ErrorCheck.Unspecified, error, arg0, arg1, arg2, arg3, null);
        }
    }

    /// <summary>Called from generated code.</summary>
    internal static class ThrowInGeneratedCode
    {
        /// <summary>Throws if object is null.</summary>
        /// <param name="obj">object to check.</param><param name="message">Error message.</param>
        /// <returns>object if not null.</returns>
        public static object ThrowNewErrorIfNull(this object obj, string message)
        {
            if (obj == null) Throw.It(Error.Of(message));
            return obj;
        }
    }

    /// <summary>Contains helper methods to work with Type: for instance to find Type implemented base types and interfaces, etc.</summary>
    internal static class ReflectionTools
    {
        /// <summary>Flags for <see cref="GetImplementedTypes"/> method.</summary>
        [Flags]
        internal enum AsImplementedType
        {
            /// <summary>Include nor object not source type.</summary>
            None = 0,
            /// <summary>Include source type to list of implemented types.</summary>
            SourceType = 1,
            /// <summary>Include <see cref="System.Object"/> type to list of implemented types.</summary>
            ObjectType = 2
        }

        /// <summary>Returns all interfaces and all base types (in that order) implemented by <paramref name="sourceType"/>.
        /// Specify <paramref name="asImplementedType"/> to include <paramref name="sourceType"/> itself as first item and
        /// <see cref="object"/> type as the last item.</summary>
        /// <param name="sourceType">Source type for discovery.</param>
        /// <param name="asImplementedType">Additional types to include into result collection.</param>
        /// <returns>Array of found types, empty if nothing found.</returns>
        public static Type[] GetImplementedTypes(this Type sourceType, AsImplementedType asImplementedType = AsImplementedType.None)
        {
            Type[] results;

            var interfaces = sourceType.GetImplementedInterfaces();
            var interfaceStartIndex = (asImplementedType & AsImplementedType.SourceType) == 0 ? 0 : 1;
            var includingObjectType = (asImplementedType & AsImplementedType.ObjectType) == 0 ? 0 : 1;
            var sourcePlusInterfaceCount = interfaceStartIndex + interfaces.Length;

            var baseType = sourceType.GetTypeInfo().BaseType;
            if (baseType == null || baseType == typeof(object))
                results = new Type[sourcePlusInterfaceCount + includingObjectType];
            else
            {
                List<Type> baseBaseTypes = null;
                for (var bb = baseType.GetTypeInfo().BaseType; bb != null && bb != typeof(object); bb = bb.GetTypeInfo().BaseType)
                    (baseBaseTypes ?? (baseBaseTypes = new List<Type>(2))).Add(bb);

                if (baseBaseTypes == null)
                    results = new Type[sourcePlusInterfaceCount + includingObjectType + 1];
                else
                {
                    results = new Type[sourcePlusInterfaceCount + baseBaseTypes.Count + includingObjectType + 1];
                    baseBaseTypes.CopyTo(results, sourcePlusInterfaceCount + 1);
                }

                results[sourcePlusInterfaceCount] = baseType;
            }

            if (interfaces.Length == 1)
                results[interfaceStartIndex] = interfaces[0];
            else if (interfaces.Length > 1)
                Array.Copy(interfaces, 0, results, interfaceStartIndex, interfaces.Length);

            if (interfaceStartIndex == 1)
                results[0] = sourceType;
            if (includingObjectType == 1)
                results[results.Length - 1] = typeof(object);

            return results;
        }

        /// <summary>Gets a collection of the interfaces implemented by the current type and its base types.</summary>
        /// <param name="type">Source type</param>
        /// <returns>Collection of interface types.</returns>
        public static Type[] GetImplementedInterfaces(this Type type)
        {
            return type.GetTypeInfo().ImplementedInterfaces.ToArrayOrSelf();
        }

        /// <summary>Gets all declared and base members.</summary>
        /// <param name="type">Type to get members from.</param>
        /// <param name="includeBase">(optional) When set looks into base members.</param>
        /// <returns>All members.</returns>
        public static IEnumerable<MemberInfo> GetAllMembers(this Type type, bool includeBase = false)
        {
            return type.GetMembers(t =>
                    t.DeclaredMethods.Cast<MemberInfo>().Concat(
                        t.DeclaredProperties.Cast<MemberInfo>().Concat(
                            t.DeclaredFields.Cast<MemberInfo>())),
                includeBase);
        }

        /// <summary>Returns true if <paramref name="openGenericType"/> contains all generic parameters
        /// from <paramref name="genericParameters"/>.</summary>
        /// <param name="openGenericType">Expected to be open-generic type, throws otherwise.</param>
        /// <param name="genericParameters">Generic parameters.</param>
        /// <returns>Returns true if contains, and false otherwise.</returns>
        public static bool ContainsAllGenericTypeParameters(this Type openGenericType, Type[] genericParameters)
        {
            if (!openGenericType.IsOpenGeneric())
                return false;

            // todo: may be replaced with more lightweight Bits flags.
            var matchedParams = new Type[genericParameters.Length];
            Array.Copy(genericParameters, matchedParams, genericParameters.Length);

            SetToNullGenericParametersReferencedInConstraints(matchedParams);
            SetToNullMatchesFoundInGenericParameters(matchedParams, openGenericType.GetGenericParamsAndArgs());

            for (var i = 0; i < matchedParams.Length; i++)
                if (matchedParams[i] != null)
                    return false;
            return true;
        }

        /// <summary>Returns true if class is compiler generated. Checking for CompilerGeneratedAttribute
        /// is not enough, because this attribute is not applied for classes generated from "async/await".</summary>
        /// <param name="type">Type to check.</param> <returns>Returns true if type is compiler generated.</returns>
        public static bool IsCompilerGenerated(this Type type)
        {
            return type.FullName != null && type.FullName.Contains("<>c__DisplayClass");
        }

        /// <summary>Returns true if type is generic.</summary><param name="type">Type to check.</param> <returns>True if type generic.</returns>
        public static bool IsGeneric(this Type type)
        {
            return type.GetTypeInfo().IsGenericType;
        }

        /// <summary>Returns true if type is generic type definition (open type).</summary><param name="type">Type to check.</param>
        /// <returns>True if type is open type: generic type definition.</returns>
        public static bool IsGenericDefinition(this Type type)
        {
            return type.GetTypeInfo().IsGenericTypeDefinition;
        }

        /// <summary>Returns true if type is closed generic: does not have open generic parameters, only closed/concrete ones.</summary>
        /// <param name="type">Type to check</param> <returns>True if closed generic.</returns>
        public static bool IsClosedGeneric(this Type type)
        {
            var typeInfo = type.GetTypeInfo();
            return typeInfo.IsGenericType && !typeInfo.ContainsGenericParameters;
        }

        /// <summary>Returns true if type if open generic: contains at list one open generic parameter. Could be
        /// generic type definition as well.</summary>
        /// <param name="type">Type to check.</param> <returns>True if open generic.</returns>
        public static bool IsOpenGeneric(this Type type)
        {
            var typeInfo = type.GetTypeInfo();
            return typeInfo.IsGenericType && typeInfo.ContainsGenericParameters;
        }

        /// <summary>Returns generic type definition if type is generic and null otherwise.</summary>
        /// <param name="type">Source type, could be null.</param> <returns>Generic type definition.</returns>
        public static Type GetGenericDefinitionOrNull(this Type type)
        {
            return type != null && type.GetTypeInfo().IsGenericType ? type.GetGenericTypeDefinition() : null;
        }

        /// <summary>Returns generic type parameters and arguments in order they specified. If type is not generic, returns empty array.</summary>
        /// <param name="type">Source type.</param> <returns>Array of generic type arguments (closed/concrete types) and parameters (open).</returns>
        public static Type[] GetGenericParamsAndArgs(this Type type)
        {
            var typeInfo = type.GetTypeInfo();
            return typeInfo.IsGenericTypeDefinition
                ? typeInfo.GenericTypeParameters
                : typeInfo.GenericTypeArguments;
        }

        /// <summary>Returns array of interface and base class constraints for provider generic parameter type.</summary>
        /// <param name="type">Generic parameter type.</param>
        /// <returns>Array of interface and base class constraints.</returns>
        public static Type[] GetGenericParamConstraints(this Type type)
        {
            return type.GetTypeInfo().GetGenericParameterConstraints();
        }

        /// <summary>If type is array returns is element type, otherwise returns null.</summary>
        /// <param name="type">Source type.</param> <returns>Array element type or null.</returns>
        public static Type GetArrayElementTypeOrNull(this Type type)
        {
            var typeInfo = type.GetTypeInfo();
            return typeInfo.IsArray ? typeInfo.GetElementType() : null;
        }

        /// <summary>Return base type or null, if not exist (the case for only for object type).</summary>
        /// <param name="type">Source type.</param> <returns>Base type or null for object.</returns>
        public static Type GetBaseType(this Type type)
        {
            return type.GetTypeInfo().BaseType;
        }

        /// <summary>Checks if type is public or nested public in public type.</summary>
        /// <param name="type">Type to check.</param> <returns>Return true if check succeeded.</returns>
        public static bool IsPublicOrNestedPublic(this Type type)
        {
            var typeInfo = type.GetTypeInfo();
            return typeInfo.IsPublic || typeInfo.IsNestedPublic && typeInfo.DeclaringType.IsPublicOrNestedPublic();
        }

        /// <summary>Returns true if type is class.</summary>
        /// <param name="type">Type to check.</param> <returns>Check result.</returns>
        public static bool IsClass(this Type type)
        {
            return type.GetTypeInfo().IsClass;
        }

        /// <summary>Returns true if type is value type.</summary>
        /// <param name="type">Type to check.</param> <returns>Check result.</returns>
        public static bool IsValueType(this Type type)
        {
            return type.GetTypeInfo().IsValueType;
        }

        /// <summary>Returns true if type is interface.</summary>
        /// <param name="type">Type to check.</param> <returns>Check result.</returns>
        public static bool IsInterface(this Type type)
        {
            return type.GetTypeInfo().IsInterface;
        }

        /// <summary>Returns true if type if abstract or interface.</summary>
        /// <param name="type">Type to check.</param> <returns>Check result.</returns>
        public static bool IsAbstract(this Type type)
        {
            return type.GetTypeInfo().IsAbstract;
        }

        /// <summary>Returns true if type is static.</summary>
        /// <param name="type">Type</param> <returns>True is static.</returns>
        public static bool IsStatic(this Type type)
        {
            var typeInfo = type.GetTypeInfo();
            return typeInfo.IsAbstract && typeInfo.IsSealed;
        }

        /// <summary>Returns true if type is enum type.</summary>
        /// <param name="type">Type to check.</param> <returns>Check result.</returns>
        public static bool IsEnum(this Type type)
        {
            return type.GetTypeInfo().IsEnum;
        }

        /// <summary>Returns true if instance of type is assignable to instance of <paramref name="other"/> type.</summary>
        /// <param name="type">Type to check, could be null.</param>
        /// <param name="other">Other type to check, could be null.</param>
        /// <returns>Check result.</returns>
        public static bool IsAssignableTo(this Type type, Type other)
        {
            return type != null && other != null && other.GetTypeInfo().IsAssignableFrom(type.GetTypeInfo());
        }

        /// <summary>Returns true if type of <paramref name="obj"/> is assignable to source <paramref name="type"/>.</summary>
        /// <param name="type">Is type of object.</param> <param name="obj">Object to check.</param>
        /// <returns>Check result.</returns>
        public static bool IsTypeOf(this Type type, object obj)
        {
            return obj != null && obj.GetType().IsAssignableTo(type);
        }

        /// <summary>Returns true if provided type IsPitmitive in .Net terms, or enum, or string
        /// , or array of primitives if <paramref name="orArrayOfPrimitives"/> is true.</summary>
        /// <param name="type">Type to check.</param>
        /// <param name="orArrayOfPrimitives">Says to return true for array or primitives recursively.</param>
        /// <returns>Check result.</returns>
        public static bool IsPrimitive(this Type type, bool orArrayOfPrimitives = false)
        {
            var typeInfo = type.GetTypeInfo();
            return typeInfo.IsPrimitive || typeInfo.IsEnum || type == typeof(string)
                   || orArrayOfPrimitives && typeInfo.IsArray && typeInfo.GetElementType().IsPrimitive(true);
        }

        /// <summary>Returns all attributes defined on <paramref name="type"/>.</summary>
        /// <param name="type">Type to get attributes for.</param>
        /// <param name="attributeType">(optional) Check only for that attribute type, otherwise for any attribute.</param>
        /// <param name="inherit">(optional) Additionally check for attributes inherited from base type.</param>
        /// <returns>Sequence of found attributes or empty.</returns>
        public static Attribute[] GetAttributes(this Type type, Type attributeType = null, bool inherit = false)
        {
            return type.GetTypeInfo().GetCustomAttributes(attributeType ?? typeof(Attribute), inherit)
                // ReSharper disable once RedundantEnumerableCastCall
                .Cast<Attribute>() // required in .NET 4.5
                .ToArrayOrSelf();
        }

        /// <summary>Recursive method to enumerate all input type and its base types for specific details.
        /// Details are returned by <paramref name="getMembers"/> delegate.</summary>
        /// <typeparam name="TMember">Details type: properties, fields, methods, etc.</typeparam>
        /// <param name="type">Input type.</param> <param name="getMembers">Get declared type details.</param>
        /// <param name="includeBase">(optional) When set looks into base members.</param>
        /// <returns>Enumerated details info objects.</returns>
        public static IEnumerable<TMember> GetMembers<TMember>(this Type type,
            Func<TypeInfo, IEnumerable<TMember>> getMembers,
            bool includeBase = false)
        {
            var typeInfo = type.GetTypeInfo();
            var members = getMembers(typeInfo);
            if (!includeBase)
                return members;
            var baseType = typeInfo.BaseType;
            return baseType == null || baseType == typeof(object)
                ? members
                : members.Concat(baseType.GetMembers(getMembers, true));
        }

        // todo: V3: remove.
        /// <summary>Obsolete: replaced with <see cref="GetMembers{TMember}"/>.</summary>
        public static IEnumerable<T> GetDeclaredAndBase<T>(this Type type, Func<TypeInfo, IEnumerable<T>> getDeclared)
        {
            var typeInfo = type.GetTypeInfo();
            var declared = getDeclared(typeInfo);
            var baseType = typeInfo.BaseType;
            return baseType == null || baseType == typeof(object) ? declared
                : declared.Concat(baseType.GetDeclaredAndBase(getDeclared));
        }

        /// <summary>Returns all public instance constructors for the type</summary>
        /// <param name="type"></param> <returns></returns>
        public static IEnumerable<ConstructorInfo> GetPublicInstanceConstructors(this Type type)
        {
            return type.GetTypeInfo().DeclaredConstructors.Where(c => c.IsPublic && !c.IsStatic);
        }

        /// <summary>Enumerates all constructors from input type.</summary>
        /// <param name="type">Input type.</param>
        /// <param name="includeNonPublic">(optional) If set include non-public constructors into result.</param>
        /// <param name="includeStatic">(optional) Turned off by default.</param>
        /// <returns>Enumerated constructors.</returns>
        public static IEnumerable<ConstructorInfo> GetAllConstructors(this Type type,
            bool includeNonPublic = false, bool includeStatic = false)
        {
            var ctors = type.GetTypeInfo().DeclaredConstructors;
            if (!includeNonPublic) ctors = ctors.Where(c => c.IsPublic);
            if (!includeStatic) ctors = ctors.Where(c => !c.IsStatic);
            return ctors;
        }

        /// <summary>Searches and returns constructor by its signature.</summary>
        /// <param name="type">Input type.</param>
        /// <param name="includeNonPublic">(optional) If set include non-public constructors into result.</param>
        /// <param name="args">Signature - constructor argument types.</param>
        /// <returns>Found constructor or null.</returns>
        public static ConstructorInfo GetConstructorOrNull(this Type type, bool includeNonPublic = false, params Type[] args)
        {
            return type.GetAllConstructors(includeNonPublic)
                .FirstOrDefault(c => c.GetParameters().Select(p => p.ParameterType).SequenceEqual(args));
        }

        /// <summary>Returns single constructor, otherwise if no or more than one: returns false.</summary>
        /// <param name="type">Type to inspect.</param>
        /// <param name="includeNonPublic">(optional) If set includes non-public constructors.</param>
        /// <returns>Single constructor or null.</returns>
        public static ConstructorInfo GetSingleConstructorOrNull(this Type type, bool includeNonPublic = false)
        {
            var ctors = type.GetAllConstructors(includeNonPublic).ToArrayOrSelf();
            return ctors.Length == 1 ? ctors[0] : null;
        }

        /// <summary>Returns single declared (not inherited) method by name, or null if not found.</summary>
        /// <param name="type">Input type</param> <param name="name">Method name to look for.</param>
        /// <param name="includeNonPublic">(optional) If set includes non public methods into search.</param>
        /// <returns>Found method or null.</returns>
        public static MethodInfo GetSingleMethodOrNull(this Type type, string name, bool includeNonPublic = false)
        {
            var methods = type.GetTypeInfo().DeclaredMethods
                .Where(m => (includeNonPublic || m.IsPublic) && m.Name == name)
                .ToArrayOrSelf();
            return methods.Length == 1 ? methods[0] : null;
        }

        /// <summary>Returns declared (not inherited) method by name and argument types, or null if not found.</summary>
        /// <param name="type">Input type</param> <param name="name">Method name to look for.</param>
        /// <param name="paramTypes">Argument types</param> <returns>Found method or null.</returns>
        public static MethodInfo GetMethodOrNull(this Type type, string name, params Type[] paramTypes)
        {
            var typeInfo = type.GetTypeInfo();
            var paramCount = paramTypes.Length;
            foreach (var method in typeInfo.DeclaredMethods)
            {
                if (method.Name == name)
                {
                    var methodParams = method.GetParameters();
                    if (paramCount == methodParams.Length)
                    {
                        if (paramCount == 0)
                            return method;

                        if (paramCount == 1)
                        {
                            if (paramTypes[0] == methodParams[0].ParameterType)
                                return method;
                        }
                        else
                        {
                            var i = 0;
                            for (; i < paramCount; ++i)
                                if (paramTypes[i] != methodParams[i].ParameterType)
                                    break;
                            if (i == paramCount)
                                return method;
                        }
                    }
                }
            }

            return null;
        }

        /// <summary>Returns property by name, including inherited. Or null if not found.</summary>
        /// <param name="type">Input type.</param> <param name="name">Property name to look for.</param>
        /// <returns>Found property or null.</returns>
        public static PropertyInfo GetPropertyOrNull(this Type type, string name)
        {
            return type.GetMembers(_ => _.DeclaredProperties, includeBase: true).FirstOrDefault(p => p.Name == name);
        }

        /// <summary>Returns field by name, including inherited. Or null if not found.</summary>
        /// <param name="type">Input type.</param> <param name="name">Field name to look for.</param>
        /// <returns>Found field or null.</returns>
        public static FieldInfo GetFieldOrNull(this Type type, string name)
        {
            return type.GetMembers(_ => _.DeclaredFields, includeBase: true).FirstOrDefault(p => p.Name == name);
        }

        /// <summary>Returns type assembly.</summary> <param name="type">Input type</param> <returns>Type assembly.</returns>
        public static Assembly GetAssembly(this Type type) { return type.GetTypeInfo().Assembly; }

        /// <summary>Returns true if member is static, otherwise returns false.</summary>
        /// <param name="member">Member to check.</param> <returns>True if static.</returns>
        public static bool IsStatic(this MemberInfo member)
        {
            var isStatic =
                member is MethodInfo ? ((MethodInfo)member).IsStatic :
                    member is PropertyInfo
                        ? (((PropertyInfo)member).GetGetMethodOrNull(includeNonPublic: true)
                           ?? ((PropertyInfo)member).GetSetMethodOrNull(includeNonPublic: true)).IsStatic :
                        ((FieldInfo)member).IsStatic;
            return isStatic;
        }

        /// <summary>Return either <see cref="PropertyInfo.PropertyType"/>, or <see cref="FieldInfo.FieldType"/>, <see cref="MethodInfo.ReturnType"/>.
        /// Otherwise returns null.</summary>
        /// <param name="member">Expecting member of type <see cref="PropertyInfo"/> or <see cref="FieldInfo"/> only.</param>
        /// <returns>Type of property of field.</returns>
        public static Type GetReturnTypeOrDefault(this MemberInfo member)
        {
            return member is ConstructorInfo ? member.DeclaringType
                : member is MethodInfo ? ((MethodInfo)member).ReturnType
                    : member is PropertyInfo ? ((PropertyInfo)member).PropertyType
                        : member is FieldInfo ? ((FieldInfo)member).FieldType
                            : null;
        }

        /// <summary>Returns true if field is backing field for property.</summary>
        /// <param name="field">Field to check.</param> <returns>Returns true if field is backing property.</returns>
        public static bool IsBackingField(this FieldInfo field)
        {
            return field.Name[0] == '<';
        }

        /// <summary>Returns true if property is indexer: aka this[].</summary>
        /// <param name="property">Property to check</param><returns>True if indexer.</returns>
        public static bool IsIndexer(this PropertyInfo property)
        {
            return property.GetIndexParameters().Length != 0;
        }

        /// <summary>Returns true if type is generated type of hoisted closure.</summary>
        /// <param name="type">Source type.</param> <returns>Check result.</returns>
        public static bool IsClosureType(this Type type)
        {
            return type.Name.Contains("<>c__DisplayClass");
        }

        /// <summary>Returns attributes defined for the member/method.</summary>
        /// <param name="member">Member to check.</param> <param name="attributeType">(optional) Specific attribute type to return, any attribute otherwise.</param>
        /// <param name="inherit">Check for inherited member attributes.</param> <returns>Found attributes or empty.</returns>
        public static IEnumerable<Attribute> GetAttributes(this MemberInfo member, Type attributeType = null, bool inherit = false)
        {
            return member.GetCustomAttributes(attributeType ?? typeof(Attribute), inherit).Cast<Attribute>();
        }

        /// <summary>Returns attributes defined for parameter.</summary>
        ///  <param name="parameter">Target parameter.</param>
        /// <param name="attributeType">(optional) Specific attribute type to return, any attribute otherwise.</param>
        /// <param name="inherit">Check for inherited attributes.</param> <returns>Found attributes or empty.</returns>
        public static IEnumerable<Attribute> GetAttributes(this ParameterInfo parameter, Type attributeType = null, bool inherit = false)
        {
            return parameter.GetCustomAttributes(attributeType ?? typeof(Attribute), inherit).Cast<Attribute>();
        }

        /// <summary>Get types from assembly that are loaded successfully.
        /// Hacks to <see cref="ReflectionTypeLoadException"/> for loaded types.</summary>
        /// <param name="assembly">Assembly to get types from.</param>
        /// <returns>Array of loaded types.</returns>
        public static Type[] GetLoadedTypes(this Assembly assembly)
        {
            try
            {
                return Portable.GetAssemblyTypes(assembly).ToArrayOrSelf();
            }
            catch (ReflectionTypeLoadException ex)
            {
                return ex.Types.Where(type => type != null).ToArray();
            }
        }

        /// <summary>Creates default(T) expression for provided <paramref name="type"/>.</summary>
        /// <param name="type">Type to get default value of.</param>
        /// <returns>Default value expression.</returns>
        public static Expression GetDefaultValueExpression(this Type type)
        {
            return Expression.Call(_getDefaultMethod.Value.MakeGenericMethod(type), ArrayTools.Empty<Expression>());
        }

        #region Implementation

        private static void SetToNullGenericParametersReferencedInConstraints(Type[] genericParams)
        {
            for (int i = 0; i < genericParams.Length; i++)
            {
                var genericParam = genericParams[i];
                if (genericParam == null)
                    continue;

                var genericConstraints = genericParam.GetGenericParamConstraints();
                for (var j = 0; j < genericConstraints.Length; j++)
                {
                    var genericConstraint = genericConstraints[j];
                    if (genericConstraint.IsOpenGeneric())
                    {
                        var constraintGenericParams = genericConstraint.GetGenericParamsAndArgs();
                        for (var k = 0; k < constraintGenericParams.Length; k++)
                        {
                            var constraintGenericParam = constraintGenericParams[k];
                            if (constraintGenericParam != genericParam)
                            {
                                var genericParamIndex = genericParams.IndexOf(constraintGenericParam);
                                if (genericParamIndex != -1)
                                    genericParams[genericParamIndex] = null;
                            }
                        }
                    }
                }
            }
        }

        private static void SetToNullMatchesFoundInGenericParameters(Type[] matchedParams, Type[] genericParams)
        {
            for (var i = 0; i < genericParams.Length; i++)
            {
                var genericParam = genericParams[i];
                if (genericParam.IsGenericParameter)
                {
                    var matchedIndex = matchedParams.IndexOf(genericParam);
                    if (matchedIndex != -1)
                        matchedParams[matchedIndex] = null;
                }
                else if (genericParam.IsOpenGeneric())
                    SetToNullMatchesFoundInGenericParameters(matchedParams, genericParam.GetGenericParamsAndArgs());
            }
        }

        private static readonly Lazy<MethodInfo> _getDefaultMethod = new Lazy<MethodInfo>(() =>
            typeof(ReflectionTools).GetMethodOrNull("GetDefault", ArrayTools.Empty<Type>()));

        internal static T GetDefault<T>() { return default(T); }

        #endregion
    }

    /// <summary>Provides pretty printing/debug view for number of types.</summary>
    internal static class PrintTools
    {
        /// <summary>Default separator used for printing enumerable.</summary>
        public static string DefaultItemSeparator = ", " + Environment.NewLine;

        /// <summary>Prints input object by using corresponding Print methods for know types.</summary>
        /// <param name="s">Builder to append output to.</param>
        /// <param name="x">Object to print.</param>
        /// <param name="quote">(optional) Quote to use for quoting string object.</param>
        /// <param name="itemSeparator">(optional) Separator for enumerable.</param>
        /// <param name="getTypeName">(optional) Custom type printing policy.</param>
        /// <returns>String builder with appended output.</returns>
        public static StringBuilder Print(this StringBuilder s, object x,
            string quote = null, string itemSeparator = null, Func<Type, string> getTypeName = null)
        {
            return x == null ? s.Append("null")
                : x is string ? s.Print((string)x, quote)
                    : x is Type ? s.Print((Type)x, getTypeName)
                        : (x is IEnumerable<Type> || x is IEnumerable) &&
                          !x.GetType().IsAssignableTo(typeof(IEnumerable<>).MakeGenericType(x.GetType())) // exclude infinite recursion and StackOverflowEx
                            ? s.Print((IEnumerable)x, itemSeparator ?? DefaultItemSeparator, (_, o) => _.Print(o, quote, null, getTypeName))
                            : s.Append(x);
        }

        /// <summary>Appends string to string builder quoting with <paramref name="quote"/> if provided.</summary>
        /// <param name="s">String builder to append string to.</param>
        /// <param name="str">String to print.</param>
        /// <param name="quote">(optional) Quote to add before and after string.</param>
        /// <returns>String builder with appended string.</returns>
        public static StringBuilder Print(this StringBuilder s, string str, string quote = null)
        {
            return quote == null ? s.Append(str) : s.Append(quote).Append(str).Append(quote);
        }

        /// <summary>Prints enumerable by using corresponding Print method for known item type.</summary>
        /// <param name="s">String builder to append output to.</param>
        /// <param name="items">Items to print.</param>
        /// <param name="separator">(optional) Custom separator if provided.</param>
        /// <param name="printItem">(optional) Custom item printer if provided.</param>
        /// <returns>String builder with appended output.</returns>
        public static StringBuilder Print(this StringBuilder s, IEnumerable items,
            string separator = ", ", Action<StringBuilder, object> printItem = null)
        {
            if (items == null) return s;
            printItem = printItem ?? ((_, x) => _.Print(x));
            var itemCount = 0;
            foreach (var item in items)
                printItem(itemCount++ == 0 ? s : s.Append(separator), item);
            return s;
        }

        /// <summary>Default delegate to print Type details: by default prints Type FullName and
        /// skips namespace if it start with "System."</summary>
        public static Func<Type, string> GetTypeNameDefault = t =>
#if DEBUG
            t.Name;
#else
            t.FullName != null && t.Namespace != null && !t.Namespace.StartsWith("System") ? t.FullName : t.Name;
#endif

        /// <summary>Appends type details to string builder.</summary>
        /// <param name="s">String builder to append output to.</param>
        /// <param name="type">Input type to print.</param>
        /// <param name="getTypeName">(optional) Delegate to provide custom type details.</param>
        /// <returns>String builder with appended output.</returns>
        public static StringBuilder Print(this StringBuilder s, Type type, Func<Type, string> getTypeName = null)
        {
            if (type == null) return s;

            getTypeName = getTypeName ?? GetTypeNameDefault;
            var typeName = getTypeName(type);

            var isArray = type.IsArray;
            if (isArray)
                type = type.GetElementType();

            if (!type.IsGeneric())
                return s.Append(typeName.Replace('+', '.'));

            s.Append(typeName.Substring(0, typeName.IndexOf('`')).Replace('+', '.')).Append('<');

            var genericArgs = type.GetGenericParamsAndArgs();
            if (type.IsGenericDefinition())
                s.Append(',', genericArgs.Length - 1);
            else
                s.Print(genericArgs, ", ", (_, t) => _.Print((Type)t, getTypeName));

            s.Append('>');

            if (isArray)
                s.Append("[]");

            return s;
        }
    }

    /// <summary>Ports some methods from .Net 4.0/4.5</summary>
    internal static partial class Portable
    {
        // note: fallback to DefinedTypes (PCL)
        /// <summary>Portable version of Assembly.GetTypes or Assembly.DefinedTypes.</summary>
        public static readonly Func<Assembly, IEnumerable<Type>> GetAssemblyTypes = GetAssemblyTypesMethod();

        private static Func<Assembly, IEnumerable<Type>> GetAssemblyTypesMethod()
        {
            var assemblyParamExpr = Expression.Parameter(typeof(Assembly), "a");

            Expression typesExpr;

            var definedTypeInfosProperty = typeof(Assembly).GetPropertyOrNull("DefinedTypes");
            if (definedTypeInfosProperty == null)
            {
                typesExpr = Expression.Call(assemblyParamExpr, "GetTypes", ArrayTools.Empty<Type>(),
                    ArrayTools.Empty<Expression>());
            }
            else
            {
                typesExpr = Expression.Property(assemblyParamExpr, definedTypeInfosProperty);
                if (typesExpr.Type == typeof(IEnumerable<TypeInfo>))
                {
                    var typeInfoParamExpr = Expression.Parameter(typeof(TypeInfo), "typeInfo");
                    typesExpr = Expression.Call(typeof(Enumerable),
                        "Select", new[] { typeof(TypeInfo), typeof(Type) },
                        typesExpr,
                        Expression.Lambda<Func<TypeInfo, Type>>(
                            Expression.Call(typeInfoParamExpr, "AsType",
                                ArrayTools.Empty<Type>(), ArrayTools.Empty<Expression>()),
                            typeInfoParamExpr));
                }
            }

            var resultFunc = Expression.Lambda<Func<Assembly, IEnumerable<Type>>>(typesExpr, assemblyParamExpr);
            return resultFunc.Compile();
        }

        /// <summary>Portable version of PropertyInfo.GetGetMethod.</summary>
        /// <param name="p">Target property info</param>
        /// <param name="includeNonPublic">(optional) If set then consider non-public getter</param>
        /// <returns>Setter method info if it is defined for property.</returns>
        public static MethodInfo GetGetMethodOrNull(this PropertyInfo p, bool includeNonPublic = false)
        {
            return p.DeclaringType.GetSingleMethodOrNull("get_" + p.Name, includeNonPublic);
        }

        /// <summary>Portable version of PropertyInfo.GetSetMethod.</summary>
        /// <param name="p">Target property info</param>
        /// <param name="includeNonPublic">(optional) If set then consider non-public setter</param>
        /// <returns>Setter method info if it is defined for property.</returns>
        public static MethodInfo GetSetMethodOrNull(this PropertyInfo p, bool includeNonPublic = false)
        {
            return p.DeclaringType.GetSingleMethodOrNull("set_" + p.Name, includeNonPublic);
        }

        private static readonly Lazy<Func<int>> _getEnvCurrentManagedThreadId = new Lazy<Func<int>>(() =>
        {
            var method = typeof(Environment).GetMethodOrNull("get_CurrentManagedThreadId", ArrayTools.Empty<Type>());
            if (method == null)
                return null;

            return Expression.Lambda<Func<int>>(
                Expression.Call(method, ArrayTools.Empty<Expression>()),
                ArrayTools.Empty<ParameterExpression>()).Compile();
        });

        /// <summary>Returns managed Thread ID either from Environment or Thread.CurrentThread whichever is available.</summary>
        /// <returns>Managed Thread ID.</returns>
        public static int GetCurrentManagedThreadID()
        {
            var resultID = -1;
            GetCurrentManagedThreadID(ref resultID);
            if (resultID == -1)
                resultID = _getEnvCurrentManagedThreadId.Value();
            return resultID;
        }

        static partial void GetCurrentManagedThreadID(ref int threadID);
    }
}

namespace FastExpressionCompiler
{
    using System;
    using System.Linq.Expressions;
    using System.Collections.Generic;

    /// <summary>Compiles to delegate using FastExpressionCompiler.</summary>
    internal static partial class ExpressionCompiler
    {
        internal static TDelegate DoCompile<TDelegate>(Expression bodyExpr,
            ParameterExpression[] paramExprs, Type[] paramTypes, Type returnType) where TDelegate : class
        {
            TDelegate compiledDelegate = null;
            TryCompile(ref compiledDelegate, bodyExpr, paramExprs, paramTypes, returnType);
            return compiledDelegate;
        }

        static partial void TryCompile<TDelegate>(
            ref TDelegate compileDelegate,
            Expression bodyExpr,
            IList<ParameterExpression> paramExprs,
            Type[] paramTypes,
            Type returnType) where TDelegate : class;
    }
}

namespace DryIoc.Experimental
{
    using System;
    using System.Reflection;
    using ImTools;

    /// <summary>Succinct convention-based, LINQ like API to resolve resolution root at the end.</summary>
    internal static class DI
    {
        /// <summary>Pre-configured auto-magic rules.</summary>
        public static readonly Rules Relaxed = Rules.Default
            .With(FactoryMethod.ConstructorWithResolvableArguments)
            .WithFactorySelector(Rules.SelectLastRegisteredFactory())
            .WithTrackingDisposableTransients()
            .WithConcreteTypeDynamicRegistrations();

        /// <summary>Creates new default configured container</summary>
        /// <param name="configure">(optional) Additional rules.</param>
        /// <returns>New configured container.</returns>
        public static IContainer New(Func<Rules, Rules> configure = null)
        {
            var rules = configure == null ? Relaxed : configure(Relaxed);
            return new Container(rules);
        }

        /// <summary>Auto-wired resolution of T from the container.</summary>
        /// <typeparam name="T">Type of service to resolve.</typeparam>
        /// <param name="assemblies">(optional) Assemblies to look for services implementations.</param>
        /// <returns>Resolved service or throws.</returns>
        public static T Get<T>(params Assembly[] assemblies)
        {
            return New().Get<T>(assemblies);
        }

        /// <summary>Auto-wired resolution of T from the container.</summary>
        /// <typeparam name="T">Type of service to resolve.</typeparam>
        /// <param name="container">(optional) Container to resolve from.</param>
        /// <param name="assemblies">(optional) Assemblies to look for service implementation and dependencies.</param>
        /// <returns>Resolved service or throws.</returns>
        public static T Get<T>(this IContainer container, params Assembly[] assemblies)
        {
            if (assemblies.IsNullOrEmpty())
                assemblies = new[] { typeof(T).GetAssembly() };
            return container.WithAutoFallbackDynamicRegistrations(assemblies).Resolve<T>();
        }
    }
}
