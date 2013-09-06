namespace NServiceBus.AutomaticSubscriptions
{
    using System.Linq;
    using Features;
    using Logging;

    public class AutoSubscriber:IWantToRunWhenBusStartsAndStops
    {
        public IAutoSubscriptionStrategy AutoSubscriptionStrategy { get; set; }

        public IBus Bus { get; set; }

        public void Start()
        {
            if (!Feature.IsEnabled<AutoSubscribe>())
                return;


            foreach (var eventType in AutoSubscriptionStrategy.GetEventsToSubscribe()
                .Where(t => !MessageConventionExtensions.IsInSystemConventionList(t))) //never autosubscribe system messages
            {
                Bus.Subscribe(eventType);

                Logger.DebugFormat("Autosubscribed to event {0}", eventType);
            }
        }

        public void Stop()
        {
            
        }

        readonly static ILog Logger = LogManager.GetLogger(typeof(DefaultAutoSubscriptionStrategy));
    }
}