namespace MyServer.DeferedProcessing
{
    using System;
    using NServiceBus;

    public class DeferredMessageHandler : IHandleMessages<DeferredMessage>
    {
        public void Handle(DeferredMessage message)
        {
            Console.WriteLine(string.Format("{0} - {1}", DateTime.Now.ToLongTimeString(), "Deferred message processed"));
        }
    }
}