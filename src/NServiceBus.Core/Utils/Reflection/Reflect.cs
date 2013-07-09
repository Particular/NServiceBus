//http://netfx.googlecode.com/svn/trunk/Source/Reflection/Reflect.cs

namespace NServiceBus.Utils.Reflection
{
    using System;
    using System.Linq.Expressions;
    using System.Reflection;

    /// <summary>
    /// Provides strong-typed reflection of the <typeparamref name="TTarget"/> 
    /// type.
    /// </summary>
    /// <typeparam name="TTarget">Type to reflect.</typeparam>
    static class Reflect<TTarget> 
    {
        /// <summary>
        /// Gets the property represented by the lambda expression.
        /// </summary>
        /// <exception cref="ArgumentNullException">The <paramref name="property"/> is null.</exception>
        /// <exception cref="ArgumentException">The <paramref name="property"/> is not a lambda expression or it does not represent a property access.</exception>
        public static PropertyInfo GetProperty(Expression<Func<TTarget, object>> property)
        {
            var info = GetMemberInfo(property, false) as PropertyInfo;
            if (info == null) throw new ArgumentException("Member is not a property");

            return info;
        }

        /// <summary>
        /// Gets the property represented by the lambda expression.        
        /// </summary>
        /// <param name="property"></param>
        /// <param name="checkForSingleDot">If checkForSingleDot is true, then the property expression is checked to see that only a single dot is present.</param>
        /// <returns></returns>
        public static PropertyInfo GetProperty(Expression<Func<TTarget, object>> property, bool checkForSingleDot)
        {
            return GetMemberInfo(property, checkForSingleDot) as PropertyInfo;
        }

        /// <summary>
        /// Returns a MemberInfo for an expression containing a call to a property.
        /// </summary>
        /// <param name="member"></param>
        /// <param name="checkForSingleDot">Checks that the member expression doesn't have more than one dot like a.Prop.Val</param>
        /// <returns></returns>
        static MemberInfo GetMemberInfo(Expression member, bool checkForSingleDot)
        {
            if (member == null) throw new ArgumentNullException("member");

            var lambda = member as LambdaExpression;
            if (lambda == null) throw new ArgumentException("Not a lambda expression", "member");

            MemberExpression memberExpr = null;

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

            if (memberExpr == null) throw new ArgumentException("Not a member access", "member");

            if (checkForSingleDot)
            {
                if (memberExpr.Expression is ParameterExpression)
                {
                    return memberExpr.Member;
                }
                throw new ArgumentException("Argument passed contains more than a single dot which is not allowed: " + member, "member");
            }

            return memberExpr.Member;
        }

    }
}