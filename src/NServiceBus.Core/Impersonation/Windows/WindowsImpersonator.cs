namespace NServiceBus.Impersonation.Windows
{
    using System.Security.Principal;

    /// <summary>
    /// Impersonates the client if needed
    /// </summary>
    [ObsoleteEx(
        Message = "The impersonation feature has been removed due to confusion of it being a security feature." +
                  "Once you stop using this feature the Thread.CurrentPrincipal will no longer be set to a fake principal containing the username. However you can still get access to that information using the message headers.",
        Replacement = "message.GetHeader(Headers.WindowsIdentityName)",
        RemoveInVersion = "5.0",
        TreatAsErrorFromVersion = "5.0")]
    public class WindowsImpersonator : ExtractIncomingPrincipal
    {
        public IPrincipal GetPrincipal(TransportMessage message)
        {
            if (!message.Headers.ContainsKey(Headers.WindowsIdentityName))
            {
                return null;
            }

            var name = message.Headers[Headers.WindowsIdentityName];

            if (name == null)
            {
                return null;
            }

            return new GenericPrincipal(new GenericIdentity(name), new string[0]);
        }
    }
}