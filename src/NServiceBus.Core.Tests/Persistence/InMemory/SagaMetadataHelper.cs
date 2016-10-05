namespace NServiceBus.SagaPersisters.InMemory.Tests
{
    using Sagas;

    class SagaMetadataHelper
    {
        public static SagaCorrelationProperty GetMetadata<T>(IContainSagaData entity)
        {
            var metadata = SagaMetadata.Create(typeof(T));

            SagaMetadata.CorrelationPropertyMetadata correlatedProp;
            if (!metadata.TryGetCorrelationProperty(out correlatedProp))
            {
                return SagaCorrelationProperty.None;
            }
            var prop = entity.GetType().GetProperty(correlatedProp.Name);

            var value = prop.GetValue(entity);

            return new SagaCorrelationProperty(correlatedProp.Name, value);
        }
    }
}