#nullable enable

namespace NServiceBus;

using System;
using Pipeline;

sealed class ReplaceStep(
    string idToReplace,
    Type behavior,
    string? description = null,
    Func<IServiceProvider, IBehavior>? factoryMethod = null)
{
    public string ReplaceId { get; } = idToReplace;
    public string? Description { get; } = description;
    public Type BehaviorType { get; } = behavior;
    public Func<IServiceProvider, IBehavior>? FactoryMethod { get; } = factoryMethod;
    public int RegistrationOrder { get; set; }
}