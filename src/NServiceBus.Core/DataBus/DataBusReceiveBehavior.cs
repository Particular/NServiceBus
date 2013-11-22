namespace NServiceBus.DataBus
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Transactions;
    using Gateway.HeaderManagement;
    using Pipeline;
    using Pipeline.Contexts;

    class DataBusReceiveBehavior:IBehavior<ReceiveLogicalMessageContext>
    {
        public IDataBus DataBus { get; set; }

        public IDataBusSerializer DataBusSerializer { get; set; }


        public void Invoke(ReceiveLogicalMessageContext context, Action next)
        {
            var message = context.LogicalMessage.Instance;

            using (new TransactionScope(TransactionScopeOption.Suppress))
            {
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

                    if (!context.LogicalMessage.Headers.TryGetValue(HeaderMapper.DATABUS_PREFIX + headerKey, out dataBusKey)
                        || string.IsNullOrEmpty(dataBusKey))
                    {
                        continue;
                    }

                    using (var stream = DataBus.Get(dataBusKey))
                    {
                        var value = DataBusSerializer.Deserialize(stream);

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
            }

            next();
        }

        static List<PropertyInfo> GetDataBusProperties(object message)
        {
            var messageType = message.GetType();

            if (!cache.ContainsKey(messageType))
                cache[messageType] = messageType.GetProperties()
                    .Where(property => MessageConventionExtensions.IsDataBusProperty(property))
                    .ToList();

            return cache[messageType];
        }

        readonly static IDictionary<Type, List<PropertyInfo>> cache = new ConcurrentDictionary<Type, List<PropertyInfo>>(); 
    }
}