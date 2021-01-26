﻿namespace NServiceBus
{
    using System.Threading;
    using System.Threading.Tasks;
    using Pipeline;

    interface IPipeline
    {
    }

    interface IPipeline<in TContext> : IPipeline
        where TContext : IBehaviorContext
    {
        Task Invoke(TContext context, CancellationToken token);
    }
}