namespace NServiceBus;

sealed record InlineEnvelopeState(InlineExecutionScope Scope, int Depth, bool IsRootDispatch);