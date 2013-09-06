namespace NServiceBus.Sagas.Finders
{
    using System;
    using System.Reflection;
    using Saga;

    /// <summary>
    /// Finds the given type of saga by looking it up based on the given property.
    /// </summary>
    /// <typeparam name="TSaga"></typeparam>
    /// <typeparam name="TMessage"></typeparam>
    public class PropertySagaFinder<TSaga, TMessage> : IFindSagas<TSaga>.Using<TMessage>
        where TSaga : IContainSagaData
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
        /// <param name="message"></param>
        /// <returns></returns>
        public TSaga FindBy(TMessage message)
        {
            if (SagaPersister == null)
                throw new InvalidOperationException(
                    "No saga persister configured. Please configure a saga persister if you want to use the nservicebus saga support");
            
            var propertyValue = MessageProperty.GetValue(message, null);
            
            if(SagaProperty.Name.ToLower() == "id")
                return SagaPersister.Get<TSaga>((Guid)propertyValue);

            return SagaPersister.Get<TSaga>(SagaProperty.Name, propertyValue);
        }
    }
}
