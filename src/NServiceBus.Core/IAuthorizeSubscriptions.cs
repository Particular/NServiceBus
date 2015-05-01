namespace NServiceBus
{
    using System.Collections.Generic;
    using System.Security.Principal;

    /// <summary>
    /// Implementer will be called by the infrastructure in order to authorize
    /// subscribe and unsubscribe requests from other endpoints.
    /// 
    /// Infrastructure automatically registers one implementing type in the container as a singleton.
    /// </summary>
    [ObsoleteEx(RemoveInVersion = "7.0", TreatAsErrorFromVersion = "6.0", Message = "This feature has been removed. To support authorization on a per subscription message type use a pipeline step instead.")]
    public interface IAuthorizeSubscriptions
    {
        /// <summary>
        /// Return true if the client endpoint is to be allowed to subscribe to the given message type.
        /// Implementors can access the impersonated user via <see cref="WindowsIdentity.GetCurrent()"/>
        /// </summary>
        bool AuthorizeSubscribe(string messageType, string clientEndpoint, IDictionary<string, string> headers);

        /// <summary>
        /// Return true if the client endpoint is to be allowed to unsubscribe to the given message type.
        /// </summary>
        bool AuthorizeUnsubscribe(string messageType, string clientEndpoint, IDictionary<string, string> headers);
    }
}
