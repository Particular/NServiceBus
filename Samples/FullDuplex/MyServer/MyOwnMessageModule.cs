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
            Console.WriteLine(string.Format("MyOwnMessageModule({0}) - {1}", GetHashCode(), message));
        }

    }
}