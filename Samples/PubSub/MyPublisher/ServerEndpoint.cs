using System;
using MyMessages;
using MyMessages.Other;
using NServiceBus;

namespace MyPublisher
{
    public class ServerEndpoint : IWantToRunWhenBusStartsAndStops
    {
        public IBus Bus { get; set; }

        public void Start()
        {
            Console.WriteLine("This will publish IEvent, EventMessage, and AnotherEventMessage alternately.");
            Console.WriteLine("Press 'Enter' to publish a message.To exit, Ctrl + C");

            int nextEventToPublish = 0;
            while (Console.ReadLine() != null)
            {
                IMyEvent eventMessage;

                switch (nextEventToPublish)
                {
                    case 0 :
                        eventMessage = Bus.CreateInstance<IMyEvent>();
                        nextEventToPublish = 1;
                        break;
                    case 1 :
                        eventMessage = new EventMessage();
                        nextEventToPublish = 2;
                        break;
                    default:
                        eventMessage = new AnotherEventMessage();
                        nextEventToPublish = 0;
                        break;
                }

                eventMessage.EventId = Guid.NewGuid();
                eventMessage.Time = DateTime.Now.Second > 30 ? (DateTime?)DateTime.Now : null;
                eventMessage.Duration = TimeSpan.FromSeconds(99999D);

                Bus.Publish(eventMessage);

                Console.WriteLine("Published event with Id {0}.", eventMessage.EventId);
                Console.WriteLine("==========================================================================");
            }
        }

        public void Stop()
        {

        }
    }
}