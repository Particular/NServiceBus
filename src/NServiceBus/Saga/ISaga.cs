namespace NServiceBus.Saga
{

    /// <summary>
    /// Implement this interface if you want to write a saga with minimal infrastructure support.
    /// It is recommended you inherit the abstract class <see cref="Saga{T}"/> to get the most functionality.
    /// </summary>
    [ObsoleteEx(TreatAsErrorFromVersion = "5.0", RemoveInVersion = "5.0", Replacement = "Saga<T>")]
    public interface ISaga : ITimeoutable, HasCompleted
    {
        /// <summary>
        /// The saga's data.
        /// </summary>
        IContainSagaData Entity { get; set; }

        /// <summary>
        /// Used for retrieving the endpoint which caused the saga to be initiated.
        /// </summary>
        IBus Bus { get; set; }
    }

    /// <summary>
    /// A more strongly typed version of ISaga meant to be implemented by application developers
    /// </summary>
    [ObsoleteEx(TreatAsErrorFromVersion = "5.0", RemoveInVersion = "5.0", Replacement = "Saga<T>")]
    public interface ISaga<T> : ISaga where T : IContainSagaData
    {
        /// <summary>
        /// The saga's data.
        /// </summary>
        T Data { get; set; }
    }
}
