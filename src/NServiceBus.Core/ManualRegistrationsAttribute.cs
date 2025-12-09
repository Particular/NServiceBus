#nullable enable

namespace NServiceBus;

using System;

/// <summary>
/// Instructs the NServiceBus analyzers to scan the decorated scope for manual registrations such as AddSaga or AddHandler.
/// </summary>
[AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
public sealed class ManualRegistrationsAttribute : Attribute
{
}
