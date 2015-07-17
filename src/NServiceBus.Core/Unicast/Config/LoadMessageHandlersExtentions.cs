namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using NServiceBus.Features;

    /// <summary>
    /// Provides configuration options to tune handler ordering
    /// </summary>
    public static class LoadMessageHandlersExtentions
    {
        /// <summary>
        ///     Loads all message handler assemblies in the runtime directory
        ///     and specifies that handlers in the given assembly should run
        ///     before all others.
        ///     Use First{T} to indicate the type to load from.
        /// </summary>
        /// <param name="config">The <see cref="BusConfiguration"/> instance to apply the settings to.</param>
        [ObsoleteEx(RemoveInVersion = "7.0", TreatAsErrorFromVersion = "6.0", ReplacementTypeOrMember = "ExecuteTheseHandlersFirst")]
        public static void LoadMessageHandlers<TFirst>(this BusConfiguration config)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        ///     Loads all message handler assemblies in the runtime directory
        ///     and specifies that the handlers in the given 'order' are to
        ///     run before all others and in the order specified.
        /// </summary>
        [ObsoleteEx(RemoveInVersion = "7.0", TreatAsErrorFromVersion = "6.0", ReplacementTypeOrMember = "ExecuteTheseHandlersFirst")]
        public static void LoadMessageHandlers<T>(this BusConfiguration config, First<T> order)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        ///     Loads all message handler assemblies in the runtime directory
        ///     and specifies that the handlers in the given 'order' are to
        ///     run before all others and in the order specified.
        /// </summary>
        /// <param name="config">The <see cref="BusConfiguration"/> instance to apply the settings to.</param>
        /// <param name="handlerTypes">The handler types to execute first.</param>
        public static void ExecuteTheseHandlersFirst(this BusConfiguration config, IEnumerable<Type> handlerTypes)
        {
            Guard.AgainstNull(config, "config");
            Guard.AgainstNull(handlerTypes, "handlerTypes");

            List<Type> list;
            if (!config.Settings.TryGet("NServiceBus.ExecuteTheseHandlersFirst", out list))
            {
                list = new List<Type>();
                config.Settings.Set("NServiceBus.ExecuteTheseHandlersFirst", list);
            }

            foreach (var handlerType in handlerTypes)
            {
                if (!RegisterHandlersInOrder.IsMessageHandler(handlerType))
                {
                    throw new ArgumentException(string.Format("'{0}' is not a handler type, please ensure that all types derive from IHandleMessages", handlerType));
                }

                if (list.Contains(handlerType))
                {
                    throw new ArgumentException(string.Format("The order in which the type '{0}' should be invoked was already specified by a previous call. You can only specify a handler type once.", handlerType));
                }

                list.Add(handlerType);
            }
        }

        /// <summary>
        ///     Loads all message handler assemblies in the runtime directory
        ///     and specifies that the handlers in the given 'order' are to
        ///     run before all others and in the order specified.
        /// </summary>
        /// <param name="config">The <see cref="BusConfiguration"/> instance to apply the settings to.</param>
        /// <param name="handlerTypes">The handler types to execute first.</param>
        public static void ExecuteTheseHandlersFirst(this BusConfiguration config, params Type[] handlerTypes)
        {
            Guard.AgainstNull(config, "config");
            Guard.AgainstNull(handlerTypes, "handlerTypes");

            config.ExecuteTheseHandlersFirst((IEnumerable<Type>)handlerTypes);
        }
    }
}
