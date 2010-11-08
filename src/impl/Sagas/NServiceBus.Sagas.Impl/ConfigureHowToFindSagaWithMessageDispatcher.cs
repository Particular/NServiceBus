using System;
using System.Linq.Expressions;
using NServiceBus.Saga;
using NServiceBus.Utils.Reflection;

namespace NServiceBus.Sagas.Impl
{
    /// <summary>
    /// Class used to bridge the dependency between Saga{T} in NServiceBus.dll and
    /// the Configure class found in this project in NServiceBus.Core.dll.
    /// </summary>
    public class ConfigureHowToFindSagaWithMessageDispatcher : IConfigureHowToFindSagaWithMessage
    {
        void IConfigureHowToFindSagaWithMessage.ConfigureMapping<TSagaEntity, TMessage>(Expression<Func<TSagaEntity, object>> sagaEntityProperty, Expression<Func<TMessage, object>> messageProperty)
        {
            var sagaProp = Reflect<TSagaEntity>.GetProperty(sagaEntityProperty, true);
            var messageProp = Reflect<TMessage>.GetProperty(messageProperty, false);

            Configure.ConfigureHowToFindSagaWithMessage(typeof(TSagaEntity), sagaProp, typeof(TMessage), messageProp);
        }
    }
}