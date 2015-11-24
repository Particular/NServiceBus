namespace NServiceBus.Transports.Msmq
{
    using System;

    class TimeToBeReceivedOverrideChecker
    {
        public static void Check(bool usingMsmq, bool isTransactional, bool outBoxRunning, bool auditTTBROverridden, bool forwardTTBROverridden)
        {
            if (!usingMsmq)
            {
                return;
            }

            if (!isTransactional)
            {
                return;
            }

            if (outBoxRunning)
            {
                return;
            }

            if (auditTTBROverridden)
            {
                throw new Exception("Setting a custom OverrideTimeToBeReceived for audits is not supported on transactional MSMQ.");
            }

            if (forwardTTBROverridden)
            {
                throw new Exception("Setting a custom TimeToBeReceivedOnForwardedMessages is not supported on transactional MSMQ.");
            }
        }
    }
}