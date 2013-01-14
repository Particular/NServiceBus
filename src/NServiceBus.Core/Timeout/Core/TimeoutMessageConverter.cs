namespace NServiceBus.Timeout.Core
{
    using System.Globalization;
    using MessageMutator;
    using Saga;

    /// <summary>
    /// Timeout converter.
    /// </summary>
    public class TimeoutMessageConverter : IMessageMutator, INeedInitialization
    {
        /// <summary>
        /// The bus.
        /// </summary>
        public IBus Bus { get; set; }

        /// <summary>
        /// Set the headers for outgoing messages to make sure that the 3.0 timeout manager will work correctly
        /// </summary>
        /// <param name="message"></param>
        public object MutateOutgoing(object message)
        {
            var timeoutMessage = message as TimeoutMessage;

            if (timeoutMessage == null)
                return message;

            Headers.SetMessageHeader(message, Headers.SagaId, timeoutMessage.SagaId.ToString());
            Headers.SetMessageHeader(message, TimeoutManagerHeaders.Expire, DateTimeExtensions.ToWireFormattedString(timeoutMessage.Expires));

            if (timeoutMessage.ClearTimeout)
                Headers.SetMessageHeader(message, TimeoutManagerHeaders.ClearTimeouts, true.ToString(CultureInfo.InvariantCulture));

            return message;
        }

        /// <summary>
        /// We set the values for incoming messages as well so that we are backwards compatible with 2.6 timeouts
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public object MutateIncoming(object message)
        {
            var timeoutMessage = message as TimeoutMessage;

            if (timeoutMessage == null)
                return message;

            Bus.CurrentMessageContext.Headers[Headers.SagaId] = timeoutMessage.SagaId.ToString();
            Bus.CurrentMessageContext.Headers[TimeoutManagerHeaders.Expire] = DateTimeExtensions.ToWireFormattedString(timeoutMessage.Expires);

            if (timeoutMessage.ClearTimeout)
                Bus.CurrentMessageContext.Headers[TimeoutManagerHeaders.ClearTimeouts] = true.ToString(CultureInfo.InvariantCulture);

            return message;
        }

        /// <summary>
        /// Implementers will include custom initialization code here.
        /// </summary>
        public void Init()
        {
            Configure.Instance.Configurer.ConfigureComponent<TimeoutMessageConverter>(DependencyLifecycle.InstancePerCall);
        }
    }
}
