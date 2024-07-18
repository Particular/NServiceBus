namespace NServiceBus;

using System;
using System.Threading.Tasks;
using System.Transactions;
using DataBus;
using Pipeline;

class DataBusReceiveBehavior : IBehavior<IIncomingLogicalMessageContext, IIncomingLogicalMessageContext>
{
    public DataBusReceiveBehavior(
        IDataBus dataBus,
        DataBusDeserializer deserializer,
        DataBusConventions conventions)
    {
        this.conventions = conventions;
        this.deserializer = deserializer;
        this.dataBus = dataBus;
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
                using (var stream = await dataBus.Get(dataBusKey, context.CancellationToken).ConfigureAwait(false))
                {
                    context.Headers.TryGetValue(Headers.DataBusConfigContentType, out var serializerUsed);

                    if (dataBusProperty != null)
                    {
                        var value = deserializer.Deserialize(serializerUsed, dataBusProperty.Type, stream);

                        dataBusProperty.SetValue(value);
                    }
                    else
                    {
                        var value = deserializer.Deserialize(serializerUsed, property.Type, stream);

                        property.Setter(message, value);
                    }
                }
            }
        }

        await next(context).ConfigureAwait(false);
    }

    readonly DataBusConventions conventions;
    readonly IDataBus dataBus;
    readonly DataBusDeserializer deserializer;

    public class Registration : RegisterStep
    {
        public Registration(Func<IServiceProvider, DataBusReceiveBehavior> factory) : base("DataBusReceive", typeof(DataBusReceiveBehavior), "Copies the databus shared data back to the logical message", b => factory(b))
        {
            InsertAfter("MutateIncomingMessages");
        }
    }
}