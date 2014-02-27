﻿namespace NServiceBus.Utils
{
    using System;
    using System.Runtime.Serialization;

    static class StackTracePreserver
    {
        public static void PreserveStackTrace(this Exception exception)
        {
            if (!exception.GetType().IsSerializable)
            {
                return;
            }
            try
            {
                var context = new StreamingContext(StreamingContextStates.CrossAppDomain);
                var objectManager = new ObjectManager(null, context);
                var serializationInfo = new SerializationInfo(exception.GetType(), new FormatterConverter());

                exception.GetObjectData(serializationInfo, context);
                objectManager.RegisterObject(exception, 1, serializationInfo); // prepare for SetObjectData
                objectManager.DoFixups(); // ObjectManager calls SetObjectData
            }
            // ReSharper disable once EmptyGeneralCatchClause
            catch (Exception)
            {
                //this is a best effort. if we fail to patch the stack trace just let it go
            }
        }
    }
}
