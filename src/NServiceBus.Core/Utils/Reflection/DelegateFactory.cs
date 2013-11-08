/*
 Added reflection optimization techniques from Nate Kohari and Jimmy Bogard:

http://kohari.org/2009/03/06/fast-late-bound-invocation-with-expression-trees/

http://www.lostechies.com/blogs/jimmy_bogard/archive/2009/06/17/more-on-late-bound-invocations-with-expression-trees.aspx

http://www.lostechies.com/blogs/jimmy_bogard/archive/2009/08/05/late-bound-invocations-with-dynamicmethod.aspx 
 */

namespace NServiceBus.Utils.Reflection
{
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Reflection;
    using System.Reflection.Emit;

    /// <summary>
    /// Late Bound Method
    /// </summary>
    /// <param name="target">Target object</param>
    /// <param name="arguments">Arguments</param>
    public delegate object LateBoundMethod(object target, object[] arguments);

    /// <summary>
    /// Late Bound Property
    /// </summary>
    /// <param name="target">Target Object</param>
    public delegate object LateBoundProperty(object target);

    /// <summary>
    /// Late Bound Field
    /// </summary>
    /// <param name="target">Target Objects </param>
    public delegate object LateBoundField(object target);

    /// <summary>
    /// Late Bound Field Set
    /// </summary>
    /// <param name="target">Target Object</param>
    public delegate void LateBoundFieldSet(object target, object value);

    /// <summary>
    /// Late Bound Property Set
    /// </summary>
    /// <param name="target">Target Object</param>
    public delegate void LateBoundPropertySet(object target, object value);

    /// <summary>
    /// Delegate Factory
    /// </summary>
	public static class DelegateFactory
	{
        static readonly Dictionary<PropertyInfo, LateBoundProperty> PropertyInfoToLateBoundProperty = new Dictionary<PropertyInfo, LateBoundProperty>();
        static readonly Dictionary<FieldInfo, LateBoundField> FieldInfoToLateBoundField = new Dictionary<FieldInfo, LateBoundField>();
        static readonly Dictionary<PropertyInfo, LateBoundPropertySet> PropertyInfoToLateBoundPropertySet = new Dictionary<PropertyInfo, LateBoundPropertySet>();
        static readonly Dictionary<FieldInfo, LateBoundFieldSet> FieldInfoToLateBoundFieldSet = new Dictionary<FieldInfo, LateBoundFieldSet>();
        static readonly Dictionary<MethodInfo, LateBoundMethod> MethodInfoToLateBoundMethod = new Dictionary<MethodInfo, LateBoundMethod>();

        /// <summary>
        /// Create Late Bound methods
        /// </summary>
        /// <param name="method">MethodInfo</param>
        /// <returns>LateBoundMethod</returns>
		public static LateBoundMethod Create(MethodInfo method)
		{
            LateBoundMethod lateBoundMethod;

            if (!MethodInfoToLateBoundMethod.TryGetValue(method, out lateBoundMethod))
            {
                var instanceParameter = Expression.Parameter(typeof(object), "target");
                var argumentsParameter = Expression.Parameter(typeof(object[]), "arguments");

                var call = Expression.Call(
                    Expression.Convert(instanceParameter, method.DeclaringType),
                    method,
                    CreateParameterExpressions(method, argumentsParameter));

                var lambda = Expression.Lambda<LateBoundMethod>(
                    Expression.Convert(call, typeof(object)),
                    instanceParameter,
                    argumentsParameter);

                lateBoundMethod = lambda.Compile();
                MethodInfoToLateBoundMethod[method] = lateBoundMethod;
            }

            return lateBoundMethod;
		}

        /// <summary>
        /// Creates LateBoundProperty
        /// </summary>
        public static LateBoundProperty Create(PropertyInfo property)
        {
            LateBoundProperty lateBoundProperty;

            if (!PropertyInfoToLateBoundProperty.TryGetValue(property, out lateBoundProperty))
            {
                var instanceParameter = Expression.Parameter(typeof(object), "target");

                var member = Expression.Property(Expression.Convert(instanceParameter, property.DeclaringType), property);

                var lambda = Expression.Lambda<LateBoundProperty>(
                    Expression.Convert(member, typeof(object)),
                    instanceParameter
                    );

                lateBoundProperty = lambda.Compile();
                PropertyInfoToLateBoundProperty[property] = lateBoundProperty;
            }

            return lateBoundProperty;
        }
        
        public static LateBoundField Create(FieldInfo field)
        {
            LateBoundField lateBoundField;

            if (!FieldInfoToLateBoundField.TryGetValue(field, out lateBoundField))
            {
                var instanceParameter = Expression.Parameter(typeof(object), "target");

                var member = Expression.Field(Expression.Convert(instanceParameter, field.DeclaringType), field);

                var lambda = Expression.Lambda<LateBoundField>(
                    Expression.Convert(member, typeof(object)),
                    instanceParameter
                    );

                lateBoundField = lambda.Compile();
                FieldInfoToLateBoundField[field] = lateBoundField;
            }

            return lateBoundField;
        }

        /// <summary>
        /// Create filed set 
        /// </summary>
        public static LateBoundFieldSet CreateSet(FieldInfo field)
        {
            LateBoundFieldSet callback;

            if (!FieldInfoToLateBoundFieldSet.TryGetValue(field, out callback))
            {
                var sourceType = field.DeclaringType;
                var method = new DynamicMethod("Set" + field.Name, null, new[] { typeof(object), typeof(object) }, true);
                var gen = method.GetILGenerator();

                gen.Emit(OpCodes.Ldarg_0); // Load input to stack
                gen.Emit(OpCodes.Castclass, sourceType); // Cast to source type
                gen.Emit(OpCodes.Ldarg_1); // Load value to stack
                gen.Emit(OpCodes.Unbox_Any, field.FieldType); // Unbox the value to its proper value type
                gen.Emit(OpCodes.Stfld, field); // Set the value to the input field
                gen.Emit(OpCodes.Ret);

                callback = (LateBoundFieldSet)method.CreateDelegate(typeof(LateBoundFieldSet));
                FieldInfoToLateBoundFieldSet[field] = callback;
            }

            return callback;
        }

        /// <summary>
        /// Creates Property Set 
        /// </summary>
        /// <param name="property">PropertyInfo</param>
        /// <returns>LateBoundPropertySet</returns>
        public static LateBoundPropertySet CreateSet(PropertyInfo property)
        {
            LateBoundPropertySet result;

            if (!PropertyInfoToLateBoundPropertySet.TryGetValue(property, out result))
            {
                var method = new DynamicMethod("Set" + property.Name, null, new[] { typeof(object), typeof(object) }, true);
                var gen = method.GetILGenerator();

                var sourceType = property.DeclaringType;
                var setter = property.GetSetMethod(true);

                gen.Emit(OpCodes.Ldarg_0); // Load input to stack
                gen.Emit(OpCodes.Castclass, sourceType); // Cast to source type
                gen.Emit(OpCodes.Ldarg_1); // Load value to stack
                gen.Emit(OpCodes.Unbox_Any, property.PropertyType); // Unbox the value to its proper value type
                gen.Emit(OpCodes.Callvirt, setter); // Call the setter method
                gen.Emit(OpCodes.Ret);

                result = (LateBoundPropertySet)method.CreateDelegate(typeof(LateBoundPropertySet));
                PropertyInfoToLateBoundPropertySet[property] = result;
            }

            return result;
        }

        private static Expression[] CreateParameterExpressions(MethodInfo method, Expression argumentsParameter)
        {
            return method.GetParameters().Select((parameter, index) =>
                Expression.Convert(
                    Expression.ArrayIndex(argumentsParameter, Expression.Constant(index)),
                    parameter.ParameterType)).ToArray();
        }	
    }
}
