namespace NServiceBus
{
    using System;
    using System.ServiceModel;

    /// <summary>
    /// Service interface for a generic WCF adapter to a messaging endpoint.
    /// </summary>
    [ServiceContract(Namespace = "http://nservicebus.com")]
    public interface IWcfService<TRequest, TResponse>
    {
        /// <summary>
        /// Sends the message to the messaging endpoint.
        /// </summary>
        [OperationContract(AsyncPattern = true)]
        IAsyncResult BeginProcess(TRequest request, AsyncCallback callback, object state);

        /// <summary>
        /// Returns the result received from the messaging endpoint.
        /// </summary>
        TResponse EndProcess(IAsyncResult asyncResult);
    }
}
