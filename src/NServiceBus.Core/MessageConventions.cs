namespace NServiceBus
{
    using System;
    using System.Reflection;

    /// <summary>
    /// Static extension methods to Configure.
    /// </summary>
    public static class MessageConventions
    {
        /// <summary>
        /// Sets the function to be used to evaluate whether a type is a message.
        /// </summary>
        [ObsoleteEx(
            RemoveInVersion = "6",
            TreatAsErrorFromVersion = "5",
            Replacement = "Configure.With(b=> b.Conventions(c=> c.DefiningMessagesAs(definesMessageType)))")]
        public static Configure DefiningMessagesAs(this Configure config, Func<Type, bool> definesMessageType)
        {
            throw new NotImplementedException();

        }

        /// <summary>
        /// Sets the function to be used to evaluate whether a type is a commands.
        /// </summary>
        [ObsoleteEx(
            RemoveInVersion = "6",
            TreatAsErrorFromVersion = "5",
            Replacement = "Configure.With(b=> b.Conventions(c=> c.DefiningCommandsAs(definesCommandType)))")]
        public static Configure DefiningCommandsAs(this Configure config, Func<Type, bool> definesCommandType)
        {
            throw new NotImplementedException();

        }

        /// <summary>
        /// Sets the function to be used to evaluate whether a type is a event.
        /// </summary>
        [ObsoleteEx(
            RemoveInVersion = "6",
            TreatAsErrorFromVersion = "5",
            Replacement = "Configure.With(b=> b.Conventions(c=> c.DefiningEventsAs(definesEventType)))")]
        public static Configure DefiningEventsAs(this Configure config, Func<Type, bool> definesEventType)
        {
            throw new NotImplementedException();

        }

        /// <summary>
        /// Sets the function to be used to evaluate whether a property should be encrypted or not.
        /// </summary>
        [ObsoleteEx(
            RemoveInVersion = "6",
            TreatAsErrorFromVersion = "5",
            Replacement = "Configure.With(b=> b.Conventions(c=> c.DefiningEncryptedPropertiesAs(definesEncryptedProperty)))")]
        public static Configure DefiningEncryptedPropertiesAs(this Configure config, Func<PropertyInfo, bool> definesEncryptedProperty)
        {
            throw new NotImplementedException();

        }

        /// <summary>
        /// Sets the function to be used to evaluate whether a property should be sent via the DataBus or not.
        /// </summary>
        [ObsoleteEx(
            RemoveInVersion = "6",
            TreatAsErrorFromVersion = "5",
            Replacement = "Configure.With(b=> b.Conventions(c=> c.DefiningDataBusPropertiesAs(definesDataBusProperty)))")]
        public static Configure DefiningDataBusPropertiesAs(this Configure config, Func<PropertyInfo, bool> definesDataBusProperty)
        {
            throw new NotImplementedException();

        }

        /// <summary>
        /// Sets the function to be used to evaluate whether a message has a time to be received.
        /// </summary>
        [ObsoleteEx(
            RemoveInVersion = "6",
            TreatAsErrorFromVersion = "5",
            Replacement = "Configure.With(b=> b.Conventions(c=> c.DefiningTimeToBeReceivedAs(retrieveTimeToBeReceived)))")]
        public static Configure DefiningTimeToBeReceivedAs(this Configure config, Func<Type, TimeSpan> retrieveTimeToBeReceived)
        {
            throw new NotImplementedException();

        }

        /// <summary>
        /// Sets the function to be used to evaluate whether a type is an express message or not.
        /// </summary>
        [ObsoleteEx(
            RemoveInVersion = "6",
            TreatAsErrorFromVersion = "5",
            Replacement = "Configure.With(b=> b.Conventions(c=> c.DefiningExpressMessagesAs(definesExpressMessageType)))")]
        public static Configure DefiningExpressMessagesAs(this Configure config, Func<Type, bool> definesExpressMessageType)
        {
            throw new NotImplementedException();
        }
    }
}
