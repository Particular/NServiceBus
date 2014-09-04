#pragma warning disable 1591
// ReSharper disable UnusedParameter.Global
namespace NServiceBus
{
    using System;

    public static partial class LoadMessageHandlersExtentions
    {
        [ObsoleteEx(Replacement = "It is safe to remove this method call. This is the default behavior.", RemoveInVersion = "6.0", TreatAsErrorFromVersion = "5.0")]
        public static Configure LoadMessageHandlers(this Configure config)
        {
            throw new InvalidOperationException();
        }

        [ObsoleteEx(Replacement = "Use configuration.LoadMessageHandlers<TFirst>, where configuration is an instance of type BusConfiguration)", RemoveInVersion = "6.0", TreatAsErrorFromVersion = "5.0")]
        public static Configure LoadMessageHandlers<TFirst>(this Configure config)
        {
            throw new InvalidOperationException();
        }

        [ObsoleteEx(Replacement = "Use configuration.LoadMessageHandlers<T>, where configuration is an instance of type BusConfiguration)", RemoveInVersion = "6.0", TreatAsErrorFromVersion = "5.0")]
        public static Configure LoadMessageHandlers<T>(this Configure config, First<T> order)
        {
            throw new InvalidOperationException();
        }
    }
}