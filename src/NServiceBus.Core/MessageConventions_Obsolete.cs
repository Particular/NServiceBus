#pragma warning disable 1591
// ReSharper disable UnusedParameter.Global
namespace NServiceBus
{
    using System;
    using System.Reflection;
    
    [ObsoleteEx(
        RemoveInVersion = "6",
        TreatAsErrorFromVersion = "5",
        Replacement = "Configure.With(b=> b.Conventions(c=> c.DefiningMessagesAs(definesMessageType)))")]
    public static class MessageConventions
    {
        [ObsoleteEx(
            RemoveInVersion = "6",
            TreatAsErrorFromVersion = "5",
            Replacement = "Configure.With(b=> b.Conventions(c=> c.DefiningMessagesAs(definesMessageType)))")]
        public static Configure DefiningMessagesAs(this Configure config, Func<Type, bool> definesMessageType)
        {
            throw new NotImplementedException();

        }

        [ObsoleteEx(
            RemoveInVersion = "6",
            TreatAsErrorFromVersion = "5",
            Replacement = "Configure.With(b=> b.Conventions(c=> c.DefiningCommandsAs(definesCommandType)))")]
        public static Configure DefiningCommandsAs(this Configure config, Func<Type, bool> definesCommandType)
        {
            throw new NotImplementedException();

        }

        [ObsoleteEx(
            RemoveInVersion = "6",
            TreatAsErrorFromVersion = "5",
            Replacement = "Configure.With(b=> b.Conventions(c=> c.DefiningEventsAs(definesEventType)))")]
        public static Configure DefiningEventsAs(this Configure config, Func<Type, bool> definesEventType)
        {
            throw new NotImplementedException();

        }

        [ObsoleteEx(
            RemoveInVersion = "6",
            TreatAsErrorFromVersion = "5",
            Replacement = "Configure.With(b=> b.Conventions(c=> c.DefiningEncryptedPropertiesAs(definesEncryptedProperty)))")]
        public static Configure DefiningEncryptedPropertiesAs(this Configure config, Func<PropertyInfo, bool> definesEncryptedProperty)
        {
            throw new NotImplementedException();

        }

        [ObsoleteEx(
            RemoveInVersion = "6",
            TreatAsErrorFromVersion = "5",
            Replacement = "Configure.With(b=> b.Conventions(c=> c.DefiningDataBusPropertiesAs(definesDataBusProperty)))")]
        public static Configure DefiningDataBusPropertiesAs(this Configure config, Func<PropertyInfo, bool> definesDataBusProperty)
        {
            throw new NotImplementedException();

        }

        [ObsoleteEx(
            RemoveInVersion = "6",
            TreatAsErrorFromVersion = "5",
            Replacement = "Configure.With(b=> b.Conventions(c=> c.DefiningTimeToBeReceivedAs(retrieveTimeToBeReceived)))")]
        public static Configure DefiningTimeToBeReceivedAs(this Configure config, Func<Type, TimeSpan> retrieveTimeToBeReceived)
        {
            throw new NotImplementedException();

        }

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
