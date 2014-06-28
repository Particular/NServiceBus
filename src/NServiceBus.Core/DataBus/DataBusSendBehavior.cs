namespace NServiceBus.DataBus
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Transactions;
    using Pipeline;
    using Pipeline.Contexts;
    using Unicast.Transport;

    class DataBusSendBehavior : IBehavior<OutgoingContext>
    {
        public IDataBus DataBus { get; set; }

        public IDataBusSerializer DataBusSerializer { get; set; }
        public Conventions Conventions { get; set; }

        public void Invoke(OutgoingContext context, Action next)
        {
            if (context.OutgoingLogicalMessage.IsControlMessage())
            {
                next();
                return;
            }

            var timeToBeReceived = context.OutgoingLogicalMessage.Metadata.TimeToBeReceived;
            var message = context.OutgoingLogicalMessage.Instance;

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
                    context.OutgoingLogicalMessage.Headers["NServiceBus.DataBus." + headerKey] = headerValue;
                }
            }

            next();
        }

        IEnumerable<PropertyInfo> GetDataBusProperties(object message)
        {
            var messageType = message.GetType();


            List<PropertyInfo> value;

            if (!cache.TryGetValue(messageType, out value))
            {
                value = messageType.GetProperties()
                    .Where(Conventions.IsDataBusProperty)
                    .ToList();

                cache[messageType] = value;
            }


            return value;
        }

        readonly static ConcurrentDictionary<Type, List<PropertyInfo>> cache = new ConcurrentDictionary<Type, List<PropertyInfo>>();

        public class Registration : RegisterBehavior
        {
            public Registration(): base("DataBusSend", typeof(DataBusSendBehavior), "Saves the payload into the shared location")
            {
                InsertAfter(WellKnownStep.MutateOutgoingMessages);
                InsertBefore(WellKnownStep.CreatePhysicalMessage);
            }
        }
    }
}
