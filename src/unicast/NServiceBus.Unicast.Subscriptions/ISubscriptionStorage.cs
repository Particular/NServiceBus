using System;
using System.Collections.Generic;
using System.Text;
using NServiceBus.Unicast.Transport;

namespace NServiceBus.Unicast.Subscriptions
{
	/// <summary>
	/// Defines storage for subscriptions
	/// </summary>
    public interface ISubscriptionStorage
    {
		/// <summary>
		/// Gets all messages in the subscription store.
		/// </summary>
		/// <returns></returns>
        IList<Msg> GetAllMessages();

		/// <summary>
		/// Adds a message to the subscription store.
		/// </summary>
		/// <param name="m">The message to add.</param>
        void Add(Msg m);

		/// <summary>
		/// Removes a message from the subscription store.
		/// </summary>
		/// <param name="m">The message to remove.</param>
        void Remove(Msg m);
    }
}
