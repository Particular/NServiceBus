namespace NServiceBus.Saga
{
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Sagas metamodel.
    /// </summary>
    public class SagaMetaModel : IEnumerable<SagaMetadata>
    {
        /// <summary>
        /// Populates the model with saga metadata.
        /// </summary>
        /// <param name="foundSagas">Collection of Sagas metadata found.</param>
        public void Initialize(IEnumerable<SagaMetadata> foundSagas)
        {
            var sagas = foundSagas.ToList();

            foreach (var saga in sagas)
            {
                byEntityName[saga.EntityName] = saga;
            }

            foreach (var saga in sagas)
            {
                byName[saga.Name] = saga;
            }
        }

        /// <summary>
        /// Returns a <see cref="SagaMetadata"/> for an entity by entity name.
        /// </summary>
        /// <param name="name">Name of the entity.</param>
        /// <returns>An instance of <see cref="SagaMetadata"/></returns>
        public SagaMetadata FindByEntityName(string name)
        {
            return byEntityName[name];
        }

        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>
        /// A <see cref="T:System.Collections.Generic.IEnumerator`1"/> that can be used to iterate through the collection.
        /// </returns>
        /// <filterpriority>1</filterpriority>
        public IEnumerator<SagaMetadata> GetEnumerator()
        {
            return byEntityName.Values.GetEnumerator();
        }

        /// <summary>
        /// Returns a <see cref="SagaMetadata"/> for an entity by name.
        /// </summary>
        /// <param name="name">Saga name.</param>
        /// <returns>An instance of <see cref="SagaMetadata"/></returns>
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