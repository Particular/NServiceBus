namespace NServiceBus.Unicast.BackwardCompatibility
{
    using System;
    using MessageMutator;
    using Timeout;

    [ObsoleteEx(RemoveInVersion = "5.0", TreatAsErrorFromVersion = "5.0", Message = "Exist only for compatibility between V4 and V3. No longer needed in V5")]
    public class SetIsSagaMessageHeaderForV3XMessages : IMutateIncomingMessages, INeedInitialization
    {
        public IBus Bus { get; set; }

        public object MutateIncoming(object message)
        {
            var version = Bus.GetMessageHeader(message, Headers.NServiceBusVersion);

            if (string.IsNullOrEmpty(version))
                return message;

            if (!version.StartsWith("3"))
                return message;

            if (!string.IsNullOrEmpty(Bus.GetMessageHeader(message, Headers.IsSagaTimeoutMessage)))
                return message;

            //make sure that this a timeout of any kind
            if (string.IsNullOrEmpty(Bus.GetMessageHeader(message, TimeoutManagerHeaders.Expire)))
                return message;

            //if the message has a target saga id on it this must be a saga timeout
            if (string.IsNullOrEmpty(Bus.GetMessageHeader(message, Headers.SagaId)))
                return message;

            // this is a little bit of a hack since we can change headers on applicative messages on the fly
            // but this will work since saga timeouts will never be bundled with other messages
            Bus.CurrentMessageContext.Headers[Headers.IsSagaTimeoutMessage] = Boolean.TrueString;

            return message;
        }

        public void Init()
        {
            Configure.Instance.Configurer.ConfigureComponent<SetIsSagaMessageHeaderForV3XMessages>(DependencyLifecycle.InstancePerCall);
        }
    }
}