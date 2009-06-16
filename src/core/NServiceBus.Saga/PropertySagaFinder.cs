using System.Reflection;

namespace NServiceBus.Saga
{
    /// <summary>
    /// Finds the given type of saga by looking it up based on the given property.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <typeparam name="M"></typeparam>
    public class PropertySagaFinder<T, M> : IFindSagas<T>.Using<M> where T : ISagaEntity where M : IMessage
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
        public T FindBy(M message)
        {
            return SagaPersister.Get<T>(SagaProperty.Name, MessageProperty.GetValue(message, null));
        }
    }
}
