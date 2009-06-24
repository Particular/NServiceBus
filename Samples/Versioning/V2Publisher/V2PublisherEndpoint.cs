using System;
using NServiceBus;
using NServiceBus.Host;

namespace V2Publisher
{
    public class V2PublisherEndpoint : IMessageEndpoint
    {
        public IBus Bus { get; set; }
        
        public void OnStart()
        {
            Console.WriteLine("Press 'Enter' to publish a message, Ctrl + C to exit.");

            Action a = () =>
                           {
                               while (Console.ReadLine() != null)
                               {
                                   Bus.Publish<V2.Messages.SomethingHappened>(sh =>
                                                                                  {
                                                                                      sh.SomeData = 1;
                                                                                      sh.MoreInfo = "It's a secret.";
                                                                                  });

                                   Console.WriteLine("Published event.");
                               }
                           };

            a.BeginInvoke(null, null);
        }

        public void OnStop()
        {
        }
    }
}