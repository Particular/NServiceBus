namespace NServiceBus
{
    using System;
    using System.IO;
    using System.Threading.Tasks;
    using System.Transactions;
    using DataBus;
    using DeliveryConstraints;
    using Microsoft.Extensions.DependencyInjection;
    using Performance.TimeToBeReceived;
    using Pipeline;

    class DataBusSendBehavior : IBehavior<IOutgoingLogicalMessageContext, IOutgoingLogicalMessageContext>
    {
        public DataBusSendBehavior(IDataBus databus, IDataBusSerializer serializer, Conventions conventions)
        {
            this.conventions = conventions;
            dataBusSerializer = serializer;
            dataBus = databus;
        }

        public async Task Invoke(IOutgoingLogicalMessageContext context, Func<IOutgoingLogicalMessageContext, Task> next)
        {
            var timeToBeReceived = TimeSpan.MaxValue;

            if (context.Extensions.TryGetDeliveryConstraint(out DiscardIfNotReceivedBefore constraint))
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

            await next(context).ConfigureAwait(false);
        }

        readonly Conventions conventions;
        readonly IDataBus dataBus;
        readonly IDataBusSerializer dataBusSerializer;

        public class Registration : RegisterStep
        {
            public Registration(Conventions conventions) : base("DataBusSend", typeof(DataBusSendBehavior), "Saves the payload into the shared location", b => new DataBusSendBehavior(b.GetService<IDataBus>(), b.GetService<IDataBusSerializer>(), conventions))
            {
                InsertAfter("MutateOutgoingMessages");
                InsertAfter("ApplyTimeToBeReceived");
            }
        }
    }
}