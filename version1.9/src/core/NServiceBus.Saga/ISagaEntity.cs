using System;

namespace NServiceBus.Saga
{
    /// <summary>
    /// Defines the basic data used by long-running processes.
    /// </summary>
    public interface ISagaEntity
    {
        /// <summary>
        /// Gets/sets the Id of the process. Do NOT generate this value in your code.
        /// The value of the Id will be generated automatically to provide the
        /// best performance for saving in a database.
        /// </summary>
        /// <remarks>
        /// The reason Guid is used for process Id is that messages containing this Id need
        /// to be sent by the process even before it is persisted.
        /// </remarks>
        Guid Id { get; set; }

        /// <summary>
        /// Contains the return address of the endpoint that caused the process to run.
        /// </summary>
        string Originator { get; set; }
    }
}
