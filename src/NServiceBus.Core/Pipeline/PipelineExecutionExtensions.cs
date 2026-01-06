#nullable enable

namespace NServiceBus;

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using FastExpressionCompiler;
using Pipeline;

static class PipelineExecutionExtensions
{
    extension(IBehavior[] behaviors)
    {
        [RequiresUnreferencedCode("Requires FastExpressionCompiler")]
        public Func<TRootContext, Task> CreatePipelineExecutionFuncFor<TRootContext>(List<Expression>? expressions = null)
            where TRootContext : IBehaviorContext =>
            (Func<TRootContext, Task>)behaviors.CreatePipelineExecutionExpression(expressions);

        /// <code>
        /// rootContext
        ///    => GetBehavior(rootContext, 0).Invoke(rootContext,
        ///       context1 => GetBehavior(context2, 1).Invoke(context1,
        ///        ...
        ///          context{N} => GetBehavior(context{N}, {N-1}).Invoke(context{N},
        ///             context{N+1} => TaskEx.Completed))
        /// </code>
        [RequiresUnreferencedCode("Requires FastExpressionCompiler")]
        public Delegate CreatePipelineExecutionExpression(List<Expression>? expressions = null)
        {
            Delegate? lambdaExpression = null;
            var behaviorCount = behaviors.Length - 1;
            // We start from the end of the list know the lambda expressions deeper in the call stack in advance
            for (var i = behaviorCount; i >= 0; i--)
            {
                var currentBehavior = behaviors[i];
                var behaviorInterfaceType = currentBehavior.GetType().GetBehaviorInterface() ?? throw new InvalidOperationException("Behaviors must implement IBehavior<TInContext, TOutContext>");
                // Select the method on the type which was implemented from the behavior interface.
                var methodInfo = currentBehavior.GetType().GetInterfaceMap(behaviorInterfaceType).TargetMethods.FirstOrDefault() ?? throw new InvalidOperationException("Behaviors must implement IBehavior<TInContext, TOutContext> and provide an invocation method.");

                var genericArguments = behaviorInterfaceType.GetGenericArguments();
                var inContextType = genericArguments[0];
                var outContextType = genericArguments[1];

                var inContextParameter = Expression.Parameter(inContextType, $"context{i}");

                if (i == behaviorCount)
                {
                    var doneDelegate = CreateDoneDelegate(outContextType, i);
                    lambdaExpression = CreateBehaviorCallDelegate(methodInfo, inContextParameter, currentBehavior.GetType(), doneDelegate, i, expressions);
                    continue;
                }

                lambdaExpression = CreateBehaviorCallDelegate(methodInfo, inContextParameter, currentBehavior.GetType(), lambdaExpression!, i, expressions);
            }

            return lambdaExpression!;
        }
    }

    /// <code>
    /// context{i} => GetBehavior(context{i}, {i}).Invoke(context{i+1} => previous)
    /// </code>>
    [RequiresUnreferencedCode("Requires FastExpressionCompiler")]
    static Delegate CreateBehaviorCallDelegate(MethodInfo methodInfo, ParameterExpression outerContextParam, Type behaviorType, Delegate previous, int i, List<Expression>? expressions = null)
    {
        MethodInfo getBehaviorMethodInfo = GetBehaviorMethodInfo.MakeGenericMethod(outerContextParam.Type, behaviorType);
        Expression getBehaviorCallExpression = Expression.Call(null, getBehaviorMethodInfo, outerContextParam, Expression.Constant(i));
        Expression body = Expression.Call(getBehaviorCallExpression, methodInfo, outerContextParam, Expression.Constant(previous));
        var lambdaExpression = Expression.Lambda(body, outerContextParam);
        expressions?.Add(lambdaExpression);
        return lambdaExpression.CompileFast();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TBehavior GetBehavior<TContext, TBehavior>(TContext context, int index)
        where TContext : class, IBehaviorContext
        where TBehavior : class, IBehavior
        => Unsafe.As<TBehavior>(
            Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(context.Extensions.Behaviors), index));

    /// <code>
    /// context{i} => return TaskEx.CompletedTask;
    /// </code>>
    [RequiresUnreferencedCode("Requires FastExpressionCompiler")]
    static Delegate CreateDoneDelegate(Type inContextType, int i)
    {
        var innerContextParam = Expression.Parameter(inContextType, $"context{i + 1}");
        var doneDelegateType = typeof(Func<,>).MakeGenericType(inContextType, typeof(Task));
        return Expression.Lambda(doneDelegateType, Expression.Constant(Task.CompletedTask), innerContextParam).CompileFast();
    }

    static readonly MethodInfo GetBehaviorMethodInfo =
        typeof(PipelineExecutionExtensions).GetMethod("GetBehavior")!;
}