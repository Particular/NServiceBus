namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
    using NServiceBus.Callbacks;

    /// <summary>
    /// 
    /// </summary>
    public static class RequestResponseExtensions
    {
        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="TResponse"></typeparam>
        /// <param name="bus"></param>
        /// <param name="requestMessage"></param>
        /// <returns></returns>
        public static SendContext<TResponse> RequestResponse<TResponse>(this IBus bus, object requestMessage)
        {
            Guard.AgainstNull(requestMessage, "requestMessage");
            Guard.AgainstNull(bus, "bus");

            var correlationId = Guid.NewGuid().ToString();

            bus.SetMessageHeader(requestMessage, "NServiceBus.HasCallback", Boolean.TrueString);
            bus.SetMessageHeader(requestMessage, Headers.CorrelationId, correlationId);
            bus.Send(requestMessage);

            var callbackMessageLookup = ((IServiceProvider) bus).GetService<CallbackMessageLookup>();
            var tcs = new TaskCompletionSource<TResponse>();

            callbackMessageLookup.RegisterResult(correlationId, tcs);

            return new SendContext<TResponse>(tcs);
        }
    }
}
