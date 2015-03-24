namespace NServiceBus.Unicast.Routing
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using Logging;

    /// <summary>
    ///     The default message router
    /// </summary>
    public class StaticMessageRouter
    {
        /// <summary>
        ///     Initializes the router with all known messages
        /// </summary>
        public StaticMessageRouter(IEnumerable<Type> knownMessages)
        {
            Guard.AgainstNull(knownMessages, "knownMessages");
            routes = new ConcurrentDictionary<Type, List<string>>();
            foreach (var knownMessage in knownMessages)
            {
                routes[knownMessage] = new List<string>();
            }
        }

        /// <summary>
        /// Returns all the routes for a given message
        /// </summary>
        /// <param name="messageType">The <see cref="Type"/> of the message to get the destination <see cref="Address"/> list for.</param>
        public List<string> GetDestinationFor(Type messageType)
        {
            Guard.AgainstNull(messageType, "messageType");
            List<string> address;
            if (!routes.TryGetValue(messageType, out address))
            {
                return new List<string>();
            }

            return address;
        }

        /// <summary>
        /// Registers a route for the given event
        /// </summary>
        /// <param name="eventType">The <see cref="Type"/> of the event</param>
        /// <param name="endpointAddress">The <see cref="Address"/> representing the logical owner for the event</param>
        public void RegisterEventRoute(Type eventType, string endpointAddress)
        {
            Guard.AgainstNull(eventType, "eventType");
            if (endpointAddress == null)
            {
                throw new InvalidOperationException(String.Format("'{0}' can't be registered with null route.", eventType.FullName));
            }

            List<string> currentAddress;

            if (!routes.TryGetValue(eventType, out currentAddress))
            {
                routes[eventType] = currentAddress = new List<string>();
            }

            Logger.DebugFormat(currentAddress.Any() ? "Routing for message: {0} appending {1}" : "Routing for message: {0} set to {1}", eventType, endpointAddress);

            currentAddress.Add(endpointAddress);

            foreach (var route in routes.Where(route => eventType != route.Key && route.Key.IsAssignableFrom(eventType)))
            {
                if (route.Value.Any())
                {
                    Logger.InfoFormat("Routing for inherited message: {0}({1}) appending {2}", route.Key, eventType, endpointAddress);
                }
                else
                {
                    Logger.DebugFormat("Routing for inherited message: {0}({1}) set to {2}", route.Key, eventType, endpointAddress);
                }

                route.Value.Add(endpointAddress);
            }
        }

        /// <summary>
        /// Registers a route for the given message type
        /// </summary>
        /// <param name="messageType">The message type</param>
        /// <param name="endpointAddress">The address of the logical owner</param>
        public void RegisterMessageRoute(Type messageType, string endpointAddress)
        {
            Guard.AgainstNull(messageType, "messageType");
            if (endpointAddress == null)
            {
                throw new InvalidOperationException(String.Format("'{0}' can't be registered with Address.Undefined route.", messageType.FullName));
            }

            List<string> currentAddress;

            if (!routes.TryGetValue(messageType, out currentAddress))
            {
                routes[messageType] = currentAddress = new List<string>();
            }

            Logger.DebugFormat("Routing for message: {0} set to {1}", messageType, endpointAddress);
            currentAddress.Clear();
            currentAddress.Add(endpointAddress);

            //go through the existing routes and see if this means that we can route any of those
            foreach (var route in routes.Where(route => messageType != route.Key && route.Key.IsAssignableFrom(messageType)))
            {
                Logger.DebugFormat("Routing for inherited message: {0}({1}) set to {2}", route.Key, messageType, endpointAddress);
                route.Value.Clear();
                route.Value.Add(endpointAddress);
            }
        }

        /// <summary>
        /// Obsolete
        /// </summary>
        [ObsoleteEx(TreatAsErrorFromVersion = "6", RemoveInVersion = "7", ReplacementTypeOrMember = "config.AutoSubscribe().AutoSubscribePlainMessages()")]
        public bool SubscribeToPlainMessages { get; set; }

        static ILog Logger = LogManager.GetLogger<StaticMessageRouter>();
        readonly ConcurrentDictionary<Type, List<string>> routes;
    }
}