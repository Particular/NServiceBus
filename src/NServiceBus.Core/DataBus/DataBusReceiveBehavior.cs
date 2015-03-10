namespace NServiceBus
{
    using System;
    using System.Transactions;
    using NServiceBus.DataBus;
    using Pipeline;
    using Pipeline.Contexts;

    class DataBusReceiveBehavior : LogicalMessageProcessingStageBehavior
    {
        public IDataBus DataBus { get; set; }

        public IDataBusSerializer DataBusSerializer { get; set; }

        public Conventions Conventions { get; set; }

        public override void Invoke(Context context, Action next)
        {
            var message = context.IncomingLogicalMessage.Instance;

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

                using (new TransactionScope(TransactionScopeOption.Suppress))
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

            next();
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