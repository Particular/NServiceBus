namespace MyServer.DeferedProcessing
{
    using System;
    using NServiceBus;

    public class DeferredMessage:IMessage
    {
        public DateTime ProcessAt { get; set; }
    }
}