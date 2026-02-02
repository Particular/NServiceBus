#nullable enable

namespace NServiceBus;

using System;
using System.Diagnostics.CodeAnalysis;
using Logging;
using Settings;

/// <summary>
/// A utility class to get the configured audit queue settings.
/// </summary>
public static class AuditConfigReader
{
    /// <summary>
    /// Gets the audit queue address for the endpoint.
    /// The audit queue address can be configured using 'EndpointConfiguration.AuditProcessedMessagesTo()'.
    /// </summary>
    /// <param name="settings">The configuration settings for the endpoint.</param>
    /// <param name="address">When this method returns, contains the audit queue address for the endpoint, if it has been configured, or null if it has not.</param>
    /// <returns>True if an audit queue address is configured; otherwise, false.</returns>
    public static bool TryGetAuditQueueAddress(this IReadOnlySettings settings, [NotNullWhen(true)] out string? address)
    {
        ArgumentNullException.ThrowIfNull(settings);

        var result = GetConfiguredAuditQueue(settings);

        if (result == null || result.Disabled)
        {
            address = null;
            return false;
        }

        address = result.Address;
        return address != null;
    }

    /// <summary>
    /// Gets the audit message expiration time span for the endpoint.
    /// The audit message expiration time span can be configured using 'EndpointConfiguration.AuditProcessedMessagesTo()'.
    /// </summary>
    /// <param name="settings">The configuration settings for the endpoint.</param>
    /// <param name="auditMessageExpiration">When this method returns, contains the audit message expiration time span, if it has been configured, or TimeSpan.Zero if has not.</param>
    /// <returns>True if an audit message expiration time span is configured; otherwise, false.</returns>
    public static bool TryGetAuditMessageExpiration(this IReadOnlySettings settings, out TimeSpan auditMessageExpiration)
    {
        ArgumentNullException.ThrowIfNull(settings);

        var result = GetConfiguredAuditQueue(settings);

        if (result?.TimeToBeReceived == null)
        {
            auditMessageExpiration = TimeSpan.Zero;
            return false;
        }

        auditMessageExpiration = result.TimeToBeReceived.Value;
        return true;
    }

    internal static void SetAuditQueueDefaults(this SettingsHolder settings)
    {
        var environment = settings.Get<SystemEnvironment>();
        var auditDisabledValue = environment.GetEnvironmentVariable(IsDisabledEnvironmentVariableKey);
        var canParseAuditDisabledEnvironmentVariable = bool.TryParse(auditDisabledValue, out var isDisabled);

        if (auditDisabledValue is not null && !canParseAuditDisabledEnvironmentVariable)
        {
            Log.Warn($"{IsDisabledEnvironmentVariableKey} should be either `TRUE` or `FALSE`. `{auditDisabledValue}` is not a valid value.");
        }

        if (isDisabled)
        {
            settings.Set(new Result());
            return;
        }

        var userOverride = settings.GetOrDefault<Result>();
        var defaultAuditQueue = environment.GetEnvironmentVariable(AddressEnvironmentVariableKey);
        settings.Set(userOverride ?? (!string.IsNullOrEmpty(defaultAuditQueue) ? new Result(defaultAuditQueue) : new Result()));
    }

    static Result? GetConfiguredAuditQueue(IReadOnlySettings settings)
        => settings.TryGet(out Result configResult) ? configResult : null;

    static readonly ILog Log = LogManager.GetLogger(typeof(AuditConfigReader));

    internal const string AddressEnvironmentVariableKey = "NSERVICEBUS__AUDIT__ADDRESS";
    internal const string IsDisabledEnvironmentVariableKey = "NSERVICEBUS__AUDIT__DISABLED";

    internal class Result(string? address = null, TimeSpan? timeToBeReceived = null)
    {
        public readonly string? Address = address;
        public TimeSpan? TimeToBeReceived = timeToBeReceived;
        [MemberNotNullWhen(false, nameof(Address))]
        public bool Disabled => Address == null;
    }
}