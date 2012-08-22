namespace NServiceBus.Timeout.Core.Dispatch
{
    using System;
    using System.Collections.Generic;
    using Unicast.Queuing;
    using Unicast.Transport;

    public class TimeoutDispatcher
    {
        public ISendMessages MessageSender { get; set; }
        public Address TimeoutManagerAddress { get; set; }

        public void DispatchTimeout(TimeoutData timeoutData)
        {
            var dispatchRequest = MapToTransportMessage(timeoutData);

            dispatchRequest.Headers[TimeoutIdToDispatchHeader] = timeoutData.Id;
            dispatchRequest.Headers[TimeoutDestinationHeader] = timeoutData.Destination.ToString();

            MessageSender.Send(dispatchRequest, TimeoutManagerAddress);
        }

        static TransportMessage MapToTransportMessage(TimeoutData timeoutData)
        {
            var replyToAddress = Address.Local;

            if(timeoutData.Headers.ContainsKey(TimeoutTransportMessageHandler.OriginalReplyToAddress))
            {
                replyToAddress =
                    Address.Parse(timeoutData.Headers[TimeoutTransportMessageHandler.OriginalReplyToAddress]);

                timeoutData.Headers.Remove(TimeoutTransportMessageHandler.OriginalReplyToAddress);
            }

            var transportMessage = new TransportMessage
            {
                ReplyToAddress = replyToAddress,
                Headers = new Dictionary<string, string>(),
                Recoverable = true,
                MessageIntent = MessageIntentEnum.Send,
                CorrelationId = timeoutData.CorrelationId,
                Body = timeoutData.State
            };

            if (timeoutData.Headers != null)
            {
                transportMessage.Headers = timeoutData.Headers;
            }
            else
            {
                //we do this to be backwards compatible, this can be removed when going to 3.1.X
                transportMessage.Headers[Headers.Expire] = timeoutData.Time.ToWireFormattedString();

                if (timeoutData.SagaId != Guid.Empty)
                    transportMessage.Headers[Headers.SagaId] = timeoutData.SagaId.ToString();

            }

            return transportMessage;
        }


        public static string TimeoutIdToDispatchHeader = "NServiceBus.Timeout.TimeoutIdToDispatch";
        public static string TimeoutDestinationHeader = "NServiceBus.Timeout.Destination";
    }
}