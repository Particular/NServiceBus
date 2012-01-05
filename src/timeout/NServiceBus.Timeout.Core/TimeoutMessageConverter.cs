namespace NServiceBus.Timeout.Core
{
    using System.Globalization;
    using Config;
    using MessageMutator;
    using Saga;

    public class TimeoutMessageConverter : IMutateOutgoingMessages,INeedInitialization
    {
        public object MutateOutgoing(object message)
        {
            var timeoutMessage = message as TimeoutMessage;

            if (timeoutMessage != null)
            {
                message.SetHeader(Headers.Expire, timeoutMessage.Expires.ToWireFormattedString());

                if (timeoutMessage.ClearTimeout)
                    message.SetHeader(Headers.ClearTimeouts, true.ToString(CultureInfo.InvariantCulture));
            }

            return message;
        }

        public void Init()
        {
            Configure.Instance.Configurer.ConfigureComponent<TimeoutMessageConverter>(DependencyLifecycle.InstancePerCall);
        }
    }
}