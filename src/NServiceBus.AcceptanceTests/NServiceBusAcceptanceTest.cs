namespace NServiceBus.AcceptanceTests;

using System;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
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
    public void SetUp() =>
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
            TestContext.Out.WriteLine($@"Test settings:
{string.Join(Environment.NewLine, runDescriptor.Settings.Select(setting => $"   {setting.Key}: {setting.Value}"))}");

            TestContext.Out.WriteLine($@"Context:
{string.Join(Environment.NewLine, scenarioContext.GetType().GetProperties().Select(p => $"{p.Name} = {ExtractScenarioContextValues(scenarioContext, p)}"))}");
        }

        if (TestExecutionContext.CurrentContext.CurrentResult.ResultState == ResultState.Failure || TestExecutionContext.CurrentContext.CurrentResult.ResultState == ResultState.Error)
        {
            TestContext.Out.WriteLine(string.Empty);
            TestContext.Out.WriteLine($"Log entries (log level: {scenarioContext.LogLevel}):");
            TestContext.Out.WriteLine("--- Start log entries ---------------------------------------------------");
            foreach (var logEntry in scenarioContext.Logs)
            {
                TestContext.Out.WriteLine($"{logEntry.Timestamp:T} {logEntry.Level} {logEntry.Endpoint ?? TestContext.CurrentContext.Test.Name}: {logEntry.Message}");
            }
            TestContext.Out.WriteLine("--- End log entries ---------------------------------------------------");
        }

        return;

        static object ExtractScenarioContextValues(ScenarioContext scenarioContext, PropertyInfo property)
        {
            var contextPropertyValue = property.GetValue(scenarioContext, null);
            return contextPropertyValue is TaskCompletionSource tcs ? tcs.Task.IsCompletedSuccessfully : contextPropertyValue;
        }
    }
}