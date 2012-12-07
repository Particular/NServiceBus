using System;
using MyMessages;
using MyMessages.Other;
using NServiceBus;

namespace MyPublisher
{
    using MyMessages.Publisher;
    using MyMessages.Subscriber1;
    using MyMessages.Subscriber2;
    using MyMessages.SubscriberNMS;

    public class ServerEndpoint : IWantToRunWhenBusStartsAndStops
    {
        private int nextEventToPublish = 0;
        private int nextCommandToPublish = 0;
        public IBus Bus { get; set; }

        public void Start()
        {
            Console.WriteLine("Press 'e' to publish an IEvent, EventMessage, and AnotherEventMessage alternately.");
            Console.WriteLine("Press 's' to send a command to Subscriber1, Subscriber2, SubscriberNMS alternately");
            Console.WriteLine("Press 'q' to exit");

            
            while (true)
            {
                var key = Console.ReadKey();
                switch (key.KeyChar)
                {
                    case 'q':
                        return;
                    case 'e':
                        this.PublishEvent();
                        break;
                    case 's':
                        this.SendCommand();
                        break;
                }
            }
        }

        private void SendCommand()
        {
            IMyCommand commandMessage;

            switch (nextCommandToPublish)
            {
                case 0:
                    commandMessage = this.Bus.CreateInstance<MyRequest1>();
                    nextCommandToPublish = 1;
                    break;
                case 1:
                    commandMessage = this.Bus.CreateInstance<IMyRequest2>();
                    nextCommandToPublish = 2;
                    break;
                default:
                    commandMessage = new MyRequestNMS();
                    nextCommandToPublish = 0;
                    break;
            }

            commandMessage.CommandId = Guid.NewGuid();
            commandMessage.Time = DateTime.Now.Second > -1 ? (DateTime?)DateTime.Now : null;
            commandMessage.Duration = TimeSpan.FromSeconds(99999D);

            this.Bus.Send(commandMessage).Register<ResponseCode>(response =>
                {
                    Console.WriteLine("Received Response to request {0}: {1}", commandMessage.CommandId, response);
                    Console.WriteLine("==========================================================================");
                });

            Console.WriteLine("Published event with Id {0}.", commandMessage.CommandId);
            Console.WriteLine("==========================================================================");
        }

        private void PublishEvent()
        {
            IMyEvent eventMessage;

            switch (nextEventToPublish)
            {
                case 0:
                    eventMessage = this.Bus.CreateInstance<IMyEvent>();
                    nextEventToPublish = 1;
                    break;
                case 1:
                    eventMessage = new EventMessage();
                    nextEventToPublish = 2;
                    break;
                default:
                    eventMessage = new AnotherEventMessage();
                    nextEventToPublish = 0;
                    break;
            }

            eventMessage.EventId = Guid.NewGuid();
            eventMessage.Time = DateTime.Now.Second > -1 ? (DateTime?)DateTime.Now : null;
            eventMessage.Duration = TimeSpan.FromSeconds(99999D);

            this.Bus.Publish(eventMessage);

            Console.WriteLine("Published event with Id {0}.", eventMessage.EventId);
            Console.WriteLine("==========================================================================");
        }

        public void Stop()
        {

        }
    }
}