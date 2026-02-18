#nullable enable

namespace NServiceBus;

using System;
using System.Collections.Generic;
using Pipeline;

readonly record struct PipelineStageModel(
    Type ContextType,
    IReadOnlyList<RegisterStep> Behaviors,
    RegisterStep? Connector,
    Type? NextContextType,
    bool IsTerminator);

readonly record struct PipelineModel(
    Type RootContextType,
    IReadOnlyList<RegisterStep> Steps,
    IReadOnlyList<PipelineStageModel> Stages);