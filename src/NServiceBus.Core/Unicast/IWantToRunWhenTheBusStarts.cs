namespace NServiceBus.Unicast
{

    /// <summary>
    /// Implement this interface if you want to be called when the bus starts up
    /// </summary>
    [ObsoleteEx(Replacement = "NServiceBus!NServiceBus.IWantToRunWhenBusStartsAndStops", TreatAsErrorFromVersion = "4.0", RemoveInVersion = "5.0")]
    public interface IWantToRunWhenTheBusStarts
    {
        /// <summary>
        /// Method called on start up
        /// </summary>
        void Run();
    }
}
