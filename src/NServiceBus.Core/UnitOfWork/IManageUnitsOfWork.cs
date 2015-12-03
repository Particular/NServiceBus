namespace NServiceBus.UnitOfWork
{
    using System;
    using System.Threading.Tasks;

    /// <summary>
    /// Interface used by NServiceBus to manage units of work as a part of the
    /// message processing pipeline.
    /// </summary>
    public interface IManageUnitsOfWork
    {
        /// <summary>
        /// Called before all message handlers and modules.
        /// </summary>
        Task Begin();

        /// <summary>
        /// Called after all message handlers and modules, if an error has occurred the exception will be passed.
        /// </summary>
        Task End(Exception ex = null);
    }
}
