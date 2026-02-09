#nullable enable

namespace NServiceBus;

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Pipeline;

static class BehaviorPartFactory
{
    static class Cache<TContext, TBehavior>
        where TContext : class, IBehaviorContext
        where TBehavior : class, IBehavior<TContext, TContext>
    {
        public static readonly Func<IBehaviorContext, int, int, Task> Invoke =
            static (ctx, _, _) =>
            {
                var extensions = ctx.Extensions;
                var behavior = extensions.GetBehavior<TBehavior>();
                return behavior.Invoke(Unsafe.As<TContext>(ctx), Start!);
            };

        static readonly Func<TContext, Task> Start = StageRunners.Next;
    }

    [DebuggerNonUserCode]
    [DebuggerHidden]
    [DebuggerStepThrough]
    [StackTraceHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static PipelinePart Create<TContext, TBehavior>()
        where TContext : class, IBehaviorContext
        where TBehavior : class, IBehavior<TContext, TContext> =>
        new(Cache<TContext, TBehavior>.Invoke, 0, 0, typeof(TBehavior).Name, typeof(TContext).Name);
}