namespace NServiceBus
{
    using System;
    using System.ServiceModel;

    /// <summary>
    /// Service interface for a generic WCF adapter to a messaging endpoint.
    /// </summary>
    /// <typeparam name="TRequest"></typeparam>
    /// <typeparam name="TResponse"></typeparam>
    [ServiceContract(Namespace = "http://nservicebus.com")]
    public interface IWcfService<TRequest, TResponse>
    {
        /// <summary>
        /// Sends the message to the messaging endpoint.
        /// </summary>
        /// <param name="request"></param>
        /// <param name="callback"></param>
        /// <param name="state"></param>
        /// <returns></returns>
        [OperationContract(AsyncPattern = true)]
        IAsyncResult BeginProcess(TRequest request, AsyncCallback callback, object state);

        /// <summary>
        /// Returns the result received from the messaging endpoint.
        /// </summary>
        /// <param name="asyncResult"></param>
        /// <returns></returns>
        TResponse EndProcess(IAsyncResult asyncResult);
    }
}
