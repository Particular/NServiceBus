namespace NServiceBus.Impersonation
{
    using System.Security.Principal;

    /// <summary>
    /// Allows different authentication techniques to be plugged in.
    /// </summary>
    [ObsoleteEx(
        Message = "The impersonation feature has been removed due to confusion of it being a security feature." +
                  "Once you stop using this feature the Thread.CurrentPrincipal will no longer be set to a fake principal containing the username. However you can still get access to that information using the message headers.",
        Replacement = "message.GetHeader(Headers.WindowsIdentityName)",
        RemoveInVersion = "5.0",
        TreatAsErrorFromVersion = "4.3")]
    public interface ExtractIncomingPrincipal
    {
        /// <summary>
        /// Gets the principal of the client to be used when handling the message
        /// </summary>
        IPrincipal GetPrincipal(TransportMessage message);
    }
}