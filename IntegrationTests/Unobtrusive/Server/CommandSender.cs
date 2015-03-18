namespace Server
{
    using System;
    using Events;
    using Messages;
    using NServiceBus;

    class CommandSender
    {
        public IBus Bus { get; set; }

        public void Start()
        {
            Console.WriteLine("Press 'E' to publish an event");
            Console.WriteLine("Press 'D' to send a deferred message");
            Console.WriteLine("To exit, press Ctrl + C");

            while (true)
            {
                var cmd = Console.ReadKey().Key.ToString().ToLower();

                switch (cmd)
                {
                    case "e":
                        PublishEvent();
                        break;
                    case "d":
                        DeferMessage();
                        break;
                }
            }
        }

        void DeferMessage()
        {
            Bus.Defer(TimeSpan.FromSeconds(10), new DeferredMessage());
            Console.WriteLine();
           Console.WriteLine("{0} - {1}", DateTime.Now.ToLongTimeString(), "Sent a message that is deferred for 10 seconds");
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
    }
}