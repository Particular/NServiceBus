namespace NServiceBus.Core.Tests.OpenTelemetry;

using System.Collections.Generic;
using NUnit.Framework;
using Settings;

[TestFixture]
public class OpenTelemetryExtensionsTests
{
    [Test]
    public void StartNewTraceOnReceive_should_set_span_link_override_on_send_options()
    {
        var options = new SendOptions();

        options.StartNewTraceOnReceive();

        Assert.That(options.Context.TryGet(OpenTelemetryExtensions.TraceConnectorOverrideKey, out TraceMode connector), Is.True);
        Assert.That(connector, Is.EqualTo(TraceMode.StartNew));
    }

    [Test]
    public void ContinueExistingTraceOnReceive_should_set_child_span_override_on_send_options()
    {
        var options = new SendOptions();

        options.ContinueExistingTraceOnReceive();

        Assert.That(options.Context.TryGet(OpenTelemetryExtensions.TraceConnectorOverrideKey, out TraceMode connector), Is.True);
        Assert.That(connector, Is.EqualTo(TraceMode.ContinueExisting));
    }

    [Test]
    public void StartNewTraceOnReceive_should_set_span_link_override_on_publish_options()
    {
        var options = new PublishOptions();

        options.StartNewTraceOnReceive();

        Assert.That(options.Context.TryGet(OpenTelemetryExtensions.TraceConnectorOverrideKey, out TraceMode connector), Is.True);
        Assert.That(connector, Is.EqualTo(TraceMode.StartNew));
    }

    [Test]
    public void ContinueExistingTraceOnReceive_should_set_child_span_override_on_publish_options()
    {
        var options = new PublishOptions();

        options.ContinueExistingTraceOnReceive();

        Assert.That(options.Context.TryGet(OpenTelemetryExtensions.TraceConnectorOverrideKey, out TraceMode connector), Is.True);
        Assert.That(connector, Is.EqualTo(TraceMode.ContinueExisting));
    }

    [Test]
    public void Last_override_call_wins()
    {
        var options = new PublishOptions();

        options.ContinueExistingTraceOnReceive();
        options.StartNewTraceOnReceive();

        Assert.That(options.Context.TryGet(OpenTelemetryExtensions.TraceConnectorOverrideKey, out TraceMode connector), Is.True);
        Assert.That(connector, Is.EqualTo(TraceMode.StartNew));
    }

    [Test]
    public void Defaults_to_span_and_logs_when_opt_in_environment_variable_is_not_set()
    {
        var settingsHolder = new SettingsHolder();
        settingsHolder.Set<SystemEnvironment>(new FakeEnvironment { ValueToReturn = [] });

        InstrumentationOptions.SetExceptionRecordingModeDefault(settingsHolder);

        Assert.That(settingsHolder.Get<InstrumentationOptions>().ExceptionRecordingMode, Is.EqualTo(ExceptionRecordingMode.SpanAndLogs));
    }

    [Test]
    public void Uses_logs_only_when_opt_in_environment_variable_is_logs()
    {
        var settingsHolder = new SettingsHolder();
        settingsHolder.Set<SystemEnvironment>(new FakeEnvironment
        {
            ValueToReturn = new Dictionary<string, string> { { InstrumentationOptions.ExceptionSignalOptInEnvironmentVariableKey, "logs" } }
        });

        InstrumentationOptions.SetExceptionRecordingModeDefault(settingsHolder);

        Assert.That(settingsHolder.Get<InstrumentationOptions>().ExceptionRecordingMode, Is.EqualTo(ExceptionRecordingMode.Logs));
    }

    [Test]
    public void Uses_span_and_logs_when_opt_in_environment_variable_is_logs_dup()
    {
        var settingsHolder = new SettingsHolder();
        settingsHolder.Set<SystemEnvironment>(new FakeEnvironment
        {
            ValueToReturn = new Dictionary<string, string> { { InstrumentationOptions.ExceptionSignalOptInEnvironmentVariableKey, "logs/dup" } }
        });

        InstrumentationOptions.SetExceptionRecordingModeDefault(settingsHolder);

        Assert.That(settingsHolder.Get<InstrumentationOptions>().ExceptionRecordingMode, Is.EqualTo(ExceptionRecordingMode.SpanAndLogs));
    }

    [Test]
    public void Environment_variable_takes_precedence_over_explicit_configuration()
    {
        var settingsHolder = new SettingsHolder();
        settingsHolder.Set<SystemEnvironment>(new FakeEnvironment
        {
            ValueToReturn = new Dictionary<string, string> { { InstrumentationOptions.ExceptionSignalOptInEnvironmentVariableKey, "logs" } }
        });

        // explicitly configured to something other than what the environment variable resolves to
        settingsHolder.Set(new InstrumentationOptions { ExceptionRecordingMode = ExceptionRecordingMode.SpanAndLogs });
        InstrumentationOptions.SetExceptionRecordingModeDefault(settingsHolder);

        Assert.That(settingsHolder.Get<InstrumentationOptions>().ExceptionRecordingMode, Is.EqualTo(ExceptionRecordingMode.Logs));
    }

    [Test]
    public void Explicit_configuration_is_preserved_when_environment_variable_is_not_set()
    {
        var settingsHolder = new SettingsHolder();
        settingsHolder.Set<SystemEnvironment>(new FakeEnvironment { ValueToReturn = [] });

        settingsHolder.Set(new InstrumentationOptions { ExceptionRecordingMode = ExceptionRecordingMode.Logs });
        InstrumentationOptions.SetExceptionRecordingModeDefault(settingsHolder);

        Assert.That(settingsHolder.Get<InstrumentationOptions>().ExceptionRecordingMode, Is.EqualTo(ExceptionRecordingMode.Logs));
    }

    class FakeEnvironment : SystemEnvironment
    {
        public Dictionary<string, string> ValueToReturn { get; set; }

        public override string GetEnvironmentVariable(string variable) => ValueToReturn.GetValueOrDefault(variable);
    }
}
