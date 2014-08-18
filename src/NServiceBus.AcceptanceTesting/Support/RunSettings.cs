namespace NServiceBus.AcceptanceTesting.Support
{
    using System;

    public class RunSettings
    {
        public TimeSpan? TestExecutionTimeout;
        public bool UseSeparateAppDomains;
    }
}