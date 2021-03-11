namespace NServiceBus
{
    using System.Collections.Generic;
    using System.Linq.Expressions;
    using System.Text;

    static class LambdaExpressionPrettyPrint
    {
        public static string PrettyPrint(this List<Expression> expression)
        {
            expression.Reverse();
            var sb = new StringBuilder();
            for (var i = 0; i < expression.Count; i++)
            {
                sb.AppendLine($"{new string(' ', i * 4)}{expression[i].ToString().TrimStart()},");
            }
            return sb.ToString();
        }
    }
}