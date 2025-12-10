#nullable enable

namespace NServiceBus.Features;

using Logging;
using Transport;

/// <summary>
/// Enabled message auditing for this endpoint.
/// </summary>
public sealed class Audit : Feature
{
    internal const string AddressEnvironmentVariableKey = "NServiceBus__Audit__Address";
    internal const string IsEnabledEnvironmentVariableKey = "NServiceBus__Audit__IsEnabled";

    /// <summary>
    /// Creates a new instance of the audit feature.
    /// </summary>
    public Audit()
    {
        Prerequisite(context => context.Settings.GetOrDefault<bool>("Audit.Enabled"), $"Auditing was disabled via the `{IsEnabledEnvironmentVariableKey}` environment variable setting");
        Prerequisite(context => !string.IsNullOrEmpty(context.Settings.GetOrDefault<AuditConfigReader.Result>()?.Address), "No configured audit queue was found");
        Prerequisite(context => !context.Settings.GetOrDefault<bool>("Endpoint.SendOnly"), "Auditing is only relevant for endpoints receiving messages.");
        Defaults(settings =>
        {
            if (settings.HasExplicitValue("Audit.Address"))
            {
                settings.SetDefault(new AuditConfigReader.Result(settings.Get<string>("Audit.Address"), null));
            }
        });
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