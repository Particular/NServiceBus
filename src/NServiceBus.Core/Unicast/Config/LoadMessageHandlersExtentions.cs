namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Configuration;
    using System.Linq;
    using Logging;
    using Unicast;


    public static class LoadMessageHandlersExtentions
    {
        /// <summary>
        ///     Loads all message handler assemblies in the runtime directory.
        /// </summary>
        /// <returns></returns>
        public static Configure LoadMessageHandlers(this Configure config)
        {
            var types = new List<Type>();

            config.Settings.GetAvailableTypes().Where(TypeSpecifiesMessageHandlerOrdering)
                .ToList().ForEach(t =>
                {
                    Logger.DebugFormat("Going to ask for message handler ordering from {0}.", t);

                    var order = new Order();
                    ((ISpecifyMessageHandlerOrdering)Activator.CreateInstance(t)).SpecifyOrder(order);

                    order.Types.ToList().ForEach(ht =>
                    {
                        if (types.Contains(ht))
                        {
                            throw new ConfigurationErrorsException(string.Format("The order in which the type {0} should be invoked was already specified by a previous implementor of ISpecifyMessageHandlerOrdering. Check the debug logs to see which other specifiers have been invoked.", ht));
                        }
                    });

                    types.AddRange(order.Types);
                });

            return config.LoadMessageHandlers(types);
        }

        /// <summary>
        ///     Loads all message handler assemblies in the runtime directory
        ///     and specifies that handlers in the given assembly should run
        ///     before all others.
        ///     Use First{T} to indicate the type to load from.
        /// </summary>
        /// <typeparam name="TFirst"></typeparam>
        /// <returns></returns>
        public static Configure LoadMessageHandlers<TFirst>(this Configure config)
        {
            var args = typeof(TFirst).GetGenericArguments();
            if (args.Length == 1)
            {
                if (typeof(First<>).MakeGenericType(args[0]).IsAssignableFrom(typeof(TFirst)))
                {
                    return config.LoadMessageHandlers(new[]
                    {
                        args[0]
                    });
                }
            }

            throw new ArgumentException("TFirst should be of the type First<T> where T is the type to indicate as first.");
        }

        /// <summary>
        ///     Loads all message handler assemblies in the runtime directory
        ///     and specifies that the handlers in the given 'order' are to
        ///     run before all others and in the order specified.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="config"></param>
        /// <param name="order"></param>
        /// <returns></returns>
        public static Configure LoadMessageHandlers<T>(this Configure config, First<T> order)
        {
            return config.LoadMessageHandlers(order.Types);
        }

        static Configure LoadMessageHandlers(this Configure config,IEnumerable<Type> orderedTypes)
        {
            var types = new List<Type>(config.Settings.GetAvailableTypes());

            foreach (var t in orderedTypes)
            {
                types.Remove(t);
            }

            types.InsertRange(0, orderedTypes);

            return config.ConfigureMessageHandlersIn(types);
        }

        /// <summary>
        ///     Scans the given types for types that are message handlers
        ///     then uses the Configurer to configure them into the container as single call components,
        ///     finally passing them to the bus as its MessageHandlerTypes.
        /// </summary>
        /// <param name="config"></param>
        /// <param name="types"></param>
        /// <returns></returns>
        static Configure ConfigureMessageHandlersIn(this Configure config, IEnumerable<Type> types)
        {
            var handlerRegistry = new MessageHandlerRegistry();
            var handlers = new List<Type>();

            foreach (var t in types.Where(IsMessageHandler))
            {
                config.Configurer.ConfigureComponent(t, DependencyLifecycle.InstancePerUnitOfWork);
                handlerRegistry.RegisterHandler(t);
                handlers.Add(t);
            }

            config.Configurer.RegisterSingleton<IMessageHandlerRegistry>(handlerRegistry);

            return config;
        }

        public static bool IsMessageHandler(Type type)
        {
            if (type.IsAbstract || type.IsGenericTypeDefinition)
            {
                return false;
            }

            return type.GetInterfaces()
                .Select(GetMessageTypeFromMessageHandler)
                .Any(messageType => messageType != null);
        }

        static bool TypeSpecifiesMessageHandlerOrdering(Type t)
        {
            return typeof(ISpecifyMessageHandlerOrdering).IsAssignableFrom(t) && !t.IsAbstract && !t.IsInterface;
        }

        static Type GetMessageTypeFromMessageHandler(Type t)
        {
            if (t.IsGenericType)
            {
                var args = t.GetGenericArguments();
                if (args.Length != 1)
                {
                    return null;
                }

                var handlerType = typeof(IHandleMessages<>).MakeGenericType(args[0]);
                if (handlerType.IsAssignableFrom(t))
                {
                    return args[0];
                }
            }

            return null;
        }

        class LoadHandlersByDefault:IWantToRunBeforeConfigurationIsFinalized
        {
            public void Run(Configure config)
            {
                if (!config.Configurer.HasComponent<IMessageHandlerRegistry>())
                {
                    config.LoadMessageHandlers();
                }
            }
        }


        static ILog Logger = LogManager.GetLogger<Configure>();
    }
}