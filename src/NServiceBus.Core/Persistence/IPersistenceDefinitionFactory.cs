#nullable enable

namespace NServiceBus.Persistence;

/// <summary>
/// Defines a factory for creating persistence definitions used in <see cref="PersistenceConfig.UsePersistence{T}"/>.
/// </summary>
/// <typeparam name="TDefinition">The persistence definition type.</typeparam>
public interface IPersistenceDefinitionFactory<out TDefinition>
    where TDefinition : PersistenceDefinition, IPersistenceDefinitionFactory<TDefinition>
{
    /// <summary>
    /// Creates the persistence definition.
    /// </summary>
    /// <returns>The persistence definition.</returns>
    static abstract TDefinition Create();
}