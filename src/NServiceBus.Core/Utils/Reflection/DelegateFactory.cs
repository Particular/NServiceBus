namespace NServiceBus
{
    using System;
    using System.Collections.Concurrent;
    using System.Linq.Expressions;
    using System.Reflection;
    using System.Reflection.Emit;

    static class DelegateFactory
    {
        public static Func<object, object> CreateGet(PropertyInfo property)
        {
            Func<object, object> lateBoundPropertyGet;

            if (!PropertyInfoToLateBoundProperty.TryGetValue(property, out lateBoundPropertyGet))
            {
                var instanceParameter = Expression.Parameter(typeof(object), "target");

                var member = Expression.Property(Expression.Convert(instanceParameter, property.DeclaringType), property);

                var lambda = Expression.Lambda<Func<object, object>>(
                    Expression.Convert(member, typeof(object)),
                    instanceParameter
                    );

                lateBoundPropertyGet = lambda.Compile();
                PropertyInfoToLateBoundProperty[property] = lateBoundPropertyGet;
            }

            return lateBoundPropertyGet;
        }

        public static Func<object, object> CreateGet(FieldInfo field)
        {
            Func<object, object> lateBoundFieldGet;

            if (!FieldInfoToLateBoundField.TryGetValue(field, out lateBoundFieldGet))
            {
                var instanceParameter = Expression.Parameter(typeof(object), "target");

                var member = Expression.Field(Expression.Convert(instanceParameter, field.DeclaringType), field);

                var lambda = Expression.Lambda<Func<object, object>>(
                    Expression.Convert(member, typeof(object)),
                    instanceParameter
                    );

                lateBoundFieldGet = lambda.Compile();
                FieldInfoToLateBoundField[field] = lateBoundFieldGet;
            }

            return lateBoundFieldGet;
        }

        public static Action<object, object> CreateSet(FieldInfo field)
        {
            Action<object, object> callback;

            if (!FieldInfoToLateBoundFieldSet.TryGetValue(field, out callback))
            {
                var sourceType = field.DeclaringType;
                var method = new DynamicMethod("Set" + field.Name, null, new[]
                {
                    typeof(object),
                    typeof(object)
                }, true);
                var gen = method.GetILGenerator();

                gen.Emit(OpCodes.Ldarg_0); // Load input to stack
                gen.Emit(OpCodes.Castclass, sourceType); // Cast to source type
                gen.Emit(OpCodes.Ldarg_1); // Load value to stack
                gen.Emit(OpCodes.Unbox_Any, field.FieldType); // Unbox the value to its proper value type
                gen.Emit(OpCodes.Stfld, field); // Set the value to the input field
                gen.Emit(OpCodes.Ret);

                callback = (Action<object, object>) method.CreateDelegate(typeof(Action<object, object>));
                FieldInfoToLateBoundFieldSet[field] = callback;
            }

            return callback;
        }

        public static Action<object, object> CreateSet(PropertyInfo property)
        {
            Action<object, object> result;

            if (!PropertyInfoToLateBoundPropertySet.TryGetValue(property, out result))
            {
                var method = new DynamicMethod("Set" + property.Name, null, new[]
                {
                    typeof(object),
                    typeof(object)
                }, true);
                var gen = method.GetILGenerator();

                var sourceType = property.DeclaringType;
                var setter = property.GetSetMethod(true);

                gen.Emit(OpCodes.Ldarg_0); // Load input to stack
                gen.Emit(OpCodes.Castclass, sourceType); // Cast to source type
                gen.Emit(OpCodes.Ldarg_1); // Load value to stack
                gen.Emit(OpCodes.Unbox_Any, property.PropertyType); // Unbox the value to its proper value type
                gen.Emit(OpCodes.Callvirt, setter); // Call the setter method
                gen.Emit(OpCodes.Ret);

                result = (Action<object, object>) method.CreateDelegate(typeof(Action<object, object>));
                PropertyInfoToLateBoundPropertySet[property] = result;
            }

            return result;
        }

        static ConcurrentDictionary<PropertyInfo, Func<object, object>> PropertyInfoToLateBoundProperty = new ConcurrentDictionary<PropertyInfo, Func<object, object>>();
        static ConcurrentDictionary<FieldInfo, Func<object, object>> FieldInfoToLateBoundField = new ConcurrentDictionary<FieldInfo, Func<object, object>>();
        static ConcurrentDictionary<PropertyInfo, Action<object, object>> PropertyInfoToLateBoundPropertySet = new ConcurrentDictionary<PropertyInfo, Action<object, object>>();
        static ConcurrentDictionary<FieldInfo, Action<object, object>> FieldInfoToLateBoundFieldSet = new ConcurrentDictionary<FieldInfo, Action<object, object>>();
    }
}