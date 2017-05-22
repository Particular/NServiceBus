namespace NServiceBus
{
    using Transport;

    class TimeToBeReceivedOverrideChecker
    {
        public static StartupCheckResult Check(bool usingMsmq, bool isTransactional, bool outBoxRunning, bool auditTTBROverridden)
        {
            if (!usingMsmq)
            {
                return StartupCheckResult.Success;
            }

            if (!isTransactional)
            {
                return StartupCheckResult.Success;
            }

            if (outBoxRunning)
            {
                return StartupCheckResult.Success;
            }

            if (auditTTBROverridden)
            {
                return StartupCheckResult.Failed("Setting a custom OverrideTimeToBeReceived for audits is not supported on transactional MSMQ.");
            }

            return StartupCheckResult.Success;
        }
    }
}