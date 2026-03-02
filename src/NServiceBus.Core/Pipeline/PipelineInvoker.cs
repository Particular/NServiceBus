#nullable enable

namespace NServiceBus;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Pipeline;

static partial class PipelineInvoker
{
    public static Func<IBehaviorContext, Task> Build(IReadOnlyList<RegisterStep> steps, IBehavior[] behaviors)
    {
        if (steps.Count == 0)
        {
            return CompletedRoot;
        }

        InvokerNode? next = null;
        for (var i = steps.Count - 1; i >= 0; i--)
        {
            next = CreateInvokerNode(steps[i], behaviors[i], next);
        }

        return next!.Invoke;
    }

    static Task CompletedRoot(IBehaviorContext _) => Task.CompletedTask;

    abstract class InvokerNode
    {
        [StackTraceHidden]
        [DebuggerStepThrough]
        [DebuggerHidden]
        [DebuggerNonUserCode]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public abstract Task Invoke(IBehaviorContext context);
    }

    sealed class InvokerNode<TInContext, TOutContext>(IBehavior<TInContext, TOutContext> behavior, Func<TOutContext, Task> next) : InvokerNode
        where TInContext : class, IBehaviorContext
        where TOutContext : class, IBehaviorContext
    {
        [StackTraceHidden]
        [DebuggerStepThrough]
        [DebuggerHidden]
        [DebuggerNonUserCode]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override Task Invoke(IBehaviorContext context) => behavior.Invoke(Unsafe.As<TInContext>(context), next);
    }

    static InvokerNode<TInContext, TOutContext> CreateNode<TInContext, TOutContext>(IBehavior behavior, InvokerNode? next)
        where TInContext : class, IBehaviorContext
        where TOutContext : class, IBehaviorContext =>
        new((IBehavior<TInContext, TOutContext>)behavior, CreateNext<TOutContext>(next));

    static Func<TOutContext, Task> CreateNext<TOutContext>(InvokerNode? next)
        where TOutContext : class, IBehaviorContext =>
        next is null ? CompletedNextCache<TOutContext>.Next : next.Invoke;

    static class CompletedNextCache<TOutContext> where TOutContext : class, IBehaviorContext
    {
        public static readonly Func<TOutContext, Task> Next = _ => Task.CompletedTask;
    }
}