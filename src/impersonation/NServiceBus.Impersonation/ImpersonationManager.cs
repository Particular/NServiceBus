using System.Security.Principal;
using System.Threading;
using NServiceBus.Config;
using NServiceBus.MessageMutator;
using NServiceBus.Unicast.Config;
using NServiceBus.Unicast.Transport;

namespace NServiceBus.Impersonation
{
    /// <summary>
    /// Manages all aspects of impersonation
    /// </summary>
    public class ImpersonationManager : INeedInitialization, IMutateOutgoingTransportMessages
    {
        void INeedInitialization.Init()
        {
            Configure.ConfigurationComplete +=
                (o, a) =>
                    {
                        Configure.Instance.Builder.Build<ITransport>().TransportMessageReceived +=
                            Transport_TransportMessageReceived;
                    };
        }

        void Transport_TransportMessageReceived(object sender, TransportMessageReceivedEventArgs e)
        {
            if (e.Message.Headers.ContainsKey(WINDOWSIDENTITYNAME))
                Thread.CurrentPrincipal = ConfigureImpersonation.Impersonate ? new GenericPrincipal(new GenericIdentity(e.Message.Headers[WINDOWSIDENTITYNAME]), new string[0]) : null;
        }

        void IMutateOutgoingTransportMessages.MutateOutgoing(IMessage[] messages, TransportMessage transportMessage)
        {
            transportMessage.Headers.Add(WINDOWSIDENTITYNAME, Thread.CurrentPrincipal.Identity.Name);
        }

        private const string WINDOWSIDENTITYNAME = "WinIdName";
    }
}

namespace NServiceBus
{
    /// <summary>
    /// Contains extension methods for the purpose of configuring impersonation.
    /// </summary>
    public static class ConfigureImpersonation
    {
        /// <summary>
        /// Instructs the bus to run the processing of messages being handled
        /// under the permissions of the sender of the message.
        /// </summary>
        /// <param name="config"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static ConfigUnicastBus ImpersonateSender(this ConfigUnicastBus config, bool value)
        {
            Impersonate = true;

            return config;
        }

        public static bool Impersonate { get; private set; }
    }
}
