namespace NServiceBus.Saga
{

    /// <summary>
    /// Use this interface to signify that when a message of the given type is
    /// received, if a saga cannot be found by an <see cref="IFindSagas{T}"/>
    /// the saga will be created.
    /// </summary>
    public interface IAmStartedByMessages<T> : IHandleMessages<T>
    {
    }

#pragma warning disable 1591

    // TODO: We need to discuss whether this makes sense. This is just a first shot.
    public interface IAmStartedByMessage<T> : IHandle<T>
    {
    }


    public interface IAmStartedByEvent<T> : ISubscribe<T>
    { }
#pragma warning restore 1591
}
