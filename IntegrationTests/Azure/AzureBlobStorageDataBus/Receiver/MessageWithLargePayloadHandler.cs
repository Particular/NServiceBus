namespace Receiver
{
	using System;
	using Messages;
	using NServiceBus;

	public class MessageWithLargePayloadHandler : IHandleMessages<MessageWithLargePayload>
	{
		public void Handle(MessageWithLargePayload message)
		{
			Console.WriteLine("Message received, size of blob property: " + message.LargeBlob.Value.Length + " Bytes");
		}
	}
}