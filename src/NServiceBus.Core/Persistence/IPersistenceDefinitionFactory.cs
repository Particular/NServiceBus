#nullable enable

namespace NServiceBus.Persistence;

using Settings;

/// <summary>
///
/// </summary>
/// <typeparam name="TDefinition"></typeparam>
public interface IPersistenceDefinitionFactory<out TDefinition>
    where TDefinition : PersistenceDefinition, IPersistenceDefinitionFactory<TDefinition>
{
    /// <summary>
    /// Creates the persistence definition.
    /// </summary>
    /// <returns>The persistence definition.</returns>
    static abstract TDefinition Create(SettingsHolder settings);
}