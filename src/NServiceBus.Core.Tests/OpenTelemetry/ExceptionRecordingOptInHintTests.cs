namespace NServiceBus.Core.Tests.OpenTelemetry;

using System.IO;
using System.Text;
using NServiceBus.Logging;
using NUnit.Framework;
using Settings;
using Testing;

// The hint and the code under test live in obsoletes-v10.cs. Delete this file in v11 together with them.
[TestFixture]
public class ExceptionRecordingOptInHintTests
{
    static readonly StringBuilder logStatements = new();

    [OneTimeSetUp]
    public void LoggerSetup()
    {
#pragma warning disable CS0618 // Use<T> and TestingLoggerFactory (via LoggingFactoryDefinition) are deprecated; test setup uses them intentionally
        LogManager.Use<TestingLoggerFactory>()
            .WriteTo(new StringWriter(logStatements));
#pragma warning restore CS0618
    }

    [TearDown]
    public void TearDown() => logStatements.Clear();

    [TestCase(null)]
    [TestCase("not-a-recognized-value")]
    public void Should_log_hint_when_mode_is_not_configured(string value)
    {
        var options = SetDefaults(new FakeEnvironment(value));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(logStatements.ToString(), Does.Contain(InstrumentationOptions.ExceptionRecordingOptInHint));
            Assert.That(options.IsExceptionRecordingModeSet, Is.False);
            Assert.That(options.ExceptionRecordingMode, Is.EqualTo(ExceptionRecordingMode.SpanAndLogs));
        }
    }

    [TestCase(ExceptionRecordingMode.SpanAndLogs)]
    [TestCase(ExceptionRecordingMode.Logs)]
    public void Should_not_log_hint_when_mode_is_configured_in_code(ExceptionRecordingMode mode)
    {
        SetDefaults(new FakeEnvironment(null), new InstrumentationOptions { ExceptionRecordingMode = mode });

        Assert.That(logStatements.ToString(), Does.Not.Contain(InstrumentationOptions.ExceptionRecordingOptInHint));
    }

    [TestCase("logs")]
    [TestCase("logs/dup")]
    public void Should_not_log_hint_when_mode_is_set_by_environment_variable(string value)
    {
        SetDefaults(new FakeEnvironment(value));

        Assert.That(logStatements.ToString(), Does.Not.Contain(InstrumentationOptions.ExceptionRecordingOptInHint));
    }

    static InstrumentationOptions SetDefaults(SystemEnvironment environment, InstrumentationOptions options = null)
    {
        var settingsHolder = new SettingsHolder();
        settingsHolder.Set(environment);
        if (options != null)
        {
            settingsHolder.Set(options);
        }

        InstrumentationOptions.SetExceptionRecordingModeDefault(settingsHolder);

        return settingsHolder.Get<InstrumentationOptions>();
    }

    class FakeEnvironment(string optInValue) : SystemEnvironment
    {
        public override string GetEnvironmentVariable(string variable) =>
            variable == InstrumentationOptions.ExceptionSignalOptInEnvironmentVariableKey ? optInValue : null;
    }
}
