
namespace NServiceBus.Saga
{
    /// <summary>
    /// Use this interface to signify that when a message of the given type is
    /// received, if a saga cannot be found by an <see cref="IFindSagas{T}"/>
    /// the saga will be created.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface ISagaStartedBy<T> : IMessageHandler<T> where T : IMessage
    {
    }

    /// <summary>
    /// Use this interface to signify that when a message of the given type is
    /// received, if a saga cannot be found by an <see cref="IFindSagas{T}"/>
    /// the saga will be created.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IAmStartedByMessages<T> : ISagaStartedBy<T> where T : IMessage {}
}
