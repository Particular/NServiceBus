namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
    using System.Transactions;
    using DataBus;
    using Pipeline;
    using Pipeline.Contexts;

    class DataBusReceiveBehavior : Behavior<IIncomingLogicalMessageContext>
    {
        public IDataBus DataBus { get; set; }

        public IDataBusSerializer DataBusSerializer { get; set; }

        public Conventions Conventions { get; set; }

        public override async Task Invoke(IIncomingLogicalMessageContext context, Func<Task> next)
        {
            var message = context.Message.Instance;

            foreach (var property in Conventions.GetDataBusProperties(message))
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

                string dataBusKey;

                if (!context.Headers.TryGetValue("NServiceBus.DataBus." + headerKey, out dataBusKey))
                {
                    continue;
                }

                using (new TransactionScope(TransactionScopeOption.Suppress, TransactionScopeAsyncFlowOption.Enabled))
                using (var stream = await DataBus.Get(dataBusKey).ConfigureAwait(false))
                {
                    var value = DataBusSerializer.Deserialize(stream);

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

            await next().ConfigureAwait(false);
        }

        public class Registration : RegisterStep
        {
            public Registration() : base("DataBusReceive", typeof(DataBusReceiveBehavior), "Copies the databus shared data back to the logical message")
            {
                InsertAfter(WellKnownStep.MutateIncomingMessages);
            }
        }
    }
}