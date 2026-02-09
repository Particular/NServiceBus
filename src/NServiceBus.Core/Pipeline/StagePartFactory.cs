#nullable enable

namespace NServiceBus;

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Pipeline;

static class StagePartFactory
{
    static class Cache<TInContext, TOutContext, TBehavior>
        where TInContext : class, IBehaviorContext
        where TOutContext : class, IBehaviorContext
        where TBehavior : class, IBehavior<TInContext, TOutContext>
    {
        public static readonly Func<IBehaviorContext, int, int, Task> Invoke =
            static (ctx, childStart, childEnd) =>
            {
                var extensions = ctx.Extensions;
                ref var frame = ref extensions.Frame;

                frame.Index = childStart - 1;
                frame.RangeEnd = childEnd;

                return extensions.GetBehavior<TBehavior>().Invoke(Unsafe.As<TInContext>(ctx), Start!);
            };

        static readonly Func<TOutContext, Task> Start = StageRunners.Next;
    }

    [DebuggerStepThrough]
    [DebuggerHidden]
    [DebuggerNonUserCode]
    [StackTraceHidden]
    public static PipelinePart Create<TInContext, TOutContext, TBehavior>(int childStartIndex, int childEndIndex)
        where TInContext : class, IBehaviorContext
        where TOutContext : class, IBehaviorContext
        where TBehavior : class, IBehavior<TInContext, TOutContext>
        => new(Cache<TInContext, TOutContext, TBehavior>.Invoke, childStartIndex, childEndIndex, typeof(TBehavior).Name, typeof(TInContext).Name);
}
