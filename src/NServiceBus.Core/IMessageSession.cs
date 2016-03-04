// ReSharper disable NotAccessedField.Local
#pragma warning disable 1591
namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;

    public class Usage
    {
        public void Test(IMessageSession session)
        {
            var options = new SendOptions();
            options.DelayDeliveryWith(TimeSpan.FromMinutes(15));
            options.RouteToThisEndpoint();
            options.SetCorrelationId("correlationId");
            options.SetMessageId("messageId");
            options.SetHeader("header1", "value1");
            options.SetHeader("header2", "value2");
            options.RouteReplyToThisInstance();
            session.Send(new object(), options);

            //messageid, headers, correlationid
            session.Send(new object(), 
                Defer.By(TimeSpan.FromMinutes(15)), 
                Route.ToThisEndpoint(), 
                CorrelationId.Set("correlationId"), 
                MessageId.Set("messageId"),
                Header.Set("header1", "value1"),
                Header.Set("header2", "value2"),
                Reply.ToThisInstance());
        }
    }

    public class SendOption
    {
        // require implementations to provide a key which is used to check for "duplicates" (e.g. Defer.To & Defer.By)
    }

    public class Header : SendOption
    {
        public static Header Set(string key, string value)
        {
            return new Header();
        }
    }

    public class MessageId : SendOption
    {
        public static MessageId Set(string messageId)
        {
            return new MessageId();
        }
    }

    public class CorrelationId : SendOption
    {
        public static CorrelationId Set(string correlationId)
        {
            return new CorrelationId();
        }
    }

    public class Reply : SendOption
    {
        public static Reply ToThisInstance()
        {
            return new Reply();
        }
    }

    public class Route : SendOption
    {
        private Route()
        {
        }

        public static Route ToThisEndpoint()
        {
            return new Route();
        }

        public static Route ToAddress(string address)
        {
            return new Route();
        }
    }

    public class Defer : SendOption
    {
        readonly TimeSpan timespan;
        readonly DateTimeOffset deliveryDate;

        private Defer(DateTimeOffset deliveryDate)
        {
            this.deliveryDate = deliveryDate;
        }

        private Defer(TimeSpan timespan)
        {
            this.timespan = timespan;
        }

        public static Defer To(DateTimeOffset deliveryDate)
        {
            return new Defer(deliveryDate);
        }

        public static Defer By(TimeSpan timespan)
        {
            return new Defer(timespan);
        }
    }

    /// <summary>
    /// A session which provides basic message operations.
    /// </summary>
    public interface IMessageSession
    {
        /// <summary>
        /// Sends the provided message.
        /// </summary>
        /// <param name="message">The message to send.</param>
        /// <param name="options">The options for the send.</param>
        Task Send(object message, SendOptions options);

        Task Send(object message, params SendOption[] options);

        /// <summary>
        /// Instantiates a message of type T and sends it.
        /// </summary>
        /// <typeparam name="T">The type of message, usually an interface.</typeparam>
        /// <param name="messageConstructor">An action which initializes properties of the message.</param>
        /// <param name="options">The options for the send.</param>
        Task Send<T>(Action<T> messageConstructor, SendOptions options);

        /// <summary>
        /// Publish the message to subscribers.
        /// </summary>
        /// <param name="message">The message to publish.</param>
        /// <param name="options">The options for the publish.</param>
        Task Publish(object message, PublishOptions options);

        /// <summary>
        /// Instantiates a message of type T and publishes it.
        /// </summary>
        /// <typeparam name="T">The type of message, usually an interface.</typeparam>
        /// <param name="messageConstructor">An action which initializes properties of the message.</param>
        /// <param name="publishOptions">Specific options for this event.</param>
        Task Publish<T>(Action<T> messageConstructor, PublishOptions publishOptions);

        /// <summary>
        /// Subscribes to receive published messages of the specified type.
        /// This method is only necessary if you turned off auto-subscribe.
        /// </summary>
        /// <param name="eventType">The type of event to subscribe to.</param>
        /// <param name="options">Options for the subscribe.</param>
        Task Subscribe(Type eventType, SubscribeOptions options);

        /// <summary>
        /// Unsubscribes to receive published messages of the specified type.
        /// </summary>
        /// <param name="eventType">The type of event to unsubscribe to.</param>
        /// <param name="options">Options for the subscribe.</param>
        Task Unsubscribe(Type eventType, UnsubscribeOptions options);
    }
}