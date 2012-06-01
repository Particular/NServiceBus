namespace NServiceBus.Unicast
{
    using System;

    /// <summary>
    /// Implement this interface if you want to be called when the bus starts up
    /// </summary>
    [Obsolete("This interface is obsolete, it has been moved to NServiceBus.IWantToRunWhenTheBusStarts, NServiceBus.", false)]
    public interface IWantToRunWhenTheBusStarts
    {
        /// <summary>
        /// Method called on start up
        /// </summary>
        void Run();
    }
}