namespace NServiceBus.Sagas
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;

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
            return byEntity[entityType];
        }

        /// <summary>
        /// Returns a <see cref="SagaMetadata" /> for an entity by name.
        /// </summary>
        /// <param name="sagaType">Saga type.</param>
        /// <returns>An instance of <see cref="SagaMetadata" />.</returns>
        public SagaMetadata Find(Type sagaType)
        {
            return byType[sagaType];
        }

        Dictionary<Type, SagaMetadata> byEntity = new Dictionary<Type, SagaMetadata>();
        Dictionary<Type, SagaMetadata> byType = new Dictionary<Type, SagaMetadata>();
    }
}