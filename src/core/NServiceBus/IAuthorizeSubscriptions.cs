using System.Collections.Generic;

namespace NServiceBus
{
    /// <summary>
    /// Implementer will be called by the infrastructure in order to authorize
    /// subscribe and unsubscribe requests from other endpoints.
    /// </summary>
    public interface IAuthorizeSubscriptions
    {
        /// <summary>
        /// Return true if the client endpoint is to be allowed to subscribe to the given message type.
        /// </summary>
        /// <param name="messageType"></param>
        /// <param name="clientEndpoint"></param>
        /// <param name="clientWindowsIdentity"></param>
        /// <param name="headers"></param>
        /// <returns></returns>
        bool AuthorizeSubscribe(string messageType, string clientEndpoint, string clientWindowsIdentity,
                                IDictionary<string, string> headers);

        /// <summary>
        /// Return true if the client endpoint is to be allowed to unsubscribe to the given message type.
        /// </summary>
        /// <param name="messageType"></param>
        /// <param name="clientEndpoint"></param>
        /// <param name="clientWindowsIdentity"></param>
        /// <param name="headers"></param>
        /// <returns></returns>
        bool AuthorizeUnsubscribe(string messageType, string clientEndpoint, string clientWindowsIdentity,
                        IDictionary<string, string> headers);

    }
}
