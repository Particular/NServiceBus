namespace NServiceBus
{
    using System;
    using System.Collections.Generic;

    interface IRecoverabilityPolicy
    {
        RecoveryAction Invoke(Exception exception, Dictionary<string, string> headers, int numberOfProcessingAttempts, Dictionary<string, string> metadata);
    }
}