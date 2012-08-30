using System;
using System.Collections.Concurrent;
using NServiceBus;
using NServiceBus.Management.Retries;

namespace MyServer.Common
{
    public class MyMessageHandler : IHandleMessages<MyMessage>
    {
        private static readonly ConcurrentDictionary<Guid, string> Last = new ConcurrentDictionary<Guid, string>();

        public IBus Bus{ get; set; }
        public void Handle(MyMessage message)
        {
            Console.WriteLine("ReplyToAddress: " + Bus.CurrentMessageContext.ReplyToAddress);
            var numOfRetries = message.GetHeader(Headers.Retries);

            if (numOfRetries != null)
            {                
                string value;
                Last.TryGetValue(message.Id, out value);

                if (numOfRetries != value)
                {
                    Console.WriteLine("This is second level retry number {0}, MessageId: {1} (Notice that NSB keeps the ID consistent for all retries)", numOfRetries,Bus.CurrentMessageContext.Id);
                    Last.AddOrUpdate(message.Id, numOfRetries, (key, oldValue) => numOfRetries);
                }
            }            
            
            throw new Exception("An exception occurred in the handler.");
        }
    }
}