#nullable enable

namespace NServiceBus;

using System;
using System.Diagnostics.CodeAnalysis;
using Pipeline;

sealed class ReplaceStep(
    string idToReplace,
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.Interfaces)] Type behavior,
    string? description = null,
    Func<IServiceProvider, IBehavior>? factoryMethod = null)
{
    public string ReplaceId { get; } = idToReplace;
    public string? Description { get; } = description;
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.Interfaces)]
    public Type BehaviorType { get; } = behavior;
    public Func<IServiceProvider, IBehavior>? FactoryMethod { get; } = factoryMethod;
    public int RegistrationOrder { get; set; }
}