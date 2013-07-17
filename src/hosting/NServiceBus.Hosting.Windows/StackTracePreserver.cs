namespace NServiceBus.Hosting.Windows
{
    using System;
    using System.Runtime.Serialization;

    static class StackTracePreserver
    {
        public static void PreserveStackTrace(this Exception exception)
        {
            try
            {
                var context = new StreamingContext(StreamingContextStates.CrossAppDomain);
                var objectManager = new ObjectManager(null, context);
                var serializationInfo = new SerializationInfo(exception.GetType(), new FormatterConverter());

                exception.GetObjectData(serializationInfo, context);
                objectManager.RegisterObject(exception, 1, serializationInfo); // prepare for SetObjectData
                objectManager.DoFixups(); // ObjectManager calls SetObjectData
            }
            catch (Exception)
            {
                //this is a best effort. if we fail to patch the stack trace just let it go
            }
        }
    }
}