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
            var fullNameSpan = t.FullName.AsSpan();
            var lastDot = fullNameSpan.LastIndexOf('.');
            var classAndEndpointSpan = lastDot >= 0 ? fullNameSpan[(lastDot + 1)..] : fullNameSpan;

            var plusIndex = classAndEndpointSpan.IndexOf('+');
            var testNameSpan = plusIndex >= 0 ? classAndEndpointSpan[..plusIndex] : classAndEndpointSpan;
            var endpointBuilderSpan = plusIndex >= 0 ? classAndEndpointSpan[(plusIndex + 1)..] : ReadOnlySpan<char>.Empty;

            if (testNameSpan.StartsWith("When_"))
            {
                testNameSpan = testNameSpan[5..];
            }

            var testName = Thread.CurrentThread.CurrentCulture.TextInfo.ToTitleCase(testNameSpan.ToString());
            testName = testName.Replace("_", "");

            var endpointBuilder = endpointBuilderSpan.Length > 0 ? endpointBuilderSpan.ToString() : string.Empty;

            return $"{testName}.{endpointBuilder}";
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