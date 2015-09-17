namespace NServiceBus
{
    using System;

    /// <summary>
    /// Syntactic sugar for <see cref="IWantToRunWhenBusStartsAndStops"/>.
    /// </summary>
    public static class IWantToRunWhenBusStartsAndStopsExtensions_obsoletes
    {
        /// <summary>
        /// Method called at startup.
        /// </summary>
        [ObsoleteEx(
            TreatAsErrorFromVersion = "6",
            RemoveInVersion = "7",
            ReplacementTypeOrMember = "StartAsync()")]
        public static void Start(this IWantToRunWhenBusStartsAndStops runnable)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Method called on shutdown.
        /// </summary>
        [ObsoleteEx(
            TreatAsErrorFromVersion = "6",
            RemoveInVersion = "7",
            ReplacementTypeOrMember = "StopAsync()")]
        public static void Stop(this IWantToRunWhenBusStartsAndStops runnable)
        {
            throw new NotImplementedException();
        }
    }
}