using System;
using System.Runtime.Serialization;


namespace NServiceBus.Hosting.Windows
{
    public static class PreserveStackTracePreserver
    {
        public static void PreserveStackTrace(this Exception e)
        {
            var context = new StreamingContext(StreamingContextStates.CrossAppDomain);
            var objectManager = new ObjectManager(null, context);
            var serializationInfo = new SerializationInfo(e.GetType(), new FormatterConverter());

            e.GetObjectData(serializationInfo, context);
            objectManager.RegisterObject(e, 1, serializationInfo); // prepare for SetObjectData
            objectManager.DoFixups(); // ObjectManager calls SetObjectData
        }
    }
}