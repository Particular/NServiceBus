using System;
using System.Collections.Generic;
using System.Security.Principal;

namespace NServiceBus
{
    /// <summary>
    /// Implementer will be called by the infrastructure in order to authorize
    /// subscribe and unsubscribe requests from other endpoints.
    /// 
    /// Infrastructure automatically registers one implementing type in the container as a singleton.
    /// </summary>
    public interface IAuthorizeSubscriptions
    {
        /// <summary>
        /// Return true if the client endpoint is to be allowed to subscribe to the given message type.
        /// Implementors can access the impersonated user via <see cref="WindowsIdentity.GetCurrent()"/>
        /// </summary>
        /// <param name="messageType"></param>
        /// <param name="clientEndpoint"></param>
        /// <param name="headers"></param>
        /// <returns></returns>
        [Obsolete("Use the overload which accepts Address instead.", true)]
        bool AuthorizeSubscribe(string messageType, string clientEndpoint, IDictionary<string, string> headers);

        /// <summary>
        /// Return true if the client endpoint is to be allowed to subscribe to the given message type.
        /// Implementors can access the impersonated user via <see cref="WindowsIdentity.GetCurrent()"/>
        /// </summary>
        /// <param name="messageType"></param>
        /// <param name="clientAddress"></param>
        /// <param name="headers"></param>
        /// <returns></returns>
        bool AuthorizeSubscribe(string messageType, Address clientAddress, IDictionary<string, string> headers);

        /// <summary>
        /// Return true if the client endpoint is to be allowed to unsubscribe to the given message type.
        /// </summary>
        /// <param name="messageType"></param>
        /// <param name="clientEndpoint"></param>
        /// <param name="headers"></param>
        /// <returns></returns>
        [Obsolete("Use the overload which accepts Address instead.", true)]
        bool AuthorizeUnsubscribe(string messageType, string clientEndpoint, IDictionary<string, string> headers);

        /// <summary>
        /// Return true if the client endpoint is to be allowed to unsubscribe to the given message type.
        /// </summary>
        /// <param name="messageType"></param>
        /// <param name="clientAddress"></param>
        /// <param name="headers"></param>
        /// <returns></returns>
        bool AuthorizeUnsubscribe(string messageType, Address clientAddress, IDictionary<string, string> headers);
    }
}
