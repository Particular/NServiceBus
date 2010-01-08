using System;
using NServiceBus;

namespace V2Publisher
{
    public class V2PublisherEndpoint : IWantToRunAtStartup
    {
        public IBus Bus { get; set; }
        
        public void Run()
        {
            Console.WriteLine("Press 'Enter' to publish a message, Ctrl + C to exit.");

           while (Console.ReadLine() != null)
           {
               Bus.Publish<V2.Messages.SomethingHappened>(sh =>
                                                              {
                                                                  sh.SomeData = 1;
                                                                  sh.MoreInfo = "It's a secret.";
                                                              });

               Console.WriteLine("Published event.");
           }
        }

        public void Stop()
        {
        }
    }
}