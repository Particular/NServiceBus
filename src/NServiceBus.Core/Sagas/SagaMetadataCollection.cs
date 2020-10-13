namespace NServiceBus.Sagas
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using Logging;

    /// <summary>
    /// Sagas metamodel.
    /// </summary>
    public class SagaMetadataCollection : IEnumerable<SagaMetadata>
    {
        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        public IEnumerator<SagaMetadata> GetEnumerator()
        {
            return byEntity.Values.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <summary>
        /// Populates the model with saga metadata from the provided collection of types.
        /// </summary>
        /// <param name="availableTypes">A collection of types to scan for sagas.</param>
        public void Initialize(IEnumerable<Type> availableTypes)
        {
            Initialize(availableTypes, new Conventions());
        }

        /// <summary>
        /// Populates the model with saga metadata from the provided collection of types.
        /// </summary>
        /// <param name="availableTypes">A collection of types to scan for sagas.</param>
        /// <param name="conventions">Custom conventions to be used while scanning types.</param>
        public void Initialize(IEnumerable<Type> availableTypes, Conventions conventions)
        {
            Guard.AgainstNull(nameof(availableTypes), availableTypes);
            Guard.AgainstNull(nameof(conventions), conventions);

            var availableTypesList = availableTypes.ToList();

            var foundSagas = availableTypesList.Where(SagaMetadata.IsSagaType)
                .Select(t => SagaMetadata.Create(t, availableTypesList, conventions))
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
            Guard.AgainstNull(nameof(entityType), entityType);
            return byEntity[entityType];
        }

        /// <summary>
        /// Returns a <see cref="SagaMetadata" /> for an entity by name.
        /// </summary>
        /// <param name="sagaType">Saga type.</param>
        /// <returns>An instance of <see cref="SagaMetadata" />.</returns>
        public SagaMetadata Find(Type sagaType)
        {
            Guard.AgainstNull(nameof(sagaType), sagaType);
            return byType[sagaType];
        }

        internal bool TryFind(Type sagaType, out SagaMetadata targetSagaMetaData)
        {
            return byType.TryGetValue(sagaType, out targetSagaMetaData);
        }

        internal void VerifyIfEntitiesAreShared()
        {
            var violations = new List<string>();

            foreach (var saga in byType.Values)
            {
                foreach (var entityItem in byEntity)
                {
                    if (entityItem.Value.SagaType == saga.SagaType) continue;

                    var entityItemTypeIsAssignableBySagaEntityType = entityItem.Key.IsAssignableFrom(saga.SagaEntityType);
                    var sagaEntityTypeIsAssignableByEntityItemType = saga.SagaEntityType.IsAssignableFrom(entityItem.Key);

                    if (entityItemTypeIsAssignableBySagaEntityType || sagaEntityTypeIsAssignableByEntityItemType) violations.Add($"Entity '{saga.SagaEntityType}' used by saga types '{saga.SagaType}' and '{entityItem.Value.SagaType}'.");
                }
            }

            if (violations.Any()) throw new Exception("Best practice violation: Multiple saga types are sharing the same saga state which can result in persisters to physically share the same storage structure.\n\n- " + string.Join("\n- ", violations));
        }

        static readonly ILog Log = LogManager.GetLogger<SagaMetadataCollection>();
        Dictionary<Type, SagaMetadata> byEntity = new Dictionary<Type, SagaMetadata>();
        Dictionary<Type, SagaMetadata> byType = new Dictionary<Type, SagaMetadata>();
    }
}
