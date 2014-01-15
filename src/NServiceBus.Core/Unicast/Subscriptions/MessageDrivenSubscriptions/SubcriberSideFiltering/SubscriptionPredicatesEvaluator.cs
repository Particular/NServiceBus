namespace NServiceBus.Unicast.Subscriptions.MessageDrivenSubscriptions.SubcriberSideFiltering
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Manages subscriptions and predicates for messages published by other endpoints
    /// and subscribed to by the local bus.
    /// </summary>
	/// <remarks>
	/// Thread safe.
	/// </remarks>
    public class SubscriptionPredicatesEvaluator
    {
		/// <summary>
		/// Gets the list of conditions associated with a message.
		/// </summary>
		/// <param name="message">The message to get conditions for.</param>
		/// <returns>A list of conditions that are associated with type of message provided.</returns>
        public IList<Predicate<object>> GetConditionsForMessage(object message)
        {
            var result = new List<Predicate<object>>();

            lock (locker)
            {
                List<Predicate<object>> conditionLookup;
                if (messageTypeToConditionLookup.TryGetValue(message.GetType(), out conditionLookup))
                {
                    foreach (var condition in conditionLookup)
                    {
                        result.Add(condition);
                    }
                }
            }

		    return result;
        }

		/// <summary>
		/// Adds a condition to a message type.
		/// </summary>
		/// <param name="messageType">The message type to add a condition to.</param>
		/// <param name="condition">The condition to add.</param>
		/// <remarks>
		/// All conditions added to a message type must be met if the messages of that type 
		/// are to be published to a subscriber.</remarks>
        public void AddConditionForSubscriptionToMessageType(Type messageType, Predicate<object> condition)
        {
		    if (condition == null)
		    {
		        return;
		    }

		    lock (locker)
		    {
		        List<Predicate<object>> conditionLookup;
		        if (!messageTypeToConditionLookup.TryGetValue(messageType, out conditionLookup))
		        {
		            messageTypeToConditionLookup[messageType] = conditionLookup = new List<Predicate<object>>();
		        }

		        if (!conditionLookup.Contains(condition))
		        {
		            conditionLookup.Add(condition);
		        }
		    }
        }

        private readonly IDictionary<Type, List<Predicate<object>>> messageTypeToConditionLookup = new Dictionary<Type, List<Predicate<object>>>();

        private readonly object locker = new object();
    }
}
