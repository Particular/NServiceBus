namespace NServiceBus.DataBus
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Transactions;
    using Gateway.HeaderManagement;
    using Pipeline;
    using Pipeline.Contexts;

    class DataBusSendBehavior:IBehavior<SendLogicalMessageContext>
    {
        public IDataBus DataBus { get; set; }

        public IDataBusSerializer DataBusSerializer { get; set; }

         
        public void Invoke(SendLogicalMessageContext context, Action next)
        {
            var timeToBeReceived = context.MessageToSend.Metadata.TimeToBeReceived;

            var message = context.MessageToSend.Instance;

            using (new TransactionScope(TransactionScopeOption.Suppress))
            {
                foreach (var property in GetDataBusProperties(message))
                {
                    var propertyValue = property.GetValue(message, null);

                    if (propertyValue == null)
                        continue;

                    using (var stream = new MemoryStream())
                    {
                        var dataBusProperty = propertyValue as IDataBusProperty;

                        if (dataBusProperty != null)
                        {
                            propertyValue = dataBusProperty.GetValue();
                        }

                        DataBusSerializer.Serialize(propertyValue, stream);
                        stream.Position = 0;

                        var headerValue = DataBus.Put(stream, timeToBeReceived);
                        string headerKey;

                        if (dataBusProperty != null)
                        {
                            dataBusProperty.Key = headerValue;
                            //we use the headers to in order to allow the infrastructure (eg. the gateway) to modify the actual key
                            headerKey = headerValue;
                        }
                        else
                        {
                            property.SetValue(message, null, null);
                            headerKey = String.Format("{0}.{1}", message.GetType().FullName, property.Name);
                        }

                        //we use the headers to in order to allow the infrastructure (eg. the gateway) to modify the actual key
                        context.MessageToSend.Headers[HeaderMapper.DATABUS_PREFIX + headerKey] = headerValue;
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
