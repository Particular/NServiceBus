using System;
using System.Collections.Generic;

namespace NServiceBus.Unicast
{
    /// <summary>
    /// Manages subscriptions and predicates for messages published by other endpoints
    /// and subscribed to by the local bus.
    /// </summary>
	/// <remarks>
	/// Thread safe.
	/// </remarks>
    public class SubscriptionsManager
    {
		/// <summary>
		/// Gets the list of conditions associated with a message.
		/// </summary>
		/// <param name="message">The message to get conditions for.</param>
		/// <returns>A list of conditions that are associated with type of message provided.</returns>
        public IList<Predicate<IMessage>> GetConditionsForMessage(IMessage message)
        {
            IList<Predicate<IMessage>> result = new List<Predicate<IMessage>>();

            lock (this.locker)
                if (this.messageTypeToConditionLookup.ContainsKey(message.GetType()))
                    foreach (Predicate<IMessage> condition in this.messageTypeToConditionLookup[message.GetType()])
                        result.Add(condition);

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
        public void AddConditionForSubscriptionToMessageType(Type messageType, Predicate<IMessage> condition)
        {
            if (condition == null)
                return;

            lock (this.locker)
            {
                if (!this.messageTypeToConditionLookup.ContainsKey(messageType))
                    this.messageTypeToConditionLookup.Add(messageType, new List<Predicate<IMessage>>());

                if (!this.messageTypeToConditionLookup[messageType].Contains(condition))
                    this.messageTypeToConditionLookup[messageType].Add(condition);
            }
        }

        private readonly IDictionary<Type, List<Predicate<IMessage>>> messageTypeToConditionLookup = new Dictionary<Type, List<Predicate<IMessage>>>();

        private readonly object locker = new object();
    }
}
