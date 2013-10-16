
namespace NServiceBus.Saga
{
    /// <summary>
    /// Use this interface to signify that when a message of the given type is
    /// received, if a saga cannot be found by an <see cref="IFindSagas{T}"/>
    /// the saga will be created.
    /// </summary>
    [ObsoleteEx(Replacement = "IAmStartedByMessages<T>", TreatAsErrorFromVersion = "4.0", RemoveInVersion = "5.0")]
    public interface ISagaStartedBy<T> : IAmStartedByMessages<T>
    {
    }

    /// <summary>
    /// Use this interface to signify that when a message of the given type is
    /// received, if a saga cannot be found by an <see cref="IFindSagas{T}"/>
    /// the saga will be created.
    /// </summary>
    public interface IAmStartedByMessages<T> : IHandleMessages<T>
    {
    }
}
