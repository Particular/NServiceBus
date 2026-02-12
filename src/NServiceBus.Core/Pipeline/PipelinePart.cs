#nullable enable

namespace NServiceBus;

readonly record struct PipelinePart(
    byte InvokerId,
    int ChildStart,
    int ChildEnd,
    string BehaviorTypeName,
    string ContextTypeName);