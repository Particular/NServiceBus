namespace NServiceBus
{
    using System;

    public static partial class LoadMessageHandlersExtentions
    {
        /// <summary>
        ///     Loads all message handler assemblies in the runtime directory.
        /// </summary>
// ReSharper disable UnusedParameter.Global
        [ObsoleteEx(Replacement = "It is safe to remove this method call. This is the default behavior.", RemoveInVersion = "6.0", TreatAsErrorFromVersion = "5.0")]
        public static Configure LoadMessageHandlers(this Configure config)
// ReSharper restore UnusedParameter.Global
        {
            throw new InvalidOperationException();
        }

        /// <summary>
        ///     Loads all message handler assemblies in the runtime directory
        ///     and specifies that handlers in the given assembly should run
        ///     before all others.
        ///     Use First{T} to indicate the type to load from.
        /// </summary>
// ReSharper disable UnusedParameter.Global
        [ObsoleteEx(Replacement = "Configure.With(c => c.LoadMessageHandlers<TFirst>)", RemoveInVersion = "6.0", TreatAsErrorFromVersion = "5.0")]
        public static Configure LoadMessageHandlers<TFirst>(this Configure config)
// ReSharper restore UnusedParameter.Global
        {
            throw new InvalidOperationException();
        }

        /// <summary>
        ///     Loads all message handler assemblies in the runtime directory
        ///     and specifies that the handlers in the given 'order' are to
        ///     run before all others and in the order specified.
        /// </summary>
// ReSharper disable UnusedParameter.Global
        [ObsoleteEx(Replacement = "Configure.With(c => c.LoadMessageHandlers<T>)", RemoveInVersion = "6.0", TreatAsErrorFromVersion = "5.0")]
        public static Configure LoadMessageHandlers<T>(this Configure config, First<T> order)
// ReSharper restore UnusedParameter.Global
        {
            throw new InvalidOperationException();
        }
    }
}