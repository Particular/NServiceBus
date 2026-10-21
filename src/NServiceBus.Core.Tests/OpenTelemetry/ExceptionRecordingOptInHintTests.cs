namespace NServiceBus.Core.Tests.OpenTelemetry;

using System.IO;
using System.Text;
using NServiceBus.Logging;
using NUnit.Framework;
using Settings;
using Testing;

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
        SetDefaults(new FakeEnvironment(value));

        Assert.That(logStatements.ToString(), Does.Contain(InstrumentationOptions.ExceptionRecordingOptInHint));
    }

    [Test]
    public void Should_not_log_hint_when_mode_is_configured_in_code()
    {
        SetDefaults(new FakeEnvironment(null), new InstrumentationOptions { ExceptionRecordingMode = ExceptionRecordingMode.SpanAndLogs });

        Assert.That(logStatements.ToString(), Does.Not.Contain(InstrumentationOptions.ExceptionRecordingOptInHint));
    }

    [TestCase("logs")]
    [TestCase("logs/dup")]
    public void Should_not_log_hint_when_mode_is_set_by_environment_variable(string value)
    {
        SetDefaults(new FakeEnvironment(value));

        Assert.That(logStatements.ToString(), Does.Not.Contain(InstrumentationOptions.ExceptionRecordingOptInHint));
    }

    static void SetDefaults(SystemEnvironment environment, InstrumentationOptions options = null)
    {
        var settingsHolder = new SettingsHolder();
        settingsHolder.Set(environment);
        if (options != null)
        {
            settingsHolder.Set(options);
        }

        InstrumentationOptions.SetExceptionRecordingModeDefault(settingsHolder);
    }

    class FakeEnvironment(string optInValue) : SystemEnvironment
    {
        public override string GetEnvironmentVariable(string variable) =>
            variable == InstrumentationOptions.ExceptionSignalOptInEnvironmentVariableKey ? optInValue : null;
    }
}
