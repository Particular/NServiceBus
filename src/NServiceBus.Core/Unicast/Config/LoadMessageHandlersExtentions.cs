namespace NServiceBus
{
    using System;

    /// <summary>
    /// Provides configuration options to tune handler ordering
    /// </summary>
    public static partial class LoadMessageHandlersExtentions
    {
        /// <summary>
        ///     Loads all message handler assemblies in the runtime directory
        ///     and specifies that handlers in the given assembly should run
        ///     before all others.
        ///     Use First{T} to indicate the type to load from.
        /// </summary>
        public static ConfigurationBuilder LoadMessageHandlers<TFirst>(this ConfigurationBuilder config)
        {
            var args = typeof(TFirst).GetGenericArguments();
            if (args.Length == 1)
            {
                if (typeof(First<>).MakeGenericType(args[0]).IsAssignableFrom(typeof(TFirst)))
                {
                    config.settings.Set("LoadMessageHandlers.Order.Types", new[] { args[0] });
                    return config;
                }
            }

            throw new ArgumentException("TFirst should be of the type First<T> where T is the type to indicate as first.");
        }

        /// <summary>
        ///     Loads all message handler assemblies in the runtime directory
        ///     and specifies that the handlers in the given 'order' are to
        ///     run before all others and in the order specified.
        /// </summary>
        public static ConfigurationBuilder LoadMessageHandlers<T>(this ConfigurationBuilder config, First<T> order)
        {
            config.settings.Set("LoadMessageHandlers.Order.Types", order.Types);
            return config;
        }
    }
}