#nullable enable

namespace NServiceBus.Features;

using System;
using Logging;
using Transport;

/// <summary>
/// Enabled message auditing for this endpoint.
/// </summary>
public sealed class Audit : Feature
{
    const string AuditAddressEnvironmentVariableKey = "Audit__Address";
    const string AuditEnabledEnvironmentVariableKey = "Audit__IsEnabled";

    /// <summary>
    /// Creates a new instance of the audit feature.
    /// </summary>
    public Audit()
    {
        Prerequisite(_ =>
        {
            var auditEnabledValue = Environment.GetEnvironmentVariable(AuditEnabledEnvironmentVariableKey);

            // auditing is enabled by default and must be explicitly disabled
            return auditEnabledValue is null ||
                   !bool.TryParse(auditEnabledValue, out var isEnabled) ||
                   isEnabled;
        }, $"Auditing was disabled via the `{AuditEnabledEnvironmentVariableKey}` environment variable setting");

        var defaultAuditQueue = Environment.GetEnvironmentVariable(AuditAddressEnvironmentVariableKey);
        if (defaultAuditQueue is not null)
        {
            Defaults(settings => settings.SetDefault(new AuditConfigReader.Result(defaultAuditQueue, null)));
        }

        Prerequisite(config => !string.IsNullOrEmpty(config.Settings.GetOrDefault<AuditConfigReader.Result>()?.Address), "No configured audit queue was found");
        Prerequisite(context => !context.Settings.GetOrDefault<bool>("Endpoint.SendOnly"), "Auditing is only relevant for endpoints receiving messages.");
    }


    /// <summary>
    /// See <see cref="Feature.Setup" />.
    /// </summary>
    protected override void Setup(FeatureConfigurationContext context)
    {
        var auditConfig = context.Settings.Get<AuditConfigReader.Result>();

        context.Pipeline.Register("AuditToDispatchConnector", new AuditToRoutingConnector(), "Dispatches the audit message to the transport");
        context.Pipeline.Register("AuditProcessedMessage", new InvokeAuditPipelineBehavior(auditConfig.Address, auditConfig.TimeToBeReceived), "Execute the audit pipeline");

        context.Settings.Get<QueueBindings>().BindSending(auditConfig.Address);

        context.Settings.AddStartupDiagnosticsSection("Audit", new
        {
            AuditQueue = auditConfig.Address,
            AuditTTBR = auditConfig.TimeToBeReceived?.ToString("g") ?? "-"
        });

        Logger.InfoFormat($"Auditing processed messages to '{auditConfig.Address}'");
    }

    static readonly ILog Logger = LogManager.GetLogger<Audit>();
}