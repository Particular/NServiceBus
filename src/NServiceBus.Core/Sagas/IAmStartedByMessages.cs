namespace NServiceBus
{
    using Sagas;

    /// <summary>
    /// Use this interface to signify that when a message of the given type is
    /// received, if a saga cannot be found by an <see cref="ISagaFinder{TSagaData, TMessage}" />
    /// the saga will be created.
    /// </summary>
    public interface IAmStartedByMessages<T> : IHandleMessages<T>
    {
    }
}