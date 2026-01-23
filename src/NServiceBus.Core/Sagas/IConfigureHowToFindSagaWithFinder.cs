#nullable enable
namespace NServiceBus;

using System.Diagnostics.CodeAnalysis;
using Sagas;

/// <summary>
/// Implementation provided by the infrastructure - don't implement this
/// unless you intend
/// to substantially change the way sagas work.
/// </summary>
public interface IConfigureHowToFindSagaWithFinder
{
    /// <summary>
    /// Specify the custom saga finder to match the given message to a saga instance.
    /// </summary>
    void ConfigureMapping<[DynamicallyAccessedMembers(DynamicMemberTypeAccess.SagaData)] TSagaEntity, TMessage, [DynamicallyAccessedMembers(DynamicMemberTypeAccess.SagaFinder)] TFinder>() where TFinder : class, ISagaFinder<TSagaEntity, TMessage> where TSagaEntity : class, IContainSagaData;
}