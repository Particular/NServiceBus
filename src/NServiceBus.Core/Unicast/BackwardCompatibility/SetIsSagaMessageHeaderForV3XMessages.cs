namespace NServiceBus.Unicast.BackwardCompatibility
{
    using System;
    using MessageMutator;
    using Timeout;

    public class SetIsSagaMessageHeaderForV3XMessages : IMutateIncomingMessages, INeedInitialization
    {
        public IBus Bus { get; set; }

        public object MutateIncoming(object message)
        {
            if (!string.IsNullOrEmpty(Bus.GetMessageHeader(message, Headers.IsSagaTimeoutMessage)))
                return message;

            //make sure that this a timeout of any kind
            if (string.IsNullOrEmpty(Bus.GetMessageHeader(message, TimeoutManagerHeaders.Expire)))
                return message;

            //if this is a real message it can't be a timeout since they are not "messages"
            if (MessageConventionExtensions.IsMessage(message))
                return message;

            Bus.SetMessageHeader(message,Headers.IsSagaTimeoutMessage,Boolean.TrueString);

            return message;
        }

        public void Init()
        {
            Configure.Instance.Configurer.ConfigureComponent<SetIsSagaMessageHeaderForV3XMessages>(DependencyLifecycle.InstancePerCall);
        }
    }
}