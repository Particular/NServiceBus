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
    using MessageMutator;

    public class DataBusMessageMutator : IMessageMutator
    {
        readonly IDataBus dataBus;
        readonly IDataBusSerializer serializer;

        public DataBusMessageMutator(IDataBus dataBus, IDataBusSerializer serializer)
        {
            this.dataBus = dataBus;
            this.serializer = serializer;
        }
        
        object IMutateOutgoingMessages.MutateOutgoing(object message)
        {
            var timeToBeReceived = TimeToBeReceived(message);

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

                        serializer.Serialize(propertyValue, stream);
                        stream.Position = 0;

                        var headerValue = dataBus.Put(stream, timeToBeReceived);
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
                        Headers.SetMessageHeader(message, HeaderMapper.DATABUS_PREFIX + headerKey, headerValue);
                    }
                }
            }

            return message;
        }

        object IMutateIncomingMessages.MutateIncoming(object message)
        {
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

                    var dataBusKey = Headers.GetMessageHeader(message, HeaderMapper.DATABUS_PREFIX + headerKey);

                    if (string.IsNullOrEmpty(dataBusKey))
                        continue;

                    using (var stream = dataBus.Get(dataBusKey))
                    {
                        var value = serializer.Deserialize(stream);

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

            return message;
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

        static TimeSpan TimeToBeReceived(object message)
        {
            if (GetDataBusProperties(message).Count == 0)
            {
                return TimeSpan.MaxValue;
            }

            return MessageConventionExtensions.TimeToBeReceivedAction(message.GetType());
        }

        readonly static IDictionary<Type, List<PropertyInfo>> cache = new ConcurrentDictionary<Type, List<PropertyInfo>>(); 
    }
}