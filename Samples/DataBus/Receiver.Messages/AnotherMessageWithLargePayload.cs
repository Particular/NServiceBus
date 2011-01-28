namespace Receiver.Messages
{
	using NServiceBus;

	public class AnotherMessageWithLargePayload : IMessage
	{
		public byte[]LargeBlob { get; set; }
	}
}