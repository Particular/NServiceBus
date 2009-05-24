using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

namespace NServiceBus.Saga
{
    public class PropertySagaFinder<T, M> : IFindSagas<T>.Using<M> where T : ISagaEntity where M : IMessage
    {
        public ISagaPersister SagaPersister { get; set; }

        public PropertyInfo SagaProperty { get; set; }
        public PropertyInfo MessageProperty { get; set; }

        public T FindBy(M message)
        {
            return SagaPersister.Get<T>(SagaProperty.Name, MessageProperty.GetValue(message, null));
        }
    }
}
