namespace NServiceBus.AcceptanceTests.Exceptions
{
    using System;
    using System.Reflection;

    static class SerializerCorrupter
    {
        public static Restorer Corrupt()
        {
            var msmqUtilitiesType = Type.GetType("NServiceBus.MsmqUtilities, NServiceBus.Core");
            var headerSerializerField = msmqUtilitiesType.GetField("headerSerializer", BindingFlags.Static | BindingFlags.NonPublic);
            var value = headerSerializerField.GetValue(null);
            headerSerializerField.SetValue(null, null);
            return new Restorer(headerSerializerField, value);
        }

        internal sealed class Restorer : IDisposable
        {
            readonly object headerSerializerFieldValue;
            readonly FieldInfo headerSerializerField;

            public Restorer(FieldInfo headerSerializerField, object headerSerializerFieldValue)
            {
                this.headerSerializerField = headerSerializerField;
                this.headerSerializerFieldValue = headerSerializerFieldValue;
            }

            public void Dispose()
            {
                headerSerializerField.SetValue(null, headerSerializerFieldValue);
            }
        }
    }
}