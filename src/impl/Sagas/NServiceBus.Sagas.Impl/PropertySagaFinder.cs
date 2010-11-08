using System.Reflection;
using NServiceBus.Saga;

namespace NServiceBus.Sagas.Impl
{
    /// <summary>
    /// Finds the given type of saga by looking it up based on the given property.
    /// </summary>
    /// <typeparam name="TSaga"></typeparam>
    /// <typeparam name="TMessage"></typeparam>
    public class PropertySagaFinder<TSaga, TMessage> : IFindSagas<TSaga>.Using<TMessage>
        where TSaga : ISagaEntity
        where TMessage : IMessage
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
            return SagaPersister.Get<TSaga>(SagaProperty.Name, MessageProperty.GetValue(message, null));
        }
    }
}
