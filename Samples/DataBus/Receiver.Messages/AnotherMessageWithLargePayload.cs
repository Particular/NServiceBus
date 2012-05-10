namespace Receiver.Messages
{
	using NServiceBus;

	public class AnotherMessageWithLargePayload : ICommand
	{
		public byte[]LargeBlob { get; set; }
	}
}