namespace NServiceBus.DataBus
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Transactions;
    using Gateway.HeaderManagement;
    using Pipeline;
    using Pipeline.Contexts;

    [Obsolete("This is a prototype API. May change in minor version releases.")]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public class DataBusSendBehavior : IBehavior<SendLogicalMessageContext>
    {
        public IDataBus DataBus { get; set; }

        public IDataBusSerializer DataBusSerializer { get; set; }


        public void Invoke(SendLogicalMessageContext context, Action next)
        {
            var timeToBeReceived = context.MessageToSend.Metadata.TimeToBeReceived;

            var message = context.MessageToSend.Instance;


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

                    string headerValue;

                    using (new TransactionScope(TransactionScopeOption.Suppress))
                    {
                        headerValue = DataBus.Put(stream, timeToBeReceived);
                    }

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
