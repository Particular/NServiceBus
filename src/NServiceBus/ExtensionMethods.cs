namespace NServiceBus
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Extension method on message handler.
    /// </summary>
    public static class MessageHandlerExtensionMethods
    {
        /// <summary>
        /// Extension method on <see cref="IHandleMessages{T}"/>. Users can avoid declaring an <see cref="IBus"/> to be injected, and use the bus implicitly.
        /// </summary>
        /// <example> The following is an example on how a <see cref="IHandleMessages{T}"/> might look like using the <see cref="Bus{T}"/> extension method:
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
        /// <typeparam name="T">The message type to handle.</typeparam>
        /// <param name="handler">The <see cref="IHandleMessages{T}" /> implementing class.</param>
        /// <returns><see cref="IBus"/> interface.</returns>
        public static IBus Bus<T>(this IHandleMessages<T> handler)
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
        /// Get the header with the given key. Cannot be used to change its value.
        /// </summary>
        /// <param name="bus">The <see cref="IBus"/>.</param>
        /// <param name="msg">The message to retrieve a header from.</param>
        /// <param name="key">The header key.</param>
        /// <returns>The value assigned to the header.</returns>
        public static string GetMessageHeader(this IBus bus, object msg, string key)
        {
            return GetHeaderAction(msg, key);
        }

        /// <summary>
        /// Sets the value of the header for the given key.
        /// </summary>
        /// <param name="bus">The <see cref="IBus"/>.</param>
        /// <param name="msg">The message to add a header to.</param>
        /// <param name="key">The header key.</param>
        /// <param name="value">The value to assign to the header.</param>
        public static void SetMessageHeader(this IBus bus, object msg, string key, string value)
        {
            SetHeaderAction(msg, key, value);
        }

        /// <summary>
        /// Get the header with the given key. Cannot be used to change its value.
        /// </summary>
        /// <param name="msg">The <see cref="IMessage"/> to retrieve a header from.</param>
        /// <param name="key">The header key.</param>
        /// <returns>The value assigned to the header.</returns>
        public static string GetHeader(this IMessage msg, string key)
        {
            return GetHeaderAction(msg, key);
        }

        /// <summary>
        /// Sets the value of the header for the given key.
        /// </summary>
        /// <param name="msg">The <see cref="IMessage"/> to add a header to.</param>
        /// <param name="key">The header key.</param>
        /// <param name="value">The value to assign to the header.</param>
        public static void SetHeader(this IMessage msg, string key, string value)
        {
            SetHeaderAction(msg, key, value);
        }

        /// <summary>
        /// Instantiates an instance of <typeparamref name="T"/> and adds it to the list.
        /// </summary>
        /// <typeparam name="T">The type to instantiate.</typeparam>
        /// <param name="list">The list to which to add the new element.</param>
        /// <param name="initializer">An <see cref="Action"/> for setting properties of the created instance of <typeparamref name="T"/>.</param>
        [ObsoleteEx(RemoveInVersion = "5.0", TreatAsErrorFromVersion = "4.0", Replacement = "list.Add(Bus.CreateInstance(initializer))")]
        public static void Add<T>(this IList<T> list, Action<T> initializer)
        {
            if (MessageCreator == null)
                throw new InvalidOperationException("MessageCreator has not been set.");

            list.Add(MessageCreator.CreateInstance(initializer));
        }

       

        /// <summary>
        /// Get the header with the given key. Cannot be used to change its value.
        /// </summary>
        [ObsoleteEx(Replacement = "bus.GetMessageHeader(object msg, string key) or Headers.GetMessageHeader(object msg, string key)", RemoveInVersion = "5.0", TreatAsErrorFromVersion = "4.0")]
        public static string GetHeader(this object msg, string key)
        {
            return GetMessageHeader(null, msg, key);
        }

        /// <summary>
        /// If the source of this message was an Http endpoint, returns its address
        /// otherwise returns null.
        /// </summary>
        [ObsoleteEx(RemoveInVersion = "5.0", TreatAsErrorFromVersion = "4.0", Replacement = "Headers.GetMessageHeader(msg, NServiceBus.Headers.HttpFrom)")]
        public static string GetHttpFromHeader(this object msg)
        {
            return GetMessageHeader(null, msg, Headers.HttpFrom);
        }

        /// <summary>
        /// If the target destination of this message is an Http endpoint,
        /// return the address of that target, otherwise null.
        /// </summary>
        [ObsoleteEx(RemoveInVersion = "5.0", TreatAsErrorFromVersion = "4.0", Replacement = "Headers.GetMessageHeader(msg, NServiceBus.Headers.HttpTo)")]
        public static string GetHttpToHeader(this object msg)
        {
            return GetMessageHeader(null, msg, Headers.HttpTo);
        }

        /// <summary>
        /// Returns the list of destination sites for this message
        /// </summary>
        [ObsoleteEx(RemoveInVersion = "5.0", TreatAsErrorFromVersion = "4.0", Replacement = "Headers.GetMessageHeader(msg, NServiceBus.Headers.DestinationSites)")]
        public static string GetDestinationSitesHeader(this object msg)
        {
            return GetMessageHeader(null, msg, Headers.DestinationSites);
        }

        /// <summary>
        /// Returns the site key for the site for which this message originated, null if this message wasn't sent via the gateway
        /// </summary>
        [ObsoleteEx(RemoveInVersion = "5.0", TreatAsErrorFromVersion = "4.0", Replacement = "Headers.GetMessageHeader(msg, NServiceBus.Headers.OriginatingSite)")]
        public static string GetOriginatingSiteHeader(this object msg)
        {
            return GetMessageHeader(null, msg, Headers.OriginatingSite);
        }

        /// <summary>
        /// Sets the value of the header for the given key.
        /// </summary>
        [ObsoleteEx(Replacement = "bus.SetMessageHeader(object msg, string key, string value) or Headers.SetMessageHeader(object msg, string key, string value)", RemoveInVersion = "5.0", TreatAsErrorFromVersion = "4.0")]
        public static void SetHeader(this object msg, string key, string value)
        {
            SetMessageHeader(null, msg, key, value);
        }

		/// <summary>
        /// Sets the list of sites to where this message should be routed
        /// This method is reserved for the NServiceBus Gateway.
        /// </summary>
        [ObsoleteEx(RemoveInVersion = "5.0", TreatAsErrorFromVersion = "4.0", Replacement = "Headers.SetMessageHeader(msg, NServiceBus.Headers.DestinationSites, value)")]
        public static void SetDestinationSitesHeader(this object msg, string value)
        {
            SetMessageHeader(null, msg, Headers.DestinationSites, value);
        }


        /// <summary>
        /// Sets the originating site header
        /// This method is reserved for the NServiceBus Gateway.
        /// </summary>
        [ObsoleteEx(RemoveInVersion = "5.0", TreatAsErrorFromVersion = "4.0", Replacement = "Headers.SetMessageHeader(msg, NServiceBus.Headers.OriginatingSite, value)")]
        public static void SetOriginatingSiteHeader(this object msg, string value)
        {
            SetMessageHeader(null, msg, Headers.OriginatingSite, value);
        }
		
        /// <summary>
        /// Sets the Http address from which this message was received.
        /// This method is reserved for the NServiceBus Gateway.
        /// </summary>
        [ObsoleteEx(RemoveInVersion = "5.0", TreatAsErrorFromVersion = "4.0", Replacement = "Headers.SetMessageHeader(msg, NServiceBus.Headers.HttpFrom, value)")]
        public static void SetHttpFromHeader(this object msg, string value)
        {
            SetMessageHeader(null, msg, Headers.HttpFrom, value);
        }

        /// <summary>
        /// Sets the Http address to which this message should be sent.
        /// Requires the use of the NServiceBus Gateway.
        /// </summary>
        [ObsoleteEx(RemoveInVersion = "5.0", TreatAsErrorFromVersion = "4.0", Replacement = "Headers.SetMessageHeader(msg, NServiceBus.Headers.HttpTo, value)")]
        public static void SetHttpToHeader(this object msg, string value)
        {
            SetMessageHeader(null, msg, Headers.HttpTo, value);
        }

        /// <summary>
        /// Gets the value of the header with the given key and sets it for this message.
        /// </summary>
        [ObsoleteEx(RemoveInVersion = "5.0", TreatAsErrorFromVersion = "4.0", Replacement = "Headers.SetMessageHeader(msg, key, Bus.CurrentMessageContext.Headers[key])")]
        public static void CopyHeaderFromRequest(this object msg, string key)
        {
            if (msg == CurrentMessageBeingHandled)
                throw new InvalidOperationException("This method is not supported on the request message.");

            SetMessageHeader(null, msg, key, GetMessageHeader(null, CurrentMessageBeingHandled, key));
        }

        /// <summary>
        /// The <see cref="IMessageCreator"/> used by the extension methods to instantiate types.
        /// </summary>
        [ObsoleteEx(
            Message = "No longer required since the IBus batch operations have been trimmed",
            TreatAsErrorFromVersion = "4.3",
            RemoveInVersion = "5.0")]
        public static IMessageCreator MessageCreator { get; set; }

        /// <summary>
        /// The <see cref="IBus"/> used by the extension methods for accessing headers.
        /// </summary>
        public static IBus Bus { get; set; }

        /// <summary>
        /// The object used to see whether headers requested are for the handled message.
        /// </summary>
        public static object CurrentMessageBeingHandled { get { return currentMessageBeingHandled; } set { currentMessageBeingHandled = value; } }

        [ThreadStatic]
        static object currentMessageBeingHandled;

        /// <summary>
        /// The <see cref="Action{T1,T2,T3}"/> used to set the header in the <see cref="SetHeader(NServiceBus.IMessage,string,string)"/> method.
        /// </summary>
        public static Action<object, string, string> SetHeaderAction = (x, y, z) =>
                                                                           {
                                                                               //default to no-op to avoid getting in the way of unit testing
                                                                           };

        /// <summary>
        /// The <see cref="Func{T1,T2,TResult}"/> used to get the header value in the <see cref="GetHeader(NServiceBus.IMessage,string)"/> method.
        /// </summary>
        public static Func<object, string, string> GetHeaderAction = (x, y) => "No header get header action was defined, please specify one using ExtensionMethods.GetHeaderAction = ...";

        /// <summary>
        /// The <see cref="Func{TResult}"/> used to get all the headers for a message.
        /// </summary>
        public static Func<IDictionary<string, string>> GetStaticOutgoingHeadersAction { get; set; }
    }
}
