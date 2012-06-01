namespace NServiceBus
{
    /// <summary>
    /// Implement this interface if you want to be called when the bus starts up
    /// </summary>
    public interface IWantToRunWhenTheBusStarts
    {
        /// <summary>
        /// Method called on start up
        /// </summary>
        void Run();
    }
}