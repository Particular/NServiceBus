using System;
using System.Collections.Generic;

namespace NServiceBus
{
    /// <summary>
    /// Class containing extension methods for base class libraries for using interface-based messages.
    /// </summary>
    public static class ExtensionMethods
    {
        /// <summary>
        /// Instantiates an instance of T and adds it to the list.
        /// </summary>
        /// <typeparam name="T">The type to instantiate.</typeparam>
        /// <param name="list">The list to which to add the new element</param>
        /// <param name="initializer">An action for setting properties of the created instance.</param>
        public static void Add<T>(this IList<T> list, Action<T> initializer)
        {
            if (MessageCreator == null)
                throw new InvalidOperationException("MessageCreator has not been set.");

            list.Add(MessageCreator.CreateInstance(initializer));
        }

        /// <summary>
        /// Returns true if the given object is a message.
        /// </summary>
        /// <param name="o"></param>
        /// <returns></returns>
        public static bool IsMessage(this object o)
        {
            return o.GetType().IsMessageType();
        }

        /// <summary>
        /// Returns true if the given type is a message type.
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        public static bool IsMessageType(this Type t)
        {
            return IsMessageTypeAction(t);
        }

        /// <summary>
        /// Returns true if the given object is a command.
        /// </summary>
        /// <param name="o"></param>
        /// <returns></returns>
        public static bool IsCommand(this object o)
        {
            return o.GetType().IsCommandType();
        }

        /// <summary>
        /// Returns true if the given type is a command type.
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        public static bool IsCommandType(this Type t)
        {
            return IsCommandTypeAction(t);
        }

        /// <summary>
        /// Returns true if the given object is a event.
        /// </summary>
        /// <param name="o"></param>
        /// <returns></returns>
        public static bool IsEvent(this object o)
        {
            return o.GetType().IsEventType();
        }

        /// <summary>
        /// Returns true if the given type is a event type.
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        public static bool IsEventType(this Type t)
        {
            return IsEventTypeAction(t);
        }


        /// <summary>
        /// Get the header with the given key. Cannot be used to change its value.
        /// </summary>
        /// <param name="msg"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        public static string GetHeader(this object msg, string key)
        {
            return GetHeaderAction(msg, key);
        }

        /// <summary>
        /// If the source of this message was an Http endpoint, returns its address
        /// otherwise returns null.
        /// </summary>
        /// <param name="msg"></param>
        /// <returns></returns>
        public static string GetHttpFromHeader(this object msg)
        {
            return msg.GetHeader(Headers.HttpFrom);
        }

        /// <summary>
        /// If the target destination of this message is an Http endpoint,
        /// return the address of that target, otherwise null.
        /// </summary>
        /// <param name="msg"></param>
        /// <returns></returns>
        public static string GetHttpToHeader(this object msg)
        {
            return msg.GetHeader(Headers.HttpTo);
        }

        /// <summary>
        /// Returns the list of destination sites for this message
        /// </summary>
        /// <param name="msg"></param>
        /// <returns></returns>
        public static string GetDestinationSitesHeader(this object msg)
        {
            return msg.GetHeader(Headers.DestinationSites);
        }

        /// <summary>
        /// Returns the sitekey for the site for which this message originated, null if this message wasn't sent via the gateway
        /// </summary>
        /// <param name="msg"></param>
        /// <returns></returns>
        public static string GetOriginatingSiteHeader(this object msg)
        {
            return msg.GetHeader(Headers.OriginatingSite);
        }

        /// <summary>
        /// Sets the value of the header for the given key.
        /// </summary>
        /// <param name="msg"></param>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public static void SetHeader(this object msg, string key, string value)
        {
            SetHeaderAction(msg, key, value);
        }

		/// <summary>
        /// Sets the list of sites to where this message should be routed
        /// This method is reserved for the NServiceBus Gateway.
        /// </summary>
        /// <param name="msg"></param>
        /// <param name="value"></param>
        public static void SetDestinationSitesHeader(this object msg, string value)
        {
            msg.SetHeader(Headers.DestinationSites, value);
        }


        /// <summary>
        /// Sets the originating site header
        /// This method is reserved for the NServiceBus Gateway.
        /// </summary>
        /// <param name="msg"></param>
        /// <param name="value"></param>
        public static void SetOriginatingSiteHeader(this object msg, string value)
        {
            msg.SetHeader(Headers.OriginatingSite, value);
        }
		
        /// <summary>
        /// Sets the Http address from which this message was received.
        /// This method is reserved for the NServiceBus Gateway.
        /// </summary>
        /// <param name="msg"></param>
        /// <param name="value"></param>
        public static void SetHttpFromHeader(this object msg, string value)
        {
            msg.SetHeader(Headers.HttpFrom, value);
        }

        /// <summary>
        /// Sets the Http address to which this message should be sent.
        /// Requires the use of the NServiceBus Gateway.
        /// </summary>
        /// <param name="msg"></param>
        /// <param name="value"></param>
        public static void SetHttpToHeader(this object msg, string value)
        {
            msg.SetHeader(Headers.HttpTo, value);
        }

        /// <summary>
        /// Gets the value of the header with the given key and sets it for this message.
        /// </summary>
        /// <param name="msg"></param>
        /// <param name="key"></param>
        public static void CopyHeaderFromRequest(this object msg, string key)
        {
            if (msg == CurrentMessageBeingHandled)
                throw new InvalidOperationException("This method is not supported on the request message.");

            msg.SetHeader(key, CurrentMessageBeingHandled.GetHeader(key));
        }

        /// <summary>
        /// The object used by the extention methods to instantiate types.
        /// </summary>
        public static IMessageCreator MessageCreator { get; set; }

        /// <summary>
        /// The object used by the extension methods for accessing headers.
        /// </summary>
        public static IBus Bus { get; set; }

        /// <summary>
        /// The object used to see whether headers requested are for the handled message.
        /// </summary>
        public static object CurrentMessageBeingHandled { get; set; }

        /// <summary>
        /// The action used to set the header in the <see cref="SetHeader"/> method.
        /// </summary>
        public static Action<object, string, string> SetHeaderAction { get; set; }

        /// <summary>
        /// The action used to get the header value in the <see cref="GetHeader"/> method.
        /// </summary>
        public static Func<object, string, string> GetHeaderAction { get; set; }

        /// <summary>
        /// The action used to get all the headers for a message.
        /// </summary>
        public static Func<IDictionary<string, string>> GetStaticOutgoingHeadersAction { get; set; }

        /// <summary>
        /// The function used to determine whether a type is a message type.
        /// </summary>
        public static Func<Type, bool> IsMessageTypeAction { get; set; }


        /// <summary>
        /// The function used to determine whether a type is a command type.
        /// </summary>
        public static Func<Type, bool> IsCommandTypeAction = t => typeof(ICommand).IsAssignableFrom(t) && typeof(ICommand) != t;


        /// <summary>
        /// The function used to determine whether a type is a event type.
        /// </summary>
        public static Func<Type, bool> IsEventTypeAction = t => typeof(IEvent).IsAssignableFrom(t) && typeof(IEvent) != t;
    }

    /// <summary>
    /// Static class containing headers used by NServiceBus.
    /// </summary>
    public static class Headers
    {
        /// <summary>
        /// Header for retrieving from which Http endpoint the message arrived.
        /// </summary>
        public const string HttpFrom = "NServiceBus.From";

        /// <summary>
        /// Header for specifying to which Http endpoint the message should be delivered.
        /// </summary>
        public const string HttpTo = "NServiceBus.To";

        /// <summary>
        /// Header for specifying to which queue behind the http gateway should the message be delivered.
        /// This header is considered an applicative header.
        /// </summary>
        public const string RouteTo = "NServiceBus.Header.RouteTo";
		
		/// <summary>
        /// Header for specifying to which sites the gateway should send the message. For multiple
		/// sites a comma separated list can be used
        /// This header is considered an applicative header.
        /// </summary>
        public const string DestinationSites = "NServiceBus.DestinationSites";

        /// <summary>
        /// Header for specifying the key for the site where this message originated. 
        /// This header is considered an applicative header.
        /// </summary>
        public const string OriginatingSite = "NServiceBus.OriginatingSite";

        /// <summary>
        /// Prefix included on the wire when sending applicative headers.
        /// </summary>
        public const string HeaderName = "Header";
    }
}
