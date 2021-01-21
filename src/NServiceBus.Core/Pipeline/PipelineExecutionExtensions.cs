namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Reflection;
    using System.Threading;
    using System.Threading.Tasks;
    using FastExpressionCompiler;
    using Pipeline;

    static class PipelineExecutionExtensions
    {
        public static Func<TRootContext, CancellationToken, Task> CreatePipelineExecutionFuncFor<TRootContext>(this IBehavior[] behaviors)
            where TRootContext : IBehaviorContext
        {
            return (Func<TRootContext, CancellationToken, Task>)behaviors.CreatePipelineExecutionExpression();
        }

        /// <code>
        /// (rootContext, rootToken)
        ///    => behavior1.Invoke(rootContext,
        ///       (context1, token1) => behavior2.Invoke(context1,
        ///        ..., token1
        ///          (context{N}, token{N}) => behavior{N}.Invoke(context{N},
        ///             (context{N+1}, token{N+1}) => TaskEx.Completed), token{N})
        /// </code>
        public static Delegate CreatePipelineExecutionExpression(this IBehavior[] behaviors, List<Expression> expressions = null)
        {
            Delegate lambdaExpression = null;
            var behaviorCount = behaviors.Length - 1;
            // We start from the end of the list know the lambda expressions deeper in the call stack in advance
            for (var i = behaviorCount; i >= 0; i--)
            {
                var currentBehavior = behaviors[i];
                var behaviorInterfaceType = currentBehavior.GetType().GetBehaviorInterface();
                if (behaviorInterfaceType == null)
                {
                    throw new InvalidOperationException("Behaviors must implement IBehavior<TInContext, TOutContext>");
                }
                // Select the method on the type which was implemented from the behavior interface.
                var methodInfo = currentBehavior.GetType().GetInterfaceMap(behaviorInterfaceType).TargetMethods.FirstOrDefault();
                if (methodInfo == null)
                {
                    throw new InvalidOperationException("Behaviors must implement IBehavior<TInContext, TOutContext> and provide an invocation method.");
                }

                var genericArguments = behaviorInterfaceType.GetGenericArguments();
                var inContextType = genericArguments[0];

                var inContextParameter = Expression.Parameter(inContextType, $"context{i}");
                var cancellationTokenParameter = Expression.Parameter(typeof(CancellationToken), $"token{i}");

                if (i == behaviorCount)
                {
                    var outContextType = genericArguments[1];
                    var doneDelegate = CreateDoneDelegate(outContextType, i);
                    lambdaExpression = CreateBehaviorCallDelegate(currentBehavior, methodInfo, inContextParameter, doneDelegate, cancellationTokenParameter, expressions);
                    continue;
                }

                lambdaExpression = CreateBehaviorCallDelegate(currentBehavior, methodInfo, inContextParameter, lambdaExpression, cancellationTokenParameter, expressions);
            }

            return lambdaExpression;
        }

        /// <code>
        /// (context{i}, token{i}) => behavior.Invoke(context{i}, (context{i+1}, token{i+1}) => previous, token{i})
        /// </code>
        static Delegate CreateBehaviorCallDelegate(IBehavior currentBehavior, MethodInfo methodInfo, ParameterExpression outerContextParam, Delegate previous, ParameterExpression cancellationTokenParameter, List<Expression> expressions = null)
        {
            var body = Expression.Call(Expression.Constant(currentBehavior), methodInfo, outerContextParam, Expression.Constant(previous), cancellationTokenParameter);
            var lambdaExpression = Expression.Lambda(body, outerContextParam, cancellationTokenParameter);
            expressions?.Add(lambdaExpression);
            return lambdaExpression.CompileFast();
        }

        /// <code>
        /// context{i} => return TaskEx.CompletedTask;
        /// </code>>
        static Delegate CreateDoneDelegate(Type inContextType, int i)
        {
            var innerContextParam = Expression.Parameter(inContextType, $"context{i + 1}");
            var cancellationTokenParam = Expression.Parameter(typeof(CancellationToken), $"token{i + 1}");
            var lambdaType = typeof(Func<,,>).MakeGenericType(inContextType, typeof(CancellationToken), typeof(Task));
            return Expression.Lambda(lambdaType, Expression.Constant(Task.CompletedTask), innerContextParam, cancellationTokenParam).CompileFast();
        }
    }
}