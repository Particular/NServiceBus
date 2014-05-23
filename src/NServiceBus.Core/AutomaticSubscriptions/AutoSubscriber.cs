namespace NServiceBus.AutomaticSubscriptions
{
    using System.Linq;
    using Features;
    using Logging;

    public class AutoSubscriber:IWantToRunWhenBusStartsAndStops
    {
        public AutoSubscriptionStrategy AutoSubscriptionStrategy { get; set; }

        public IBus Bus { get; set; }

        public void Start()
        {
            if (!Feature.IsEnabled<AutoSubscribe>())
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