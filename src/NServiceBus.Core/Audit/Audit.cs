#nullable enable

namespace NServiceBus.Features;

using Logging;
using Transport;

/// <summary>
/// Enabled message auditing for this endpoint.
/// </summary>
public sealed class Audit : Feature
{
    /// <summary>
    /// Creates a new instance of the audit feature.
    /// </summary>
    public Audit()
    {
        Defaults(settings => settings.SetAuditQueueDefaults());
        Prerequisite(config => !config.Settings.Get<AuditConfigReader.Result>().Disabled, "Auditing has been explicitly disabled.");
        Prerequisite(context => !context.Settings.GetOrDefault<bool>("Endpoint.SendOnly"),
            "Auditing is only relevant for endpoints receiving messages.");
    }

    /// <summary>
    /// See <see cref="Feature.Setup" />.
    /// </summary>
    protected override void Setup(FeatureConfigurationContext context)
    {
        var auditConfig = context.Settings.Get<AuditConfigReader.Result>();
        // This should never happen due to the prerequisite but adding a guard to satisfy nullability analysis
        if (auditConfig.Disabled)
        {
            return;
        }

        context.Pipeline.Register("AuditToDispatchConnector", new AuditToRoutingConnector(), "Dispatches the audit message to the transport");
        context.Pipeline.Register("AuditProcessedMessage", new InvokeAuditPipelineBehavior(auditConfig.Address, auditConfig.TimeToBeReceived), "Execute the audit pipeline");

        context.Settings.Get<QueueBindings>().BindSending(auditConfig.Address);

        context.Settings.AddStartupDiagnosticsSection("Manifest-AuditQueue", auditConfig.Address);
        context.Settings.AddStartupDiagnosticsSection("Audit", new
        {
            AuditQueue = auditConfig.Address,
            AuditTTBR = auditConfig.TimeToBeReceived?.ToString("g") ?? "-"
        });

        Logger.InfoFormat($"Auditing processed messages to '{auditConfig.Address}'");
    }

    static readonly ILog Logger = LogManager.GetLogger<Audit>();
}