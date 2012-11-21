namespace NServiceBus.Impersonation
{
    using System.Security.Principal;
    using System.Threading;
    using MessageMutator;
    using Unicast.Transport;

    /// <summary>
    /// Manages all aspects of impersonation
    /// </summary>
    public class ImpersonationManager : INeedInitialization, IMutateOutgoingTransportMessages
    {
        void INeedInitialization.Init()
        {
            Configure.Instance.Configurer.ConfigureComponent<ImpersonationManager>(DependencyLifecycle.SingleInstance);

            Configure.ConfigurationComplete +=
                () =>
                {
                    Configure.Instance.Builder.Build<ITransport>().TransportMessageReceived +=
                        Transport_TransportMessageReceived;
                };
        }

        static void Transport_TransportMessageReceived(object sender, TransportMessageReceivedEventArgs e)
        {
            if (!ConfigureImpersonation.Impersonate)
                return;

            if (!e.Message.Headers.ContainsKey(Headers.WindowsIdentityName))
                return;

            var name = e.Message.Headers[Headers.WindowsIdentityName];

            if (name == null)
                return;

            Thread.CurrentPrincipal = new GenericPrincipal(new GenericIdentity(name), new string[0]);
        }

        void IMutateOutgoingTransportMessages.MutateOutgoing(object[] messages, TransportMessage transportMessage)
        {

            if (transportMessage.Headers.ContainsKey(Headers.WindowsIdentityName))
                transportMessage.Headers.Remove(Headers.WindowsIdentityName);

            transportMessage.Headers.Add(Headers.WindowsIdentityName, Thread.CurrentPrincipal.Identity.Name);
        }
    }
}

namespace NServiceBus
{
}
