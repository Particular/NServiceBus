namespace NServiceBus.UnitOfWork
{
    using System;
    using System.Threading.Tasks;

    /// <summary>
    /// Interface used by NServiceBus to manage units of work as a part of the
    /// message processing pipeline.
    /// </summary>
    [ObsoleteEx(Message = "The unit of work pattern is more straightforward to implement in a pipeline behavior, where the using keyword and try/catch blocks can be used.", ReplacementTypeOrMember = "NServiceBus.Pipeline.Behavior<TContext>", TreatAsErrorFromVersion = "9", RemoveInVersion = "10")]
    public interface IManageUnitsOfWork
    {
        /// <summary>
        /// Called before all message handlers and modules.
        /// </summary>
        /// <exception cref="System.Exception">This exception will be thrown if <code>null</code> is returned. Return a Task or mark the method as <code>async</code>.</exception>
        Task Begin();

        /// <summary>
        /// Called after all message handlers and modules, if an error has occurred the exception will be passed.
        /// </summary>
        /// <exception cref="System.Exception">This exception will be thrown if <code>null</code> is returned. Return a Task or mark the method as <code>async</code>.</exception>
        Task End(Exception ex = null);
    }
}