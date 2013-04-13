namespace NServiceBus.Impersonation
{
    using System.Security.Principal;

    /// <summary>
    /// Allows different authentication techniques to be plugged in.
    /// </summary>
    public interface ExtractIncomingPrincipal
    {
        /// <summary>
        /// Gets the principal of the client to be used when handling the message
        /// </summary>
        /// <returns></returns>
        IPrincipal GetPrincipal(TransportMessage message);
    }
}