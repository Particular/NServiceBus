using System;
using System.Runtime.Serialization;


namespace NServiceBus.Hosting.Windows
{
    using System.Reflection;

    static class PreserveStackTracePreserver
    {
        public static void PreserveStackTrace(this Exception exception)
        {

            typeof(Exception).GetMethod("PrepForRemoting",
                BindingFlags.NonPublic | BindingFlags.Instance)
                              .Invoke(exception, new object[0]);


            //var context = new StreamingContext(StreamingContextStates.CrossAppDomain);
            //var objectManager = new ObjectManager(null, context);
            //var serializationInfo = new SerializationInfo(exception.GetType(), new FormatterConverter());

            //exception.GetObjectData(serializationInfo, context);
            //objectManager.RegisterObject(exception, 1, serializationInfo); // prepare for SetObjectData
            //objectManager.DoFixups(); // ObjectManager calls SetObjectData
        }
    }
}