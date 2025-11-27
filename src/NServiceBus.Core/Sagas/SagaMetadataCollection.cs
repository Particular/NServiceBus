#nullable enable

namespace NServiceBus.Sagas;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

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
    /// Adds a range of saga metadata instances.
    /// </summary>
    /// <param name="metadata">The saga metadata instances to add.</param>
    public void AddRange(IEnumerable<SagaMetadata> metadata)
    {
        ArgumentNullException.ThrowIfNull(metadata);

        AssertNotLocked();

        foreach (var sagaMetadata in metadata)
        {
            Add(sagaMetadata);
        }
    }

    /// <summary>
    /// Adds a saga metadata instance explicitly.
    /// </summary>
    /// <param name="metadata">The saga metadata to add.</param>
    public void Add(SagaMetadata metadata)
    {
        ArgumentNullException.ThrowIfNull(metadata);

        AssertNotLocked();

        // Deduplicate additions since the saga metadata creation is assumed to be idempotent
        if (byType.ContainsKey(metadata.SagaType) && byEntity.ContainsKey(metadata.SagaEntityType))
        {
            return;
        }

        byEntity[metadata.SagaEntityType] = metadata;
        byType[metadata.SagaType] = metadata;
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

    internal void PreventChanges() => locked = true;

    internal bool TryFind(Type sagaType, [NotNullWhen(true)] out SagaMetadata? sagaMetadata) => byType.TryGetValue(sagaType, out sagaMetadata);

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

    void AssertNotLocked()
    {
        if (locked)
        {
            throw new InvalidOperationException("SagaMetadataCollection is locked and cannot be modified.");
        }
    }

    bool locked;
    readonly Dictionary<Type, SagaMetadata> byEntity = [];
    readonly Dictionary<Type, SagaMetadata> byType = [];
}