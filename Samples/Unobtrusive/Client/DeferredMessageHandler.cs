namespace Client
{
    using System;
    using Messages;
    using NServiceBus;

    public class DeferredMessageHandler : IHandleMessages<DeferredMessage>
    {
        public void Handle(DeferredMessage message)
        {
            Console.WriteLine(string.Format("\n{0} - {1}", DateTime.Now.ToLongTimeString(), "Received a deferred message"));
        }
    }
}