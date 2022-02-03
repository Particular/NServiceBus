namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Reflection;
    using System.Threading.Tasks;
    using Extensibility;
    using FastExpressionCompiler;
    using Pipeline;

    static class PipelineExecutionExtensions
    {
        public static Func<TRootContext, Task> CreatePipelineExecutionFuncFor<TRootContext>(this IBehavior[] behaviors, List<Expression> expressions = null)
            where TRootContext : IBehaviorContext =>
            (Func<TRootContext, Task>)behaviors.CreatePipelineExecutionExpression(expressions);

        /// <code>
        /// rootContext
        ///    => ((Behavior1)rootContext.Extensions.Behaviors[0]).Invoke(rootContext,
        ///       context1 => ((Behavior2)context2.Extensions.Behaviors[1]).Invoke(context1,
        ///        ...
        ///          context{N} => ((Behavior{N})context{N}.Extensions.Behaviors[{N-1}]).Invoke(context{N},
        ///             context{N+1} => TaskEx.Completed))
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
                var outContextType = genericArguments[1];

                var inContextParameter = Expression.Parameter(inContextType, $"context{i}");

                if (i == behaviorCount)
                {
                    var doneDelegate = CreateDoneDelegate(outContextType, i);
                    lambdaExpression = CreateBehaviorCallDelegate(methodInfo, inContextParameter, currentBehavior.GetType(), doneDelegate, i, expressions);
                    continue;
                }

                lambdaExpression = CreateBehaviorCallDelegate(methodInfo, inContextParameter, currentBehavior.GetType(), lambdaExpression, i, expressions);
            }

            return lambdaExpression;
        }

        /// <code>
        /// context{i} => ((BehaviorType)context{i}.Extensions.Behaviors[i]).Invoke(context{i+1} => previous)
        /// </code>>
        static Delegate CreateBehaviorCallDelegate(MethodInfo methodInfo, ParameterExpression outerContextParam, Type behaviorType, Delegate previous, int i, List<Expression> expressions = null)
        {
            PropertyInfo extensionProperty = typeof(IExtendable).GetProperty("Extensions");
            Expression extensionPropertyExpression = Expression.Property(outerContextParam, extensionProperty);
            PropertyInfo behaviorsProperty = typeof(ContextBag).GetProperty("Behaviors", BindingFlags.Instance | BindingFlags.NonPublic);
            Expression behaviorsPropertyExpression = Expression.Property(extensionPropertyExpression, behaviorsProperty);
            Expression indexerPropertyExpression = Expression.ArrayIndex(behaviorsPropertyExpression, Expression.Constant(i));
            Expression castToBehavior = Expression.Convert(indexerPropertyExpression, behaviorType);
            Expression body = Expression.Call(castToBehavior, methodInfo, outerContextParam, Expression.Constant(previous));
            var lambdaExpression = Expression.Lambda(body, outerContextParam);
            expressions?.Add(lambdaExpression);
            return lambdaExpression.CompileFast();
        }

        /// <code>
        /// context{i} => return TaskEx.CompletedTask;
        /// </code>>
        static Delegate CreateDoneDelegate(Type inContextType, int i)
        {
            var innerContextParam = Expression.Parameter(inContextType, $"context{i + 1}");
            return Expression.Lambda(Expression.Constant(Task.CompletedTask), innerContextParam).CompileFast();
        }
    }
}