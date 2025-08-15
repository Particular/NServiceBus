#nullable enable

namespace NServiceBus;

using System;
using Extensibility;
using Pipeline;

/// <summary>
/// Provides options for disabling the best practice enforcement.
/// </summary>
public static class BestPracticesOptionExtensions
{
    /// <summary>
    /// Turns off the best practice enforcement for the given message.
    /// </summary>
    public static void DoNotEnforceBestPractices(this ExtendableOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        options.Context.SetDoNotEnforceBestPractices();
    }

    /// <summary>
    /// Returns whether <see cref="DoNotEnforceBestPractices(ExtendableOptions)" /> has been called or not.
    /// </summary>
    /// <returns><c>true</c> if best practice enforcement has been disabled, <c>false</c> otherwise.</returns>
    public static bool IgnoredBestPractices(this ExtendableOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        _ = options.Context.TryGet<EnforceBestPracticesOptions>(out var bestPracticesOptions);
        return !(bestPracticesOptions?.Enabled ?? true);
    }

    /// <summary>
    /// Turns off the best practice enforcement for the given context.
    /// </summary>
    public static void DoNotEnforceBestPractices(this IOutgoingReplyContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        context.Extensions.SetDoNotEnforceBestPractices();
    }

    /// <summary>
    /// Turns off the best practice enforcement for the given context.
    /// </summary>
    public static void DoNotEnforceBestPractices(this IOutgoingSendContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        context.Extensions.SetDoNotEnforceBestPractices();
    }

    /// <summary>
    /// Turns off the best practice enforcement for the given context.
    /// </summary>
    public static void DoNotEnforceBestPractices(this ISubscribeContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        context.Extensions.SetDoNotEnforceBestPractices();
    }

    /// <summary>
    /// Turns off the best practice enforcement for the given context.
    /// </summary>
    public static void DoNotEnforceBestPractices(this IOutgoingPublishContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        context.Extensions.SetDoNotEnforceBestPractices();
    }

    /// <summary>
    /// Turns off the best practice enforcement for the given context.
    /// </summary>
    public static void DoNotEnforceBestPractices(this IUnsubscribeContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        context.Extensions.SetDoNotEnforceBestPractices();
    }

    static void SetDoNotEnforceBestPractices(this ContextBag context)
    {
        var bestPracticesOptions = new EnforceBestPracticesOptions
        {
            Enabled = false
        };
        context.Set(bestPracticesOptions);
    }
}