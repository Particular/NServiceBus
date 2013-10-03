namespace NServiceBus.Pipeline
{
    /// <summary>
    /// This bad boy is passed down into the behavior chain
    /// </summary>
    public interface IBehaviorContext
    {
        T Get<T>();
        void Set<T>(T t);
        TransportMessage TransportMessage { get; }
    }
}