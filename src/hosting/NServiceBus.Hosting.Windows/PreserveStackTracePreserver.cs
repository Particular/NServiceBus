namespace NServiceBus.Hosting.Windows
{
    using System;
    using System.Runtime.Serialization;

    static class PreserveStackTracePreserver
    {
        public static void PreserveStackTrace(this Exception exception)
        {
            var context = new StreamingContext(StreamingContextStates.CrossAppDomain);
            var objectManager = new ObjectManager(null, context);
            var serializationInfo = new SerializationInfo(exception.GetType(), new FormatterConverter());

            exception.GetObjectData(serializationInfo, context);
            objectManager.RegisterObject(exception, 1, serializationInfo); // prepare for SetObjectData
            objectManager.DoFixups(); // ObjectManager calls SetObjectData
        }
    }
}