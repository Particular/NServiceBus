namespace NServiceBus.AcceptanceTesting
{
    using NUnit.Framework.Internal;
    using Support;

    public static class TestContextExtensions
    {
        static readonly string SettingsKey = typeof(RunDescriptor).FullName;

        public static void AddRunDescriptor(this TestExecutionContext testContext, RunDescriptor runDescriptor)
        {
            testContext.CurrentTest.Properties.Add(SettingsKey, runDescriptor);
        }

        public static bool TryGetRunDescriptor(this TestExecutionContext testContext, out RunDescriptor runDescriptor)
        {
            if (testContext.CurrentTest.Properties.ContainsKey(SettingsKey))
            {
                runDescriptor = testContext.CurrentTest.Properties.Get(SettingsKey) as RunDescriptor;
                return true;
            }

            runDescriptor = null;
            return false;
        }
    }
}