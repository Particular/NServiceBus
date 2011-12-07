namespace Server
{
    using System;
    using Events;
    using NServiceBus;

    class EventPublisher : IWantToRunAtStartup
    {
        public IBus Bus { get; set; }

        public void Run()
        {
            Console.WriteLine("Press 'E' to publish an event");
            Console.WriteLine("To exit, press Ctrl + C");

            string cmd;

            while ((cmd = Console.ReadKey().Key.ToString().ToLower()) != "q")
            {
                switch (cmd)
                {
                    case "e":
                        PublishEvent();
                        break;
                }
            }
        }


        void PublishEvent()
        {
            var eventId = Guid.NewGuid();

            Bus.Publish<IMyEvent>(m =>
            {
                m.EventId = eventId;
            });
            Console.WriteLine("Event published, id: " + eventId);
            
        }

        public void Stop()
        {
            
        }
    }
}