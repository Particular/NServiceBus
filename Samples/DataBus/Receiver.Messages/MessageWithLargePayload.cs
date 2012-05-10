namespace Receiver.Messages
{
	using NServiceBus;

	[TimeToBeReceived("00:01:00")]//the data bus is allowed to clean up transmitted properties older than the TTBR
    public class MessageWithLargePayload : ICommand
	{
		public string SomeProperty { get; set; }
		public DataBusProperty<byte[]> LargeBlob { get; set; }
	}
}
