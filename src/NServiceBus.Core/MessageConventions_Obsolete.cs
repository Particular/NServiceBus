#pragma warning disable 1591
// ReSharper disable UnusedParameter.Global
namespace NServiceBus
{
    using System;
    using System.Reflection;
    
    [ObsoleteEx(
        RemoveInVersion = "6",
        TreatAsErrorFromVersion = "5",
        Replacement = "Use configuration.Conventions().DefiningMessagesAs(definesMessageType), where configuration is an instance of type BusConfiguration")]
    public static class MessageConventions
    {
        [ObsoleteEx(
            RemoveInVersion = "6",
            TreatAsErrorFromVersion = "5",
            Replacement = "Use configuration.Conventions().DefiningMessagesAs(definesMessageType), where configuration is an instance of type BusConfiguration")]
        public static Configure DefiningMessagesAs(this Configure config, Func<Type, bool> definesMessageType)
        {
            throw new NotImplementedException();

        }

        [ObsoleteEx(
            RemoveInVersion = "6",
            TreatAsErrorFromVersion = "5",
            Replacement = "Use configuration.Conventions().DefiningCommandsAs(definesCommandType), where configuration is an instance of type BusConfiguration")]
        public static Configure DefiningCommandsAs(this Configure config, Func<Type, bool> definesCommandType)
        {
            throw new NotImplementedException();

        }

        [ObsoleteEx(
            RemoveInVersion = "6",
            TreatAsErrorFromVersion = "5",
            Replacement = "Use configuration.Conventions().DefiningEventsAs(definesEventType), where configuration is an instance of type BusConfiguration")]
        public static Configure DefiningEventsAs(this Configure config, Func<Type, bool> definesEventType)
        {
            throw new NotImplementedException();

        }

        [ObsoleteEx(
            RemoveInVersion = "6",
            TreatAsErrorFromVersion = "5",
            Replacement = "Use configuration.Conventions().DefiningEncryptedPropertiesAs(definesEncryptedProperty), where configuration is an instance of type BusConfiguration")]
        public static Configure DefiningEncryptedPropertiesAs(this Configure config, Func<PropertyInfo, bool> definesEncryptedProperty)
        {
            throw new NotImplementedException();

        }

        [ObsoleteEx(
            RemoveInVersion = "6",
            TreatAsErrorFromVersion = "5",
            Replacement = "Use configuration.Conventions().DefiningDataBusPropertiesAs(definesDataBusProperty), where configuration is an instance of type BusConfiguration")]
        public static Configure DefiningDataBusPropertiesAs(this Configure config, Func<PropertyInfo, bool> definesDataBusProperty)
        {
            throw new NotImplementedException();

        }

        [ObsoleteEx(
            RemoveInVersion = "6",
            TreatAsErrorFromVersion = "5",
            Replacement = "Use configuration.Conventions().DefiningTimeToBeReceivedAs(retrieveTimeToBeReceived), where configuration is an instance of type BusConfiguration")]
        public static Configure DefiningTimeToBeReceivedAs(this Configure config, Func<Type, TimeSpan> retrieveTimeToBeReceived)
        {
            throw new NotImplementedException();

        }

        [ObsoleteEx(
            RemoveInVersion = "6",
            TreatAsErrorFromVersion = "5",
            Replacement = "Use configuration.Conventions().DefiningExpressMessagesAs(definesExpressMessageType), where configuration is an instance of type BusConfiguration")]
        public static Configure DefiningExpressMessagesAs(this Configure config, Func<Type, bool> definesExpressMessageType)
        {
            throw new NotImplementedException();
        }
    }

}
