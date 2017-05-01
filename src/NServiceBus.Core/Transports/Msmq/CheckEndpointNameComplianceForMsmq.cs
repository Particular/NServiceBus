namespace NServiceBus.Transports.Msmq
{
    using System;

    class CheckEndpointNameComplianceForMsmq
    {
        public void Check(string endpointName)
        {
            // .NET Messaging API hardcodes the buffer size to 124. As a result, the entire format name of the queue cannot exceed 123
            var formatName = $"DIRECT=OS:{Environment.MachineName}\\private$\\{endpointName}";
            if (formatName.Length > 123)
            {
                throw new InvalidOperationException($"The specified endpoint name {endpointName} is too long. The format name for the endpoint:{formatName} needs to be 123 characters or less.");
            }
        }
    }
}
