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
        public static SendContext<TResponse> SynchronousRequestResponse<TResponse>(this IBus bus, object requestMessage)
        {
            Guard.AgainstNull(requestMessage, "requestMessage");
            Guard.AgainstNull(bus, "bus");

            var customId = Guid.NewGuid().ToString();

            bus.Send(requestMessage, new SendOptions()
                .AddHeader("NServiceBus.HasCallback", Boolean.TrueString)
                .SetCustomMessageId(customId));

            var callbackMessageLookup = ((IServiceProvider) bus).GetService<RequestResponseMessageLookup>();
            var tcs = new TaskCompletionSource<TResponse>();

            callbackMessageLookup.RegisterResult(customId, tcs);

            return new SendContext<TResponse>(tcs);
        }
    }
}
