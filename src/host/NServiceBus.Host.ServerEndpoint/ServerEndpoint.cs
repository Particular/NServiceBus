using System;
using Messages;

namespace NServiceBus.Host.ServerEndpoint
{
    public class ServerEndpoint : IMessageEndpoint
    {
        private readonly IBus bus;

        public ServerEndpoint(IBus bus)
        {
            this.bus = bus;
        }

        public void OnStop()
        {
        }

        public void OnStart()
        {
            while(true)
            {
                Console.ReadLine();
                Notify();
            }
        }

        public long IntervalMilliseconds
        {
            get { return 30000; }
        }

        bool publishIEvent = true;
           
        public void Notify()
        {
            IEvent eventMessage;
            if (publishIEvent)
                eventMessage = bus.CreateInstance<IEvent>();
            else
                eventMessage = new EventMessage();

            eventMessage.EventId = Guid.NewGuid();
            eventMessage.Time = DateTime.Now;
            eventMessage.Duration = TimeSpan.FromSeconds(99999D);

            bus.Publish(eventMessage);

            Console.WriteLine("Published event with Id {0}.", eventMessage.EventId);

            publishIEvent = !publishIEvent;
               
        }

    }
}
