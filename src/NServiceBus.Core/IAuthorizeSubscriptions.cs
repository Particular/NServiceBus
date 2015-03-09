namespace NServiceBus
{
    using System.Collections.Generic;
    using System.Security.Principal;
    using JetBrains.Annotations;

    /// <summary>
    /// Implementer will be called by the infrastructure in order to authorize
    /// subscribe and unsubscribe requests from other endpoints.
    /// 
    /// Infrastructure automatically registers one implementing type in the container as a singleton.
    /// </summary>
    [UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
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
