// ReSharper disable UnusedParameter.Global
namespace NServiceBus
{
    using System;
    using Logging;

    [ObsoleteEx(
        RemoveInVersion = "6.0",
        TreatAsErrorFromVersion = "5.0",
        Message = "Log4Net and Nlog integration has been moved to a stand alone nugets, 'NServiceBus.Log4Net' and 'NServiceBus.NLog'.")]
    public static class SetLoggingLibrary
    {

        [ObsoleteEx(
            RemoveInVersion = "6.0",
            TreatAsErrorFromVersion = "5.0",
            Message = "Log4Net integration has been moved to a stand alone nuget 'NServiceBus.Log4Net'.",
            Replacement = "Install the 'NServiceBus.Log4Net' nuget and run 'Log4NetConfigurator.Configure'.")]
        public static Configure Log4Net(this Configure config)
        {
            throw new NotImplementedException();
        }

        [ObsoleteEx(
            RemoveInVersion = "6.0",
            TreatAsErrorFromVersion = "5.0",
            Message = "Log4Net integration has been moved to a stand alone nuget 'NServiceBus.Log4Net'.",
            Replacement = "Install the 'NServiceBus.Log4Net' nuget and run 'Log4NetConfigurator.Configure'.")]
        public static Configure Log4Net<TAppender>(this Configure config, Action<TAppender> initializeAppender) where TAppender : new()
        {
            throw new NotImplementedException();
        }

        [ObsoleteEx(
            RemoveInVersion = "6.0",
            TreatAsErrorFromVersion = "5.0",
            Message = "Log4Net integration has been moved to a stand alone nuget 'NServiceBus.Log4Net'.",
            Replacement = "Install the 'NServiceBus.Log4Net' nuget and run 'Log4NetConfigurator.Configure'.")]
        public static Configure Log4Net(this Configure config, object appenderSkeleton)
        {
            throw new NotImplementedException();
        }


        [ObsoleteEx(
            RemoveInVersion = "6.0",
            TreatAsErrorFromVersion = "5.0",
            Message = "Log4Net integration has been moved to a stand alone nuget 'NServiceBus.Log4Net'.",
            Replacement = "Install the 'NServiceBus.Log4Net' nuget and run 'Log4NetConfigurator.Configure'.")]
        public static void Log4Net()
        {
            throw new NotImplementedException();
        }

        [ObsoleteEx(
            RemoveInVersion = "6.0",
            TreatAsErrorFromVersion = "5.0",
            Message = "Log4Net integration has been moved to a stand alone nuget 'NServiceBus.Log4Net'.",
            Replacement = "Install the 'NServiceBus.Log4Net' nuget and run 'Log4NetConfigurator.Configure'.")]
        public static void Log4Net(Action config)
        {
            throw new NotImplementedException();
        }

        [ObsoleteEx(
            RemoveInVersion = "6.0",
            TreatAsErrorFromVersion = "5.0",
            Message = "Nlog integration has been moved to a stand alone nuget 'NServiceBus.NLog'.",
            Replacement = "Install the 'NServiceBus.NLog' nuget and run 'NLogConfigurator.Configure'.")]
        public static Configure NLog(this Configure config, params object[] targetsForNServiceBusToLogTo)
        {
            throw new NotImplementedException();
        }

        [ObsoleteEx(
            RemoveInVersion = "6.0", 
            TreatAsErrorFromVersion = "5.0",
            Message = "Nlog integration has been moved to a stand alone nuget 'NServiceBus.NLog'.",
            Replacement = "Install the 'NServiceBus.NLog' nuget and run 'NLogConfigurator.Configure'.")]
        public static void NLog()
        {
            throw new NotImplementedException();
        }

        [ObsoleteEx(
            RemoveInVersion = "6.0", 
            TreatAsErrorFromVersion = "5.0",
            Replacement = "LogManager.LoggerFactory = new MyCustomLoggerFactory()")]
        public static void Custom(ILoggerFactory loggerFactory)
        {
            LogManager.LoggerFactory = loggerFactory;
        }

   
    }
}