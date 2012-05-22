using System;
using System.Collections.Concurrent;
using NServiceBus;
using NServiceBus.Management.Retries;

namespace MyServer.Common
{
    public class Application : IWantToRunAtStartup
    {
        public IBus Bus { get; set; }        

        public void Run()
        {            
            Console.WriteLine("Press 'S' to send a message that will throw an exception.");            
            Console.WriteLine("Press 'Q' to exit.");            
                        
            string cmd;

            while ((cmd = Console.ReadKey().Key.ToString().ToLower()) != "q")            
            {
                Console.WriteLine("");

                switch (cmd)
                {
                    case "s":
                        var m = new MyMessage{Id = Guid.NewGuid()};
                        Bus.SendLocal(m);
                        break;
                }                
            }
        }

        public void Stop()
        {            
        }
    }

    public class MyMessageHandler : IHandleMessages<MyMessage>
    {
        private static readonly ConcurrentDictionary<Guid, string> Last = new ConcurrentDictionary<Guid, string>();

        public void Handle(MyMessage message)
        {
            var numOfRetries = message.GetHeader(SecondLevelRetriesHeaders.Retries);

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

            throw new Exception("An exception occured in the handler.");
        }
    }    
}