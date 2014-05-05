namespace NServiceBus.Transports.Msmq
{
    using System;
    using MessageMutator;

    [ObsoleteEx(RemoveInVersion ="5.0")]
    public class CorrelationIdMutatorForBackwardsCompatibilityWithV3 : IMutateOutgoingTransportMessages
    {
        public void MutateOutgoing(object message, TransportMessage transportMessage)
        {
            if (transportMessage.Headers.ContainsKey(CorrIdHeader))
                return;

            var correlationIdToUse = transportMessage.CorrelationId;
            Guid correlationId;

            if (Guid.TryParse(correlationIdToUse, out correlationId))
            {
                //msmq requires the id's to be in the {guid}\{incrementing number} format so we need to fake a \0 at the end to make it compatible                
                correlationIdToUse += "\\0";
            }
            transportMessage.Headers[CorrIdHeader] = correlationIdToUse;
        }

        static string CorrIdHeader = "CorrId";
    }
}