namespace NServiceBus.AutomaticSubscriptions
{
    using System.Linq;
    using Logging;

    class AutoSubscriber:IWantToRunWhenBusStartsAndStops
    {
        public AutoSubscriptionStrategy AutoSubscriptionStrategy { get; set; }

        public IBus Bus { get; set; }


        public bool Enabled { get; set; }

        public void Start()
        {
            if (!Enabled)
                return;


            foreach (var eventType in AutoSubscriptionStrategy.GetEventsToSubscribe()
                .Where(t => !MessageConventionExtensions.IsInSystemConventionList(t))) //never auto-subscribe system messages
            {
                Bus.Subscribe(eventType);

                Logger.DebugFormat("Auto subscribed to event {0}", eventType);
            }
        }

        public void Stop()
        {
            
        }

        static ILog Logger = LogManager.GetLogger<AutoSubscriber>();
    }
}