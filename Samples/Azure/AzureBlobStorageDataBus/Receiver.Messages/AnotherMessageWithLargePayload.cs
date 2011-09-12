using System;

namespace Receiver.Messages
{
	using NServiceBus;

    [Serializable]
	public class AnotherMessageWithLargePayload : IMessage
	{
		public byte[]LargeBlob { get; set; }
	}
}