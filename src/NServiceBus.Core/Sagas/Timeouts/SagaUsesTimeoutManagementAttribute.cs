#nullable enable

namespace NServiceBus.Sagas;

using System;
using System.ComponentModel;

/// <summary>
/// Flags a saga that uses the timeout management APIs. Emitted by the NServiceBus source generator; not intended to be applied by hand.
/// </summary>
/// <remarks>
/// The attribute is applied to the saga's assembly so that it travels with the saga whether the saga is found by assembly scanning or registered explicitly,
/// and so that the NServiceBus analyzers can read it from referenced assemblies.
/// </remarks>
[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
[EditorBrowsable(EditorBrowsableState.Never)]
public sealed class SagaUsesTimeoutManagementAttribute(Type sagaType) : Attribute
{
    /// <summary>
    /// The saga that uses the timeout management APIs.
    /// </summary>
    public Type SagaType { get; } = sagaType;
}
