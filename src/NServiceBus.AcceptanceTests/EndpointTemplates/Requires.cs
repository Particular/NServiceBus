namespace NServiceBus.AcceptanceTests
{
    using EndpointTemplates;
    using NUnit.Framework;

    static class Requires
    {
        static readonly TestSuiteConstraints constraints = new TestSuiteConstraints();

        public static void DtcSupport()
        {
            if (!constraints.SupportDtc)
            {
                Assert.Ignore("Ignoring this test because it requires DTC transaction support from the transport.");
            }
        }
    }
}