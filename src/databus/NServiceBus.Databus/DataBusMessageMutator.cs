namespace NServiceBus.DataBus
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Transactions;
    using MessageMutator;

    public class DataBusMessageMutator : IMessageMutator
    {
        const string DATABUS_PREFIX = "NServiceBus.DataBus.";
        readonly IDataBus dataBus;
        readonly IDataBusSerializer serializer;
        
        public DataBusMessageMutator(IDataBus dataBus, IDataBusSerializer serializer)
        {
            this.dataBus = dataBus;
            this.serializer = serializer;
        }

        public IBus Bus { get; set; }

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
                        message.SetHeader(DATABUS_PREFIX + headerKey, headerValue);
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

                    var dataBusKey = message.GetHeader(DATABUS_PREFIX + headerKey);

                    if(String.IsNullOrEmpty(dataBusKey))
                    {
                        ThrowError(message, property);
                    }

                    object value = null;

                    try
                    {
                        using (var stream = dataBus.Get(dataBusKey))
                        {
                            value = serializer.Deserialize(stream);
                        }  
                    }
                    catch(Exception ex)
                    {
                        ThrowError(message, property, ex);
                    }

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

            return message;
        }

        private void ThrowError(object message, PropertyInfo property, Exception exception = null)
        {
            var errorMessage =
                String.Format(
                    "Databus can't retrieve the data for message with id {0} in '{1}.{2}' property. Please ensure that you have configured the Databus correctly on the sender side.",
                    Bus.CurrentMessageContext.Id, message.GetType().FullName, property.Name);

            throw new InvalidDataException(errorMessage, exception);
        }

        static List<PropertyInfo> GetDataBusProperties(object message)
        {
            var messageType = message.GetType();

            if (!cache.ContainsKey(messageType))
                cache[messageType] = messageType.GetProperties()
                    .Where(property => property.IsDataBusProperty())
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