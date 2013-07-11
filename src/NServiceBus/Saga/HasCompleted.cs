
namespace NServiceBus.Saga
{
    /// <summary>
    /// Interface used to query a saga to see if it has completed.
    /// </summary>
    public interface HasCompleted
    {
        /// <summary>
        /// Indicates if the saga has completed.
        /// </summary>
        bool Completed { get; }
    }
}
