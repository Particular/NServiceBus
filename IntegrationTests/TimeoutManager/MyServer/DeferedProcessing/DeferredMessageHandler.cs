namespace MyServer.DeferedProcessing
{
    using System;
    using NServiceBus;

    public class DeferredMessageHandler : IHandleMessages<DeferredMessage>
    {
        public IBus Bus { get; set; }

        public void Handle(DeferredMessage message)
        {
            if (!Bus.CurrentMessageContext.Headers.ContainsKey(Headers.IsDeferredMessage))
            {
                LogMessage("Message will be processed at " + message.ProcessAt.ToLongTimeString());

                Bus.Defer(message.ProcessAt, message);
                return;
            }

            LogMessage("Deferred message processed");
        }

        static void LogMessage(string message)
        {
            Console.WriteLine("{0} - {1}", DateTime.Now.ToLongTimeString(), message);
        }
    }
}