namespace NServiceBus;

using System;

/// <summary>
/// Contains extension methods to <see cref="EndpointConfiguration" />.
/// </summary>
public static class ConfigureAudit
{
    /// <summary>
    /// Configures audit settings.
    /// </summary>
    /// <param name="config">The <see cref="EndpointConfiguration" /> instance to apply the settings to.</param>
    /// <param name="auditQueue">The name of the audit queue to use.</param>
    /// <param name="timeToBeReceived">The message expiration time span to use for messages sent to the audit queue.</param>
    public static void AuditProcessedMessagesTo(this EndpointConfiguration config, string auditQueue, TimeSpan? timeToBeReceived = null)
    {
        ArgumentNullException.ThrowIfNull(config);
        ArgumentException.ThrowIfNullOrWhiteSpace(auditQueue);

        if (timeToBeReceived is not null)
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(timeToBeReceived.Value, TimeSpan.Zero);
        }

        config.Settings.Set(new AuditConfigReader.Result(auditQueue, timeToBeReceived));
    }
}