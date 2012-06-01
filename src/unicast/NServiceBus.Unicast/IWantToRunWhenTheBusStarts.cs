﻿namespace NServiceBus.Unicast
{

    /// <summary>
    /// Implement this interface if you want to be called when the bus starts up
    /// </summary>
    [ObsoleteEx(Replacement = "NServiceBus.IWantToRunWhenTheBusStarts, NServiceBus")]
    public interface IWantToRunWhenTheBusStarts
    {
        /// <summary>
        /// Method called on start up
        /// </summary>
        void Run();
    }
}