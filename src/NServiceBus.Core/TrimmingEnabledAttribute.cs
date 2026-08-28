#nullable enable

namespace NServiceBus;

using System;

/// <summary>
/// Emitted by the NServiceBus source generators into the assembly of an application that is published with trimming
/// enabled. When assembly scanning is disabled, the runtime uses this attribute to activate strict registered-only
/// message metadata mode.
/// </summary>
[AttributeUsage(AttributeTargets.Assembly)]
public sealed class TrimmingEnabledAttribute : Attribute
{
}
