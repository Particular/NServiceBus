namespace VideoStore.Common
{
    using System;
    using System.Threading;
    using NServiceBus;
    using NServiceBus.MessageMutator;

    public class DebugFlagMutator : IMutateTransportMessages, INeedInitialization
    {
        public static bool Debug { get { return debug.Value; } }

        public void MutateIncoming(TransportMessage transportMessage)
        {
            var debugFlag = transportMessage.Headers.ContainsKey("Debug") ? transportMessage.Headers["Debug"] : "false";
            if (debugFlag !=null && debugFlag.Equals("true", StringComparison.OrdinalIgnoreCase))
            {
                debug.Value = true;
            }
            else
            {
                debug.Value = false;
            }
        }

        public void MutateOutgoing(object[] messages, TransportMessage transportMessage)
        {
            transportMessage.Headers["Debug"] = Debug.ToString();
        }

        public void Init()
        {
            Configure.Instance.Configurer.ConfigureComponent<DebugFlagMutator>(DependencyLifecycle.InstancePerCall);
        }

        static readonly ThreadLocal<bool> debug = new ThreadLocal<bool>();
    }
}
