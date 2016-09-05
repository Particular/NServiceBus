namespace NServiceBus
{
    using System;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Reflection;
    using System.Threading.Tasks;
    using Pipeline;

    static class BehaviorExtensions
    {
        // ReSharper disable once SuggestBaseTypeForParameter
        public static Func<TRootContext, Task> CreatePipelineExecutionFuncFor<TRootContext>(this IBehavior[] behaviors)
            where TRootContext : IBehaviorContext
        {
            return (Func<TRootContext, Task>)behaviors.CreatePipelineExecutionExpression().Compile();
        }

        /// <code>
        /// rootContext
        ///    => behavior1.Invoke(rootContext,
        ///       context1 => behavior2.Invoke(context1,
        ///        ...
        ///          context{N} => behavior{N}.Invoke(context{N},
        ///             context{N+1} => TaskEx.Completed))
        /// </code>
        public static LambdaExpression CreatePipelineExecutionExpression(this IBehavior[] behaviors)
        {
            LambdaExpression lambdaExpression = null;
            var length = behaviors.Length - 1;
            // We start from the end of the list know the lambda expressions deeper in the call stack in advance
            for (var i = length; i >= 0; i--)
            {
                var currentBehavior = behaviors[i];
                var behaviorInterfaceType = currentBehavior.GetType().GetInterfaces().FirstOrDefault(t => t.GetGenericArguments().Length == 2 && t.FullName.StartsWith("NServiceBus.Pipeline.IBehavior"));
                if (behaviorInterfaceType == null)
                {
                    throw new InvalidOperationException("Behaviors must implement IBehavior<TInContext, TOutContext>");
                }
                var methodInfo = behaviorInterfaceType.GetMethods().FirstOrDefault();
                if (methodInfo == null)
                {
                    throw new InvalidOperationException("Behaviors must implement IBehavior<TInContext, TOutContext> and provide an invocation method.");
                }

                var genericArguments = behaviorInterfaceType.GetGenericArguments();
                var inContextType = genericArguments[0];

                var outerContextParam = Expression.Parameter(inContextType, $"context{i}");

                if (i == length)
                {
                    if (currentBehavior is IPipelineTerminator)
                    {
                        inContextType = typeof(PipelineTerminator<>.ITerminatingContext).MakeGenericType(inContextType);
                    }
                    var doneDelegate = CreateDoneDelegate(inContextType, i);
                    lambdaExpression = CreateBehaviorCallDelegate(currentBehavior, methodInfo, outerContextParam, doneDelegate);
                    continue;
                }

                lambdaExpression = CreateBehaviorCallDelegate(currentBehavior, methodInfo, outerContextParam, lambdaExpression);
            }

            return lambdaExpression;
        }

        // ReSharper disable once SuggestBaseTypeForParameter

        /// <code>
        /// context{i} => behavior.Invoke(context{i}, context{i+1} => previous)
        /// </code>>
        static LambdaExpression CreateBehaviorCallDelegate(IBehavior currentBehavior, MethodInfo methodInfo, ParameterExpression outerContextParam, LambdaExpression previous)
        {
            Expression body = Expression.Call(Expression.Constant(currentBehavior), methodInfo, outerContextParam, previous);
            return Expression.Lambda(body, outerContextParam);
        }

        /// <code>
        /// context{i} => return TaskEx.CompletedTask;
        /// </code>>
        static LambdaExpression CreateDoneDelegate(Type inContextType, int i)
        {
            var innerContextParam = Expression.Parameter(inContextType, $"context{i + 1}");
            return Expression.Lambda(typeof(Func<,>).MakeGenericType(inContextType, typeof(Task)), Expression.Constant(TaskEx.CompletedTask), innerContextParam);
        }
    }
}