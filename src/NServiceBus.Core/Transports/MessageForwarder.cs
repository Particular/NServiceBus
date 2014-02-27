namespace NServiceBus.Transports
{
    using System;

    static class MessageForwarder
    {
        public static void ForwardMessage(this ISendMessages messageSender,  TransportMessage transportMessage, TimeSpan timeToBeReceived, Address address)
        {
            // Revert the original body if needed (if any mutators were applied, forward the original body as received)
            transportMessage.RevertToOriginalBodyIfNeeded();

            // Create a new transport message which will contain the appropriate headers
            var messageToForward = new TransportMessage(transportMessage.Id, transportMessage.Headers)
                                   {
                                       Body = transportMessage.Body,
                                       CorrelationId = transportMessage.CorrelationId,
                                       MessageIntent = transportMessage.MessageIntent,
                                       Recoverable = transportMessage.Recoverable,
                                       ReplyToAddress = Address.Local,
                                       TimeToBeReceived = timeToBeReceived == TimeSpan.Zero ? transportMessage.TimeToBeReceived : timeToBeReceived
                                   };
            if (transportMessage.ReplyToAddress != null)
            {
                messageToForward.Headers[Headers.OriginatingAddress] = transportMessage.ReplyToAddress.ToString();
            }

            // Send the newly created transport message to the queue
            messageSender.Send(messageToForward, address);
        }
    }
}