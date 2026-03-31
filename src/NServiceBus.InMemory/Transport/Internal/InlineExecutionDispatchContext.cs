namespace NServiceBus;

sealed class InlineExecutionDispatchContext(InlineExecutionScope scope, int depth)
{
    public InlineExecutionScope Scope { get; } = scope;
    public int Depth { get; } = depth;
}