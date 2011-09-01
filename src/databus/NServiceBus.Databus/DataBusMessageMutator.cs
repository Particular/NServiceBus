using NServiceBus.MessageMutator;

namespace NServiceBus.DataBus
{
    using System;
    using System.IO;
    using System.Transactions;
    using Serialization;

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

        object IMutateOutgoingMessages.MutateOutgoing(object message)
        {
            var timeToBeReceived = message.TimeToBeReceived();

            using (new TransactionScope(TransactionScopeOption.Suppress))
                foreach (var dataBusProperty in message.DataBusPropertiesWithValues())
                {
                    using (var stream = new MemoryStream())
                    {
                        serializer.Serialize(dataBusProperty.GetValue(), stream);
                        stream.Position = 0;

                        dataBusProperty.Key = dataBus.Put(stream, timeToBeReceived);

                        //we use the headers to in order to allow the infrastructure (eg. the gateway) to modify the actual key
                        message.SetHeader(DATABUS_PREFIX + dataBusProperty.Key, dataBusProperty.Key);
                        

                    }
                }

            return message;
        }

        object IMutateIncomingMessages.MutateIncoming(object message)
        {
            using (new TransactionScope(TransactionScopeOption.Suppress))
                foreach (var dataBusProperty in message.DataBusPropertiesWithValues())
                {
                    var dataBusKey = message.GetHeader(DATABUS_PREFIX + dataBusProperty.Key);

                    using (var stream = dataBus.Get(dataBusKey))
                    {
                        var value = serializer.Deserialize(stream);
                        dataBusProperty.SetValue(value);
                    }
                }
            return message;
        }
    }
}