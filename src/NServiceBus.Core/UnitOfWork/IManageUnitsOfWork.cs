namespace NServiceBus.UnitOfWork
{
    using System;

    /// <summary>
    /// Interface used by NServiceBus to manage units of work as a part of the
    /// message processing pipeline.
    /// </summary>
    public interface IManageUnitsOfWork
    {
        /// <summary>
        /// Called before all message handlers and modules.
        /// </summary>
        void Begin();

        /// <summary>
        /// Called after all message handlers and modules, if an error has occurred the exception will be passed.
        /// </summary>
        void End(Exception ex = null);
    }
}
