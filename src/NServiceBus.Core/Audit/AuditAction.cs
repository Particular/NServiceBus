﻿#nullable enable

namespace NServiceBus.Audit;

using System.Collections.Generic;
using Pipeline;

/// <summary>
/// Base class for audit actions.
/// </summary>
public abstract class AuditAction
{
    /// <summary>
    /// Gets the messages, if any, this audit operation should result in.
    /// </summary>
    public abstract IReadOnlyCollection<IRoutingContext> GetRoutingContexts(IAuditActionContext context);
}