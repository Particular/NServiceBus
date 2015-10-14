namespace NServiceBus.SagaPersisters.InMemory.Tests
{
    using NServiceBus.Sagas;

    class SagaMetadataHelper
    {
        public static SagaMetadata GetMetadata<T>()
        {
            return SagaMetadata.Create(typeof(T));
        }
    }
}