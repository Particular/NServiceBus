#nullable enable

namespace NServiceBus.Sagas;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

/// <summary>
/// Sagas metamodel.
/// </summary>
public partial class SagaMetadataCollection : IEnumerable<SagaMetadata>
{
    /// <summary>
    /// Returns an enumerator that iterates through the collection.
    /// </summary>
    public IEnumerator<SagaMetadata> GetEnumerator() => byEntity.Values.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    /// <summary>
    /// Indicates whether any saga metadata is present in the collection.
    /// </summary>
    public bool HasMetadata => byEntity.Count > 0;

    /// <summary>
    /// Populates the model with saga metadata from the provided collection of types.
    /// </summary>
    /// <param name="availableTypes">A collection of types to scan for sagas.</param>
    public void Initialize(IEnumerable<Type> availableTypes)
    {
        ArgumentNullException.ThrowIfNull(availableTypes);

        var availableTypesList = availableTypes.ToList();

        var foundSagas = availableTypesList.Where(SagaMetadata.IsSagaType)
            .Select(SagaMetadata.Create)
            .ToList();

        foreach (var saga in foundSagas)
        {
            byEntity[saga.SagaEntityType] = saga;
            byType[saga.SagaType] = saga;
        }
    }

    /// <summary>
    /// Returns a <see cref="SagaMetadata" /> for an entity by entity name.
    /// </summary>
    /// <param name="entityType">Type of the entity (saga data).</param>
    /// <returns>An instance of <see cref="SagaMetadata" />.</returns>
    public SagaMetadata FindByEntity(Type entityType)
    {
        ArgumentNullException.ThrowIfNull(entityType);
        return byEntity[entityType];
    }

    /// <summary>
    /// Returns a <see cref="SagaMetadata" /> for an entity by name.
    /// </summary>
    /// <param name="sagaType">Saga type.</param>
    /// <returns>An instance of <see cref="SagaMetadata" />.</returns>
    public SagaMetadata Find(Type sagaType)
    {
        ArgumentNullException.ThrowIfNull(sagaType);
        return byType[sagaType];
    }

    internal bool TryFind(Type sagaType, [NotNullWhen(true)] out SagaMetadata? sagaMetadata) => byType.TryGetValue(sagaType, out sagaMetadata);

    /// <summary>
    /// Registers a saga metadata instance explicitly.
    /// </summary>
    /// <param name="metadata">The saga metadata to register.</param>
    public void Register(SagaMetadata metadata)
    {
        ArgumentNullException.ThrowIfNull(metadata);

        if (byType.ContainsKey(metadata.SagaType))
        {
            throw new Exception($"Saga type '{metadata.SagaType.FullName}' is already registered.");
        }

        if (byEntity.ContainsKey(metadata.SagaEntityType))
        {
            throw new Exception($"Saga entity type '{metadata.SagaEntityType.FullName}' is already registered.");
        }

        byEntity[metadata.SagaEntityType] = metadata;
        byType[metadata.SagaType] = metadata;
    }

    internal void VerifyIfEntitiesAreShared()
    {
        var violations = new List<string>();

        foreach (var saga in byType.Values)
        {
            foreach (var entityItem in byEntity)
            {
                if (entityItem.Value.SagaType == saga.SagaType)
                {
                    continue;
                }

                var entityItemTypeIsAssignableBySagaEntityType = entityItem.Key.IsAssignableFrom(saga.SagaEntityType);
                var sagaEntityTypeIsAssignableByEntityItemType = saga.SagaEntityType.IsAssignableFrom(entityItem.Key);

                if (entityItemTypeIsAssignableBySagaEntityType || sagaEntityTypeIsAssignableByEntityItemType)
                {
                    violations.Add($"Entity '{saga.SagaEntityType}' used by saga types '{saga.SagaType}' and '{entityItem.Value.SagaType}'.");
                }
            }
        }

        if (violations.Count != 0)
        {
            throw new Exception("Best practice violation: Multiple saga types are sharing the same saga state which can result in persisters to physically share the same storage structure.\n\n- " + string.Join("\n- ", violations));
        }
    }

    readonly Dictionary<Type, SagaMetadata> byEntity = [];
    readonly Dictionary<Type, SagaMetadata> byType = [];
}