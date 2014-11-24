namespace NServiceBus.AcceptanceTests.Exceptions
{
    using System;
    using System.Reflection;

    static class SerializerCorrupter
    {

        public static void Corrupt()
        {
            var msmqUtilitiesType = Type.GetType("NServiceBus.Transports.Msmq.MsmqUtilities, NServiceBus.Core");
            var headerSerializerField = msmqUtilitiesType.GetField("headerSerializer", BindingFlags.Static | BindingFlags.NonPublic);
            headerSerializerField.SetValue(null, null);
        }

    }
}