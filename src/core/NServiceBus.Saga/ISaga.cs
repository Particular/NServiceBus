
namespace NServiceBus.Saga
{
    /// <summary>
    /// This interface is used by <see cref="Configure"/> to identify sagas.
    /// </summary>
    public interface ISaga : ITimeoutable, HasCompleted
    {
        ISagaEntity Entity { get; set; }

        IBus Bus { get; set; }
    }

    public interface ISaga<T> : ISaga where T : ISagaEntity
    {
        T Data { get; set; }
    }
}
