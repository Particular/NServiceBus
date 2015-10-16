namespace NServiceBus.SagaPersisters.InMemory.Tests
{
    using System.Collections.Generic;
    using NServiceBus.Sagas;

    class SagaMetadataHelper
    {
        public static IDictionary<string, object> GetMetadata<T>(IContainSagaData entity)
        {
            var metadata = SagaMetadata.Create(typeof(T));

            var result = new Dictionary<string, object>();

            foreach (var correlatedProp in metadata.CorrelationProperties)
            {
                var prop = entity.GetType().GetProperty(correlatedProp.Name);

                var value = prop.GetValue(entity);

                result[correlatedProp.Name] = value;
            }

            return result;
        }
    }
}