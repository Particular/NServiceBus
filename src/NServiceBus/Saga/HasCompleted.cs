
namespace NServiceBus.Saga
{
    /// <summary>
    /// Interface used to query a saga to see if it has completed.
    /// </summary>
    [ObsoleteEx(TreatAsErrorFromVersion = "5.0", RemoveInVersion = "5.0", Replacement = "Saga<T>")]
    public interface HasCompleted
    {
        /// <summary>
        /// Indicates if the saga has completed.
        /// </summary>
        bool Completed { get; }
    }
}
