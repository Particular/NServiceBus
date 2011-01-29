using NServiceBus.MessageMutator;

namespace NServiceBus.DataBus
{
	using System;
	using System.IO;
	using System.Transactions;
	using Serialization;

	public class DataBusMessageMutator : IMessageMutator
	{
		readonly IDataBus dataBus;
		readonly IDatabusSeralizer serializer;

		public DataBusMessageMutator(IDataBus dataBus, IDatabusSeralizer serializer)
		{
			this.dataBus = dataBus;
			this.serializer = serializer;
		}

		IMessage IMutateOutgoingMessages.MutateOutgoing(IMessage message)
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
						}
				}

			return message;
		}

		IMessage IMutateIncomingMessages.MutateIncoming(IMessage message)
		{
			using (new TransactionScope(TransactionScopeOption.Suppress))
				foreach (var dataBusProperty in message.DataBusPropertiesWithValues())
				{
						using (var stream = dataBus.Get(dataBusProperty.Key))
						{
							var value = serializer.Deserialize(stream);
							dataBusProperty.SetValue(value);
						}
				}
			return message;
		}
	}
}