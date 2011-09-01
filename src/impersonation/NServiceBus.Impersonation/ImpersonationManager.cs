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
    public class ImpersonationManager : INeedInitialization, IMutateOutgoingTransportMessages
    {
        void INeedInitialization.Init()
        {
            NServiceBus.Configure.Instance.Configurer.ConfigureComponent<ImpersonationManager>(DependencyLifecycle.SingleInstance);

            Configure.ConfigurationComplete +=
                () =>
                    {
                        Configure.Instance.Builder.Build<ITransport>().TransportMessageReceived +=
                            Transport_TransportMessageReceived;
                    };
        }

        static void Transport_TransportMessageReceived(object sender, TransportMessageReceivedEventArgs e)
        {
            if (ConfigureImpersonation.Impersonate)
                if (e.Message.Headers.ContainsKey(WindowsIdentityName))
                    Thread.CurrentPrincipal = new GenericPrincipal(new GenericIdentity(e.Message.Headers[WindowsIdentityName]), new string[0]);
        }

        void IMutateOutgoingTransportMessages.MutateOutgoing(object[] messages, TransportMessage transportMessage)
        {
			if (transportMessage.Headers.ContainsKey(WindowsIdentityName))
				transportMessage.Headers.Remove(WindowsIdentityName);

            transportMessage.Headers.Add(WindowsIdentityName, Thread.CurrentPrincipal.Identity.Name);
        }

        private const string WindowsIdentityName = "WinIdName";
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
        /// Impersonate by default, otherwise this configuration would not be backward compatible
        /// </summary>
		static ConfigureImpersonation()
		{
			Impersonate = true;
		}
		
        /// <summary>
        /// Instructs the bus to run the processing of messages being handled
        /// under the permissions of the sender of the message.
        /// </summary>
        /// <param name="config"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static ConfigUnicastBus ImpersonateSender(this ConfigUnicastBus config, bool value)
        {
            Impersonate = value;

            return config;
        }

        public static bool Impersonate { get; private set; }
    }
}
