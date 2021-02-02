namespace NServiceBus
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Provides configuration options to tune handler ordering.
    /// </summary>
    public static class LoadMessageHandlersExtensions
    {
        /// <summary>
        /// Loads all message handler assemblies in the runtime directory
        /// and specifies that the handlers in the given 'order' are to
        /// run before all others and in the order specified.
        /// </summary>
        /// <param name="config">The <see cref="EndpointConfiguration" /> instance to apply the settings to.</param>
        /// <param name="handlerTypes">The handler types to execute first.</param>
        public static void ExecuteTheseHandlersFirst(this EndpointConfiguration config, IEnumerable<Type> handlerTypes)
        {
            Guard.AgainstNull(nameof(config), config);
            Guard.AgainstNull(nameof(handlerTypes), handlerTypes);

            var list = config.Settings.Get<ReceiveComponent.Settings>().ExecuteTheseHandlersFirst;

            foreach (var handlerType in handlerTypes)
            {
                if (!ReceiveComponent.IsMessageHandler(handlerType))
                {
                    throw new ArgumentException($"'{handlerType}' is not a handler type, ensure that all types derive from IHandleMessages");
                }

                if (list.Contains(handlerType))
                {
                    throw new ArgumentException($"The order in which the type '{handlerType}' should be invoked was already specified by a previous call. A handler type can only specified once.");
                }

                list.Add(handlerType);
            }
        }

        /// <summary>
        /// Loads all message handler assemblies in the runtime directory
        /// and specifies that the handlers in the given 'order' are to
        /// run before all others and in the order specified.
        /// </summary>
        /// <param name="config">The <see cref="EndpointConfiguration" /> instance to apply the settings to.</param>
        /// <param name="handlerTypes">The handler types to execute first.</param>
        public static void ExecuteTheseHandlersFirst(this EndpointConfiguration config, params Type[] handlerTypes)
        {
            Guard.AgainstNull(nameof(config), config);
            Guard.AgainstNull(nameof(handlerTypes), handlerTypes);

            config.ExecuteTheseHandlersFirst((IEnumerable<Type>)handlerTypes);
        }
    }
}