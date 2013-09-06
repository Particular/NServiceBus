namespace NServiceBus.Unicast.Subscriptions.MessageDrivenSubscriptions.SubcriberSideFiltering
{
    using Logging;
    using MessageMutator;
    using ObjectBuilder;

    public class FilteringMutator:IMutateIncomingMessages
    {
        public SubscriptionPredicatesEvaluator SubscriptionPredicatesEvaluator { get; set; }
        public IBuilder Builder { get; set; }
     
        public object MutateIncoming(object message)
        {
            if (SubscriptionPredicatesEvaluator == null)
                return message;

            foreach (var condition in SubscriptionPredicatesEvaluator.GetConditionsForMessage(message))
            {
                if (condition(message)) continue;

                Logger.Debug(string.Format("Condition {0} failed for message {1}", condition, message.GetType().Name));

                //service locate to avoid a circular dependency
                Builder.Build<IBus>().DoNotContinueDispatchingCurrentMessageToHandlers();
                break;
            }
            return message;
        }

        readonly static ILog Logger = LogManager.GetLogger(typeof(FilteringMutator));
    }
}