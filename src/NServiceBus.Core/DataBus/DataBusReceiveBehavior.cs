namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
    using System.Transactions;
    using NServiceBus.DataBus;
    using Pipeline;
    using Pipeline.Contexts;

    class DataBusReceiveBehavior : LogicalMessageProcessingStageBehavior
    {
        public IDataBus DataBus { get; set; }

        public IDataBusSerializer DataBusSerializer { get; set; }

        public Conventions Conventions { get; set; }

        public override Task Invoke(Context context, Func<Task> next)
        {
            var message = context.GetLogicalMessage().Instance;

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
                    headerKey = String.Format("{0}.{1}", message.GetType().FullName, property.Name);
                }

                string dataBusKey;

                if (!context.Headers.TryGetValue("NServiceBus.DataBus." + headerKey, out dataBusKey))
                {
                    continue;
                }

                using (new TransactionScope(TransactionScopeOption.Suppress, TransactionScopeAsyncFlowOption.Enabled))
                using (var stream = DataBus.Get(dataBusKey))
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

            return next();
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