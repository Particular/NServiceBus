namespace NServiceBus
{
    using System;
    using System.ServiceModel;
    using Unicast;

    /// <summary>
    /// Generic WCF service for exposing a messaging endpoint.
    /// </summary>
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.PerCall, 
                     ConcurrencyMode = ConcurrencyMode.Multiple)]
    public abstract class WcfService<TRequest, TResponse> : IWcfService<TRequest, TResponse>
    {
        /// <summary>
        /// Create an instance of <see cref="WcfService{TRequest,TResponse}"/>
        /// </summary>
        protected WcfService()
        {
            bus = Configure.Instance.Builder.Build<IBus>();   
        }

        static WcfService()
        {
            if (!typeof(TResponse).IsEnum)
                throw new InvalidOperationException(typeof(TResponse).FullName + " must be an enum representing error codes returned by the server.");
        }

        IAsyncResult IWcfService<TRequest, TResponse>.BeginProcess(TRequest request, AsyncCallback callback, object state)
        {
            var result = new ServiceAsyncResult(state);

            return ((UnicastBus) bus).SendLocal(request).Register(r => ProxyCallback(callback, result, r), state);
        }

        TResponse IWcfService<TRequest, TResponse>.EndProcess(IAsyncResult asyncResult)
        {
            var completionResult = ((ServiceAsyncResult) asyncResult).Result;

            if (completionResult == null)
                throw new InvalidOperationException("Response returned from server did not contain a CompletionResult.");

            return (TResponse)Enum.ToObject(typeof(TResponse), completionResult.ErrorCode);
        }

        private static void ProxyCallback(AsyncCallback callback, ServiceAsyncResult result, IAsyncResult busResult)
        {
            var completionResult = (CompletionResult)busResult.AsyncState;

            result.Complete(completionResult);

            callback(result);
        }

        private readonly IBus bus;
    }
}
