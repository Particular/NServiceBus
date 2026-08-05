#nullable enable

namespace NServiceBus;

using System;
using System.Collections.Generic;

sealed class RecordedExceptions
{
    // Reference equality is intentional: this tracks specific exception instances as they
    // propagate, not exceptions that merely look alike.
#pragma warning disable PS0025
    readonly HashSet<Exception> recorded = new(ReferenceEqualityComparer.Instance);
#pragma warning restore PS0025

    public bool HasBeenRecorded(Exception exception) => recorded.Contains(exception);

    public void MarkAsRecorded(Exception exception) => recorded.Add(exception);
}
