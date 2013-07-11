namespace MyServer.Common
{
    using System;
    using System.Collections.Concurrent;
    using NServiceBus;

    public class MyMessageHandler : IHandleMessages<MyMessage>
    {
        private static readonly ConcurrentDictionary<Guid, string> Last = new ConcurrentDictionary<Guid, string>();

        public IBus Bus{ get; set; }

        public void Handle(MyMessage message)
        {
            Console.WriteLine("ReplyToAddress: {0} MessageId:{1}",Bus.CurrentMessageContext.ReplyToAddress,Bus.CurrentMessageContext.Id);
            var numOfRetries = message.GetHeader(Headers.Retries);

            if (numOfRetries != null)
            {                
                string value;
                Last.TryGetValue(message.Id, out value);

                if (numOfRetries != value)
                {
                    Console.WriteLine("This is second level retry number {0}", numOfRetries);
                    Last.AddOrUpdate(message.Id, numOfRetries, (key, oldValue) => numOfRetries);
                }
            }            
            
            throw new Exception("An exception occurred in the handler.");
        }
    }
}
