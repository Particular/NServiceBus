//http://netfx.googlecode.com/svn/trunk/Source/Reflection/Reflect.cs

namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Reflection;

    static class Reflect<TTarget>
    {
        public static PropertyInfo GetProperty(Expression<Func<TTarget, object>> property)
        {
            var info = GetMemberInfo(property, false) as PropertyInfo;
            if (info == null)
            {
                throw new ArgumentException("Member is not a property");
            }

            return info;
        }

        internal static List<TTarget> GetEnumValues()
        {
            return Enum.GetValues(typeof(TTarget))
                .Cast<TTarget>()
                .ToList();
        }

        public static PropertyInfo GetProperty(Expression<Func<TTarget, object>> property, bool checkForSingleDot)
        {
            return GetMemberInfo(property, checkForSingleDot) as PropertyInfo;
        }

        public static MemberInfo GetMemberInfo(Expression member, bool checkForSingleDot)
        {
            if (member == null)
            {
                throw new ArgumentNullException(nameof(member));
            }

            var lambda = member as LambdaExpression;
            if (lambda == null)
            {
                throw new ArgumentException("Not a lambda expression", nameof(member));
            }

            MemberExpression memberExpr = null;

            // The Func<TTarget, object> we use returns an object, so first statement can be either 
            // a cast (if the field/property does not return an object) or the direct member access.
            if (lambda.Body.NodeType == ExpressionType.Convert)
            {
                // The cast is an unary expression, where the operand is the 
                // actual member access expression.
                memberExpr = ((UnaryExpression) lambda.Body).Operand as MemberExpression;
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
                if (memberExpr.Expression is ParameterExpression)
                {
                    return memberExpr.Member;
                }
                throw new ArgumentException("Argument passed contains more than a single dot which is not allowed: " + member, nameof(member));
            }

            return memberExpr.Member;
        }
    }
}