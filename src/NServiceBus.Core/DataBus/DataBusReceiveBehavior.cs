namespace NServiceBus.DataBus
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Linq;
    using System.Reflection;
    using System.Transactions;
    using Gateway.HeaderManagement;
    using Pipeline;
    using Pipeline.Contexts;

    /// <summary>
    /// Not for public consumption. May change in minor version releases.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public class DataBusReceiveBehavior : IBehavior<ReceiveLogicalMessageContext>
    {
        IDataBus dataBus;
        IDataBusSerializer dataBusSerializer;

        public DataBusReceiveBehavior(IDataBusSerializer dataBusSerializer, IDataBus dataBus)
        {
            this.dataBusSerializer = dataBusSerializer;
            this.dataBus = dataBus;
        }

        public void Invoke(ReceiveLogicalMessageContext context, Action next)
        {
            var message = context.LogicalMessage.Instance;

            foreach (var property in GetDataBusProperties(message))
            {
                var propertyValue = property.GetValue(message, null);

                var dataBusProperty = propertyValue as IDataBusProperty;
                string headerKey;

                if (dataBusProperty != null)
                {
                    headerKey = dataBusProperty.Key;
                }
                else
                {
                    headerKey = String.Format("{0}.{1}", message.GetType().FullName, property.Name);
                }

                string dataBusKey;

                if (!context.LogicalMessage.Headers.TryGetValue(HeaderMapper.DATABUS_PREFIX + headerKey, out dataBusKey))
                {
                    continue;
                }

                using (new TransactionScope(TransactionScopeOption.Suppress))
                using (var stream = dataBus.Get(dataBusKey))
                {
                    var value = dataBusSerializer.Deserialize(stream);

                    if (dataBusProperty != null)
                    {
                        dataBusProperty.SetValue(value);
                    }
                    else
                    {
                        property.SetValue(message, value, null);
                    }
                }
            }

            next();
        }

        static IEnumerable<PropertyInfo> GetDataBusProperties(object message)
        {
            var messageType = message.GetType();


            List<PropertyInfo> value;

            if (!cache.TryGetValue(messageType, out value))
            {
                value = messageType.GetProperties()
                    .Where(MessageConventionExtensions.IsDataBusProperty)
                    .ToList();

                cache[messageType] = value;
            }


            return value;
        }

        readonly static ConcurrentDictionary<Type, List<PropertyInfo>> cache = new ConcurrentDictionary<Type, List<PropertyInfo>>();

    }
}