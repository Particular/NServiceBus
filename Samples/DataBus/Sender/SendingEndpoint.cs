namespace Sender
{
	using System;
	using NServiceBus;
	using NServiceBus.DataBus;
	using Receiver.Messages;

	public class SendingEndpoint:IWantToRunAtStartup
	{
		readonly IBus bus;

		public SendingEndpoint(IBus bus)
		{
			this.bus = bus;
		}

		public void Run()
		{
			Console.WriteLine("Press 'Enter' to send a large message (>4MB). Use 'e' to send a message that will exceed the limit and throw. To exit, Ctrl + C");

			string key;

			while ((key = Console.ReadLine()) != null)
			{
				if(key == "e")
					SendMessageThatIsLargerThanMsmqCanHandle();
				else
					bus.Send<MessageWithLargePayload>(m =>
					{
						m.SomeProperty = "This message contains a large blob that will be sent on the data bus";
						m.LargeBlob = new DataBusProperty<byte[]>(new byte[1024 * 1024 * 5]);//5MB
					});

				Console.WriteLine("Message sent, the payload is stored in: " + SetupDataBus.BasePath);
			}
		}

		void SendMessageThatIsLargerThanMsmqCanHandle()
		{
			try
			{
				bus.Send<AnotherMessageWithLargePayload>(m =>
				{
					m.LargeBlob = new byte[1024 * 1024 * 5];//5MB
				});

			}
			catch (Exception ex)
			{
				Console.WriteLine(ex);
			}
	
		}

		public void Stop()
		{
		}
  
	}
}