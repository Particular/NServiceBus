#nullable enable

namespace NServiceBus;

using System;
using System.Threading.Tasks;
using Pipeline;

readonly record struct PipelinePart(
    Func<IBehaviorContext, int, int, Task> Invoke,
    int ChildStart,
    int ChildEnd,
    string BehaviorTypeName,
    string ContextTypeName);
