﻿namespace NServiceBus
{
    using System;
    using System.IO;
    using System.Threading.Tasks;
    using System.Transactions;
    using NServiceBus.DataBus;
    using NServiceBus.DeliveryConstraints;
    using NServiceBus.OutgoingPipeline;
    using NServiceBus.Performance.TimeToBeReceived;
    using NServiceBus.Pipeline;
    using NServiceBus.Pipeline.Contexts;
    using NServiceBus.TransportDispatch;

    class DataBusSendBehavior : Behavior<OutgoingContext>
    {
        public IDataBus DataBus { get; set; }

        public IDataBusSerializer DataBusSerializer { get; set; }

        public Conventions Conventions { get; set; }

        public override async Task Invoke(OutgoingContext context, Func<Task> next)
        {
            var timeToBeReceived = TimeSpan.MaxValue;

            DiscardIfNotReceivedBefore constraint;

            if (context.TryGetDeliveryConstraint(out constraint))
            {
                timeToBeReceived = constraint.MaxTime;
            }

            var message = context.GetMessageInstance();

            foreach (var property in Conventions.GetDataBusProperties(message))
            {
                var propertyValue = property.Getter(message);

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

                    using (new TransactionScope(TransactionScopeOption.Suppress, TransactionScopeAsyncFlowOption.Enabled))
                    {
                        headerValue = await DataBus.Put(stream, timeToBeReceived).ConfigureAwait(false);
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
                        property.Setter(message, null);
                        headerKey = String.Format("{0}.{1}", message.GetType().FullName, property.Name);
                    }

                    //we use the headers to in order to allow the infrastructure (eg. the gateway) to modify the actual key
                    context.SetHeader("NServiceBus.DataBus." + headerKey,headerValue);
                }
            }

            await next().ConfigureAwait(false);
        }

        public class Registration : RegisterStep
        {
            public Registration(): base("DataBusSend", typeof(DataBusSendBehavior), "Saves the payload into the shared location")
            {
                InsertAfter(WellKnownStep.MutateOutgoingMessages);
                InsertAfter("ApplyTimeToBeReceived");
            }
        }
    }
}
