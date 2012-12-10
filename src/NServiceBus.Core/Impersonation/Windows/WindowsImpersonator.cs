namespace NServiceBus.Impersonation.Windows
{
    using System.Security.Principal;

    /// <summary>
    /// Ímpersonates the client if needed
    /// </summary>
    public class WindowsImpersonator : IImpersonateClients
    {
        public IPrincipal GetPrincipal(TransportMessage message)
        {
            if (!message.Headers.ContainsKey(Headers.WindowsIdentityName))
                return null;

            var name = message.Headers[Headers.WindowsIdentityName];

            if (name == null)
                return null;

            return new GenericPrincipal(new GenericIdentity(name), new string[0]);
        }
    }
}