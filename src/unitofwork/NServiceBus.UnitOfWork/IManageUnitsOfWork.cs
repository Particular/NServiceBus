namespace NServiceBus.UnitOfWork
{
    /// <summary>
    /// Interface used by NServiceBus to manage units of work as a part of the
    /// message processing pipeline.
    /// </summary>
    public interface IManageUnitsOfWork
    {
        /// <summary>
        /// Called before all message handlers and modules
        /// </summary>
        void Begin();

        /// <summary>
        /// Called after all message handlers and modules
        /// </summary>
        void End();

        /// <summary>
        /// Called in the case there was an exception during the message processing
        /// </summary>
        void Error();
    }
}
