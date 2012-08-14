using System;
using System.Collections.Generic;

namespace NServiceBus
{
    /// <summary>
    /// Extension method on message handler.
    /// </summary>
    public static class MessageHandlerExtensionMethods
    {
        /// <summary>
        /// Extension method on MessageHandler. Users can avoid declaring an IBus to be injected, and use the bus implicitly.
        /// </summary>
        /// <example> The following is an example on how a message handler might look like using the Bus() extension method:
        /// <code escaped="false">
        /// public class RequestDataMessageHandler : IHandleMessages&lt;RequestDataMessage&gt;
        /// {
        ///    public void Handle(RequestDataMessage message)
        ///    {
        ///       var response = this.Bus().CreateInstance&lt;DataResponseMessage&gt;(m =>
        ///      {
        ///            m.DataId = message.DataId;
        ///            m.String = message.String;
        ///        });
        ///        this.Bus().Reply(response);
        ///    }
        /// }
        /// </code>
        /// </example>
        /// <typeparam name="T">The message type to handle</typeparam>
        /// <param name="handler">The <see cref="IMessageHandler{T}" /> implementing class</param>
        /// <returns>IBus interface</returns>
        public static IBus Bus<T>(this IMessageHandler<T> handler)
        {
            return ExtensionMethods.Bus;
        }
    }
    
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
        public static object CurrentMessageBeingHandled { get { return currentMessageBeingHandled; } set { currentMessageBeingHandled = value; } }

        [ThreadStatic]
        static object currentMessageBeingHandled;

        /// <summary>
        /// The action used to set the header in the <see cref="SetHeader"/> method.
        /// </summary>
        public static Action<object, string, string> SetHeaderAction = (x, y, z) =>
                                                                           {
                                                                               //default to no-op to avoid getting in the way of unittesting
                                                                           };

        /// <summary>
        /// The action used to get the header value in the <see cref="GetHeader"/> method.
        /// </summary>
        public static Func<object, string, string> GetHeaderAction = (x, y) => "No header get header action was defined, please spicify one using ExtensionMethods.GetHeaderAction = ...";

        /// <summary>
        /// The action used to get all the headers for a message.
        /// </summary>
        public static Func<IDictionary<string, string>> GetStaticOutgoingHeadersAction { get; set; }
    }
}
