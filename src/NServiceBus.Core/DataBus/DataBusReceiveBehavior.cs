﻿namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
    using System.Transactions;
    using DataBus;
    using Pipeline;

    class DataBusReceiveBehavior : IBehavior<IIncomingLogicalMessageContext, IIncomingLogicalMessageContext>
    {
        public DataBusReceiveBehavior(IDataBus databus, IDataBusSerializer serializer, Conventions conventions)
        {
            this.conventions = conventions;
            dataBusSerializer = serializer;
            dataBus = databus;
        }

        public async Task Invoke(IIncomingLogicalMessageContext context, Func<IIncomingLogicalMessageContext, Task> next)
        {
            var message = context.Message.Instance;

            foreach (var property in conventions.GetDataBusProperties(message))
            {
                var propertyValue = property.Getter(message);

                var dataBusProperty = propertyValue as IDataBusProperty;
                string headerKey;

                if (dataBusProperty != null)
                {
                    headerKey = dataBusProperty.Key;
                }
                else
                {
                    headerKey = $"{message.GetType().FullName}.{property.Name}";
                }

                if (!context.Headers.TryGetValue("NServiceBus.DataBus." + headerKey, out var dataBusKey))
                {
                    continue;
                }

                using (new TransactionScope(TransactionScopeOption.Suppress, TransactionScopeAsyncFlowOption.Enabled))
                {
                    using (var stream = await dataBus.Get(dataBusKey).ConfigureAwait(false))
                    {
                        var value = dataBusSerializer.Deserialize(stream);

                        if (dataBusProperty != null)
                        {
                            dataBusProperty.SetValue(value);
                        }
                        else
                        {
                            property.Setter(message, value);
                        }
                    }
                }
            }

            await next(context).ConfigureAwait(false);
        }

        readonly Conventions conventions;
        readonly IDataBus dataBus;
        readonly IDataBusSerializer dataBusSerializer;

        public class Registration : RegisterStep
        {
            public Registration(Conventions conventions) : base("DataBusReceive", typeof(DataBusReceiveBehavior), "Copies the databus shared data back to the logical message", b => new DataBusReceiveBehavior(b.Build<IDataBus>(), b.Build<IDataBusSerializer>(), conventions))
            {
                InsertAfter("MutateIncomingMessages");
            }
        }
    }
}