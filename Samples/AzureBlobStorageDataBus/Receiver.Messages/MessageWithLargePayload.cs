using System;

namespace Receiver.Messages
{
	using NServiceBus;

    [Serializable]
	[TimeToBeReceived("00:05:00")]//the data bus is allowed to clean up transmitted properties older than the TTBR
	public class MessageWithLargePayload:IMessage
	{
		public string SomeProperty { get; set; }
		public DataBusProperty<byte[]> LargeBlob { get; set; }
	}
}
