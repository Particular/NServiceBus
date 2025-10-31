#nullable enable
namespace NServiceBus;

using Sagas;

/// <summary>
/// Implementation provided by the infrastructure - don't implement this
/// or register implementations of it in the container unless you intend
/// to substantially change the way sagas work.
/// </summary>
public interface IConfigureHowToFindSagaWithFinder
{
    /// <summary>
    /// Specify that when the infrastructure is handling a message
    /// of the given type, which message header should be matched to
    /// which saga entity property in the persistent saga store.
    /// </summary>
    void ConfigureMapping<TSagaEntity, TMessage, TFinder>() where TFinder : ISagaFinder<TSagaEntity, TMessage> where TSagaEntity : IContainSagaData;
}