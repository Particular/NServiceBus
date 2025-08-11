namespace NServiceBus;

using System;
using Pipeline;

sealed class ThrowingPipelineCache : IPipelineCache
{
    public IPipeline<TContext> Pipeline<TContext>() where TContext : IBehaviorContext => throw new NotImplementedException("This is a fake pipeline cache that does not support pipelines.");
}