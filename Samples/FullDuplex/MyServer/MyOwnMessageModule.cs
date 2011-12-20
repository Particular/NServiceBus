namespace MyServer
{
    using System;
    using NServiceBus;

    public class MyOwnMessageModule : IMessageModule
    {
     
        public void HandleBeginMessage()
        {
            LogMessage("Begin");
        }

        public void HandleEndMessage()
        {
            LogMessage("End");
        }

        public void HandleError()
        {
            LogMessage("Error");
        }

        void LogMessage(string message)
        {
            Console.WriteLine(string.Format("MyOwnMessageModule({0}) - MessageID: {1}", GetHashCode(), Bus.CurrentMessageContext.Id));
        }

        public IBus Bus { get; set; }
    }
}