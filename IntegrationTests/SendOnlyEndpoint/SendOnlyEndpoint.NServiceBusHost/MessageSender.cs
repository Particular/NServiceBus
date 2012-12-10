namespace SendOnlyEndpoint.NServiceBusHost
{
    using System;
    using NServiceBus;

    public static class MessageSender
    {
        public static void SendMessage(object bus)
        {
            Console.Out.WriteLine("Press any key to send a message.");
            Console.ReadKey();
            if ((bus as IBus) != null)
                (bus as IBus).Send("SendOnlyDestination@someserver", new TestMessage());
            Console.WriteLine("Message sent to remote endpoint, you can verify this by looking at the outgoing queues in you msmq MMC-snapin");  
        }
    }
    public class TestMessage : IMessage{}
}
