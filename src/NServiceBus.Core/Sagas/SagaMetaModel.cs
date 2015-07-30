namespace NServiceBus.Saga
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Sagas metamodel.
    /// </summary>
    public class SagaMetaModel : IEnumerable<SagaMetadata>
    {
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
            var foundSagas = TypeBasedSagaMetaModel.Create(availableTypes.ToList(), conventions).ToList();

            foreach (var saga in foundSagas)
            {
                byEntityName[saga.EntityName] = saga;
            }

            foreach (var saga in foundSagas)
            {
                byName[saga.Name] = saga;
            }
        }

        /// <summary>
        /// Returns a <see cref="SagaMetadata"/> for an entity by entity name.
        /// </summary>
        /// <param name="name">Name of the entity.</param>
        /// <returns>An instance of <see cref="SagaMetadata"/>.</returns>
        public SagaMetadata FindByEntityName(string name)
        {
            return byEntityName[name];
        }

        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        public IEnumerator<SagaMetadata> GetEnumerator()
        {
            return byEntityName.Values.GetEnumerator();
        }

        /// <summary>
        /// Returns a <see cref="SagaMetadata"/> for an entity by name.
        /// </summary>
        /// <param name="name">Saga name.</param>
        /// <returns>An instance of <see cref="SagaMetadata"/>.</returns>
        public SagaMetadata FindByName(string name)
        {
            return byName[name];
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        Dictionary<string,SagaMetadata> byEntityName = new Dictionary<string, SagaMetadata>();
        Dictionary<string, SagaMetadata> byName = new Dictionary<string, SagaMetadata>();
    }
}