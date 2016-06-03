namespace NServiceBus
{
    using System;
    using System.IO;
    using System.Threading.Tasks;
    using System.Transactions;
    using DataBus;
    using DeliveryConstraints;
    using Performance.TimeToBeReceived;
    using Pipeline;

    class DataBusSendBehavior : Behavior<IOutgoingLogicalMessageContext>
    {
        public DataBusSendBehavior(IDataBus databus, IDataBusSerializer serializer, Conventions conventions)
        {
            this.conventions = conventions;
            dataBusSerializer = serializer;
            dataBus = databus;
        }

        public override async Task Invoke(IOutgoingLogicalMessageContext context, Func<Task> next)
        {
            var timeToBeReceived = TimeSpan.MaxValue;

            DiscardIfNotReceivedBefore constraint;

            if (context.Extensions.TryGetDeliveryConstraint(out constraint))
            {
                timeToBeReceived = constraint.MaxTime;
            }

            var message = context.Message.Instance;

            foreach (var property in conventions.GetDataBusProperties(message))
            {
                var propertyValue = property.Getter(message);

                if (propertyValue == null)
                {
                    continue;
                }

                using (var stream = new MemoryStream())
                {
                    var dataBusProperty = propertyValue as IDataBusProperty;

                    if (dataBusProperty != null)
                    {
                        propertyValue = dataBusProperty.GetValue();
                    }

                    dataBusSerializer.Serialize(propertyValue, stream);
                    stream.Position = 0;

                    string headerValue;

                    using (new TransactionScope(TransactionScopeOption.Suppress, TransactionScopeAsyncFlowOption.Enabled))
                    {
                        headerValue = await dataBus.Put(stream, timeToBeReceived).ConfigureAwait(false);
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
                        headerKey = $"{message.GetType().FullName}.{property.Name}";
                    }

                    //we use the headers to in order to allow the infrastructure (eg. the gateway) to modify the actual key
                    context.Headers["NServiceBus.DataBus." + headerKey] = headerValue;
                }
            }

            await next().ConfigureAwait(false);
        }

        Conventions conventions;
        IDataBus dataBus;
        IDataBusSerializer dataBusSerializer;

        public class Registration : RegisterStep
        {
            public Registration(Conventions conventions) : base("DataBusSend", typeof(DataBusSendBehavior), "Saves the payload into the shared location", b => new DataBusSendBehavior(b.Build<IDataBus>(), b.Build<IDataBusSerializer>(), conventions))
            {
                InsertAfter("MutateOutgoingMessages");
                InsertAfter("ApplyTimeToBeReceived");
            }
        }
    }
}