namespace NServiceBus.AcceptanceTesting;

using System.Diagnostics.CodeAnalysis;
using NUnit.Framework.Internal;
using Support;

public static class TestContextExtensions
{
    static readonly string SettingsKey = typeof(RunDescriptor).FullName!;

    extension(TestExecutionContext testContext)
    {
        public void AddRunDescriptor(RunDescriptor runDescriptor) => testContext.CurrentTest.Properties.Add(SettingsKey, runDescriptor);

        public bool TryGetRunDescriptor([NotNullWhen(true)] out RunDescriptor? runDescriptor)
        {
            if (testContext.CurrentTest.Properties.ContainsKey(SettingsKey))
            {
                runDescriptor = testContext.CurrentTest.Properties.Get(SettingsKey) as RunDescriptor;
                return runDescriptor != null;
            }

            runDescriptor = null;
            return false;
        }
    }
}