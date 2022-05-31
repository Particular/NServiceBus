namespace NServiceBus
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Text;
    using System.Text.RegularExpressions;
    using Pipeline;

    static class LambdaExpressionPrettyPrint
    {
        public static string PrettyPrint(this List<Expression> expression)
        {
            expression.Reverse();
            var sb = new StringBuilder();
            var visitor = new BehaviorPipelineExpressionVisitor(sb);
            for (var i = 0; i < expression.Count; i++)
            {
                visitor.Indent = i;
                visitor.Visit(expression[i]);
                sb.AppendLine();
            }
            return sb.ToString();
        }

        class BehaviorPipelineExpressionVisitor : ExpressionVisitor
        {
            readonly StringBuilder builder;

            public BehaviorPipelineExpressionVisitor(StringBuilder builder) => this.builder = builder;

            public int Indent { get; set; }

            protected override Expression VisitLambda<T>(Expression<T> node)
            {
                if (node.Parameters.Count == 1)
                {
                    _ = builder.Append($"{new string(' ', Indent * 4)}({node.Parameters[0].Type.Name} {node.Parameters[0].Name}) => ");
                }
                return base.VisitLambda(node);
            }

            protected override Expression VisitMethodCall(MethodCallExpression node)
            {
                if (node.Method.Name == nameof(PipelineExecutionExtensions.GetBehavior))
                {
                    var behaviorType = node.Method.GetGenericArguments().Last();
                    _ = builder.Replace(node.ToString(), behaviorType.Name);
                }

                if (node.Method.Name == nameof(IBehavior<IBehaviorContext, IBehaviorContext>.Invoke))
                {
                    // The regex is not compiled to avoid paying the cost at runtime since this whole code path is assumed to be only executed
                    // when debug logging is activated
                    string replace = Regex.Replace(node.ToString(), @", value\(System.Func`2\[NServiceBus.Pipeline.PipelineTerminator\`1\+ITerminatingContext.*\)\)", "))");
                    replace = Regex.Replace(replace, @"value\(System.Func`2.*\)\)", string.Empty);
                    _ = builder.Append(replace);
                }
                return base.VisitMethodCall(node);
            }
        }
    }
}