using System;
using NServiceBus;

namespace V2Publisher
{
    public class V2PublisherEndpoint : IWantToRunWhenBusStartsAndStops
    {
        public IBus Bus { get; set; }
        
        public void Start()
        {
            Console.WriteLine("Press 'Enter' to publish a message, Ctrl + C to exit.");

           while (Console.ReadLine() != null)
           {
               Bus.Publish<V2.Messages.ISomethingHappened>(sh =>
                                                              {
                                                                  sh.SomeData = 1;
                                                                  sh.MoreInfo = "It's a secret.";
                                                              });

               Console.WriteLine("Published event.");
               Console.WriteLine("======================================================================");
           }
        }

        public void Stop()
        {
        }
    }
}