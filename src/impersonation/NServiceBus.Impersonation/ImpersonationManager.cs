using System;
using System.Security.Principal;
using System.Threading;
using NServiceBus.Config;
using NServiceBus.Impersonation;
using NServiceBus.MessageMutator;
using NServiceBus.ObjectBuilder;
using NServiceBus.Unicast.Config;
using NServiceBus.Unicast.Transport;

namespace NServiceBus.Impersonation
{
    /// <summary>
    /// Manages all aspects of impersonation
    /// </summary>
    public class ImpersonationManager : INeedInitialization, IMapOutgoingTransportMessages
    {
        /// <summary>
        /// Injected transport
        /// </summary>
        public ITransport Transport { get; set; }

        /// <summary>
        /// Configuration on whether incoming messages should be processed using the permissions
        /// of the client who sent those messages.
        /// </summary>
        public virtual bool ImpersonateSender { get; set; }

        void INeedInitialization.Init()
        {
            Transport.TransportMessageReceived += Transport_TransportMessageReceived;
        }

        void Transport_TransportMessageReceived(object sender, TransportMessageReceivedEventArgs e)
        {
            if (e.Message.Headers.ContainsKey(WINDOWSIDENTITYNAME))
                Thread.CurrentPrincipal = ImpersonateSender ? new GenericPrincipal(new GenericIdentity(e.Message.Headers[WINDOWSIDENTITYNAME]), new string[0]) : null;
        }

        void IMapOutgoingTransportMessages.MapOutgoing(IMessage[] messages, TransportMessage transportMessage)
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
            config.Configurer.ConfigureComponent<ImpersonationManager>(ComponentCallModelEnum.Singleton)
                .ConfigureProperty(m => m.ImpersonateSender, value);

            return config;
        }
    }
}
