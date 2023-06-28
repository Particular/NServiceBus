namespace NServiceBus.AcceptanceTests
{
    using System;
    using System.Linq;
    using System.Threading;
    using AcceptanceTesting;
    using AcceptanceTesting.Customization;
    using NUnit.Framework;
    using NUnit.Framework.Interfaces;
    using NUnit.Framework.Internal;

    /// <summary>
    /// Base class for all the NSB test that sets up our conventions
    /// </summary>
    [TestFixture]
    public abstract partial class NServiceBusAcceptanceTest
    {
        [SetUp]
        public void SetUp()
        {
#if NETFRAMEWORK
            // Hack: prevents SerializationException ... Type 'x' in assembly 'y' is not marked as serializable.
            // https://docs.microsoft.com/en-us/dotnet/framework/migration-guide/mitigation-deserialization-of-objects-across-app-domains
            System.Configuration.ConfigurationManager.GetSection("X");
#endif
            Conventions.EndpointNamingConvention = t =>
            {
                var classAndEndpoint = t.FullName.Split('.').Last();

                var testName = classAndEndpoint.Split('+').First();

                testName = testName.Replace("When_", "");

                var endpointBuilder = classAndEndpoint.Split('+').Last();

                testName = Thread.CurrentThread.CurrentCulture.TextInfo.ToTitleCase(testName);

                testName = testName.Replace("_", "");

                return testName + "." + endpointBuilder;
            };
        }

        [TearDown]
        public void TearDown()
        {
            if (!TestExecutionContext.CurrentContext.TryGetRunDescriptor(out var runDescriptor))
            {
                return;
            }

            var scenarioContext = runDescriptor.ScenarioContext;

            if (Environment.GetEnvironmentVariable("CI") != "true" || Environment.GetEnvironmentVariable("VERBOSE_TEST_LOGGING")?.ToLower() == "true")
            {
                TestContext.WriteLine($@"Test settings:
{string.Join(Environment.NewLine, runDescriptor.Settings.Select(setting => $"   {setting.Key}: {setting.Value}"))}");

                TestContext.WriteLine($@"Context:
{string.Join(Environment.NewLine, scenarioContext.GetType().GetProperties().Select(p => $"{p.Name} = {p.GetValue(scenarioContext, null)}"))}");
            }

            if (TestExecutionContext.CurrentContext.CurrentResult.ResultState == ResultState.Failure || TestExecutionContext.CurrentContext.CurrentResult.ResultState == ResultState.Error)
            {
                TestContext.WriteLine(string.Empty);
                TestContext.WriteLine($"Log entries (log level: {scenarioContext.LogLevel}):");
                TestContext.WriteLine("--- Start log entries ---------------------------------------------------");
                foreach (var logEntry in scenarioContext.Logs)
                {
                    TestContext.WriteLine($"{logEntry.Timestamp:T} {logEntry.Level} {logEntry.Endpoint ?? TestContext.CurrentContext.Test.Name}: {logEntry.Message}");
                }
                TestContext.WriteLine("--- End log entries ---------------------------------------------------");
            }
        }
    }
}
