#nullable enable

namespace NServiceBus;

using System;
using System.Linq.Expressions;
using System.Reflection;

static class Inspect<TTarget>
{
    public static PropertyInfo GetProperty(Expression<Func<TTarget, object>> property)
        => GetMemberInfo(property, false) as PropertyInfo ?? throw new ArgumentException("Member is not a property");

    public static PropertyInfo? GetProperty(Expression<Func<TTarget, object>> property, bool checkForSingleDot)
        => GetMemberInfo(property, checkForSingleDot) as PropertyInfo;

    public static MemberInfo GetMemberInfo(Expression member, bool checkForSingleDot)
    {
        ArgumentNullException.ThrowIfNull(member);

        if (member is not LambdaExpression lambda)
        {
            throw new ArgumentException("Not a lambda expression", nameof(member));
        }

        MemberExpression? memberExpr = null;

        // The Func<TTarget, object> we use returns an object, so first statement can be either
        // a cast (if the field/property does not return an object) or the direct member access.
        if (lambda.Body.NodeType == ExpressionType.Convert)
        {
            // The cast is an unary expression, where the operand is the
            // actual member access expression.
            memberExpr = ((UnaryExpression)lambda.Body).Operand as MemberExpression;
        }
        else if (lambda.Body.NodeType == ExpressionType.MemberAccess)
        {
            memberExpr = lambda.Body as MemberExpression;
        }

        if (memberExpr == null)
        {
            throw new ArgumentException("Not a member access", nameof(member));
        }

        if (checkForSingleDot)
        {
            return memberExpr.Expression is ParameterExpression ? memberExpr.Member : throw new ArgumentException("Argument passed contains more than a single dot which is not allowed: " + member, nameof(member));
        }

        return memberExpr.Member;
    }
}