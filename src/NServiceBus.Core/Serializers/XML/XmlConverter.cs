namespace NServiceBus.Serializers.XML
{
    using System;
    using System.Xml;

    class XmlConverter
    {

        public static string ConvertToString(object value)
        {
            if (value is bool)
            {
                return XmlConvert.ToString((bool)value);
            }
            if (value is byte)
            {
                return XmlConvert.ToString((byte)value);
            }
            if (value is char)
            {
                return StringEscaper.Escape((char)value);
            }
            if (value is double)
            {
                return XmlConvert.ToString((double)value);
            }
            if (value is ulong)
            {
                return XmlConvert.ToString((ulong)value);
            }
            if (value is uint)
            {
                return XmlConvert.ToString((uint)value);
            }
            if (value is ushort)
            {
                return XmlConvert.ToString((ushort)value);
            }
            if (value is long)
            {
                return XmlConvert.ToString((long)value);
            }
            if (value is int)
            {
                return XmlConvert.ToString((int)value);
            }
            if (value is short)
            {
                return XmlConvert.ToString((short)value);
            }
            if (value is sbyte)
            {
                return XmlConvert.ToString((sbyte)value);
            }
            if (value is decimal)
            {
                return XmlConvert.ToString((decimal)value);
            }
            if (value is float)
            {
                return XmlConvert.ToString((float)value);
            }
            if (value is Guid)
            {
                return XmlConvert.ToString((Guid)value);
            }
            if (value is DateTime)
            {
                return XmlConvert.ToString((DateTime)value, XmlDateTimeSerializationMode.RoundtripKind);
            }
            if (value is DateTimeOffset)
            {
                return XmlConvert.ToString((DateTimeOffset)value);
            }
            if (value is TimeSpan)
            {
                return XmlConvert.ToString((TimeSpan)value);
            }
            if (value is string)
            {
                return StringEscaper.Escape((string)value);
            }

            return value.ToString();
        }
    }
}