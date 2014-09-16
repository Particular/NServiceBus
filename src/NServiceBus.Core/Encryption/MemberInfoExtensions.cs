namespace NServiceBus.Encryption
{
    using System;
    using System.Reflection;
    using Utils.Reflection;

    static class MemberInfoExtensions
    {
        public static object GetValue(this MemberInfo member, object source)
        {
            var fieldInfo = member as FieldInfo;

            if (fieldInfo != null)
            {
                var field = DelegateFactory.CreateGet(fieldInfo);
                return field.Invoke(source);
            }

            var propertyInfo = (PropertyInfo) member;
            
            if (!propertyInfo.CanRead)
            {
                if (propertyInfo.PropertyType.IsValueType)
                {
                    return Activator.CreateInstance(propertyInfo.PropertyType);
                }

                return null;
            }

            var property = DelegateFactory.CreateGet(propertyInfo);
            return property.Invoke(source);
        }

        public static void SetValue(this MemberInfo member, object target, object value)
        {
            var fieldInfo = member as FieldInfo;

            if (fieldInfo != null)
            {
                var fieldSet = DelegateFactory.CreateSet(fieldInfo);
                fieldSet.Invoke(target, value);
            }
            else
            {
                var propertyInfo = member as PropertyInfo;
                var propertySet = DelegateFactory.CreateSet(propertyInfo);
                propertySet.Invoke(target, value);
            }
        }
    }
}