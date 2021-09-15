namespace NServiceBus
{
    using System;
    using SimpleJson;
    using SimpleJson.Reflection;

    // https://github.com/facebook-csharp-sdk/simple-json/issues/15
    class EnumAwareStrategy : PocoJsonSerializerStrategy
    {
        EnumAwareStrategy() { }

        protected override object SerializeEnum(Enum p)
        {
            return p.ToString();
        }

        public override object DeserializeObject(object value, Type type)
        {
            if (!(value is string stringValue))
            {
                return base.DeserializeObject(value, type);
            }
            if (type.IsEnum)
            {
                return Enum.Parse(type, stringValue);
            }

            if (ReflectionUtils.IsNullableType(type))
            {
                var underlyingType = Nullable.GetUnderlyingType(type);
                if (underlyingType.IsEnum)
                {
                    return Enum.Parse(underlyingType, stringValue);
                }
            }

            return base.DeserializeObject(value, type);
        }

        static EnumAwareStrategy enumAwareStrategy;
        public static EnumAwareStrategy Instance => enumAwareStrategy ??= new EnumAwareStrategy();
    }
}