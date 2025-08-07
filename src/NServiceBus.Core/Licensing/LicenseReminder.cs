#nullable enable

namespace NServiceBus.Features;

using System;
using System.Diagnostics;
using Logging;
using Particular.Licensing;

class LicenseReminder : Feature
{
    public LicenseReminder()
    {
        EnableByDefault();

        Defaults(s => s.SetDefault(LicenseTextSettingsKey, null));
        Defaults(s => s.SetDefault(LicenseFilePathSettingsKey, null));
    }

    protected internal override void Setup(FeatureConfigurationContext context)
    {
        try
        {
            var result = LicenseManager.InitializeLicense(context.Settings.Get<string?>(LicenseTextSettingsKey), context.Settings.Get<string?>(LicenseFilePathSettingsKey));

            context.Settings.AddStartupDiagnosticsSection("Licensing", GenerateLicenseDiagnostics(result));

            if (!result.HasLicenseExpired())
            {
                return;
            }

            context.Pipeline.Register("LicenseReminder", new AuditInvalidLicenseBehavior(), "Audits that the message was processed by an endpoint with an expired license");

            if (Debugger.IsAttached)
            {
                context.Pipeline.Register("LogErrorOnInvalidLicense", new LogErrorOnInvalidLicenseBehavior(), "Logs an error when running in debug mode with an expired license");
            }
        }
        catch (Exception ex)
        {
            //we only log here to prevent licensing issue to abort startup and cause production outages
            Logger.Fatal("Failed to initialize the license", ex);
        }
    }

    static object GenerateLicenseDiagnostics(ActiveLicenseFindResult result) =>
        new
        {
            result.License.RegisteredTo,
            result.License.LicenseType,
            result.License.Edition,
            Tier = result.License.Edition,
            LicenseStatus = result.License.GetLicenseStatus(),
            LicenseLocation = result.Location,
            ValidApplications = string.Join(",", result.License.ValidApplications),
            CommercialLicense = result.License.IsCommercialLicense,
            IsExpired = result.HasLicenseExpired(),
            result.License.ExpirationDate,
            UpgradeProtectionExpirationDate = result.License.UpgradeProtectionExpiration
        };

    public const string LicenseTextSettingsKey = "LicenseText";
    public const string LicenseFilePathSettingsKey = "LicenseFilePath";

    static readonly ILog Logger = LogManager.GetLogger<LicenseReminder>();
}