
namespace NServiceBus.Saga
{
    /// <summary>
    /// Implement this interface if you want to write a saga with minimal infrastructure support.
    /// It is recommended you inherit the abstract class <see cref="Saga{T}"/> to get the most functionality.
    /// </summary>
    public interface ISaga : ITimeoutable, HasCompleted
    {
        /// <summary>
        /// The saga's data.
        /// </summary>
        ISagaEntity Entity { get; set; }

        /// <summary>
        /// Used for retrieving the endpoint which caused the saga to be initiated.
        /// </summary>
        IBus Bus { get; set; }
    }

    /// <summary>
    /// A more strongly typed version of ISaga meant to be implemented by application developers
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface ISaga<T> : ISaga where T : ISagaEntity
    {
        /// <summary>
        /// The saga's data.
        /// </summary>
        T Data { get; set; }
    }
}
