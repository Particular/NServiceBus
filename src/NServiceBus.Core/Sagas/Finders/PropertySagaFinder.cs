namespace NServiceBus.Sagas.Finders
{
    using System;
    using System.Reflection;
    using Saga;

    /// <summary>
    /// Finds the given type of saga by looking it up based on the given property.
    /// </summary>
    public class PropertySagaFinder<TSagaData, TMessage> : IFindSagas<TSagaData>.Using<TMessage>
        where TSagaData : IContainSagaData
    {
        /// <summary>
        /// Injected persister
        /// </summary>
        public ISagaPersister SagaPersister { get; set; }

        /// <summary>
        /// Property of the saga that will be used for lookup.
        /// </summary>
        public PropertyInfo SagaProperty { get; set; }
        
        /// <summary>
        /// Property of the message whose value will be used for the lookup.
        /// </summary>
        public PropertyInfo MessageProperty { get; set; }

        /// <summary>
        /// Uses the saga persister to find the saga.
        /// </summary>
        public TSagaData FindBy(TMessage message)
        {
            if (SagaPersister == null)
                throw new InvalidOperationException(
                    "No saga persister configured. Please configure a saga persister if you want to use the nservicebus saga support");
            
            var propertyValue = MessageProperty.GetValue(message, null);
            
            if(SagaProperty.Name.ToLower() == "id")
                return SagaPersister.Get<TSagaData>((Guid)propertyValue);

            return SagaPersister.Get<TSagaData>(SagaProperty.Name, propertyValue);
        }
    }
}
