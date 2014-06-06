#pragma warning disable 1591
// ReSharper disable UnusedParameter.Global
using System;
namespace NServiceBus
{
    [ObsoleteEx(
        Message = "Since the case where this exception was thrown should not be handled by consumers of the API it has been removed", 
        RemoveInVersion = "6", 
        TreatAsErrorFromVersion = "5")]
    public class MessageConventionException : Exception
    {
    }
}

namespace NServiceBus
{
    [ObsoleteEx(
        Message = "Since the case where this exception was thrown should not be handled by consumers of the API it has been removed", 
        RemoveInVersion = "6", 
        TreatAsErrorFromVersion = "5")]
    public class ConcurrencyException : Exception
    {
    }
}

namespace NServiceBus.IdGeneration
{
    [ObsoleteEx(
        Message = "This class was never intended to be exposed as part of the public API.",
        RemoveInVersion = "6",
        TreatAsErrorFromVersion = "5")]
    public static class CombGuid
    {
    }
}

namespace NServiceBus.Utils
{
    [ObsoleteEx(
        Message = "This class was never intended to be exposed as part of the public API.",
        RemoveInVersion = "6",
        TreatAsErrorFromVersion = "5")]
    public static class RegistryReader<T>
    {
    }
}

namespace NServiceBus.Utils
{
    [ObsoleteEx(
        Message = "This class was never intended to be exposed as part of the public API.", 
        RemoveInVersion = "6", 
        TreatAsErrorFromVersion = "5")]
    public class FileVersionRetriever
    {
    }
}
namespace NServiceBus.Unicast
{
    [ObsoleteEx(Replacement = "ICallback",
        RemoveInVersion = "6",
        TreatAsErrorFromVersion = "5")]
    public class Callback
    {
    }
}
namespace NServiceBus.Unicast
{
    [ObsoleteEx(
        Replacement = "IBus",
        RemoveInVersion = "6",
        TreatAsErrorFromVersion = "5")]
    public class IUnicastBus
    {
    }
}

namespace NServiceBus.Hosting
{
    [ObsoleteEx(
        Message = "This class was never intended to be exposed as part of the public API.", 
        RemoveInVersion = "6",
        TreatAsErrorFromVersion = "5")]
    public class GenericHost
    {
    }
}

namespace NServiceBus.Hosting
{
    [ObsoleteEx(
        Message = "This class was never intended to be exposed as part of the public API.", 
        RemoveInVersion = "6", 
        TreatAsErrorFromVersion = "5")]
    public class IHost
    {
    }
}


namespace System.Threading.Tasks.Schedulers
{
    [ObsoleteEx(
        Message = "This class was never intended to be exposed as part of the public API.", 
        RemoveInVersion = "6",
        TreatAsErrorFromVersion = "5")]
    public sealed class MTATaskScheduler 
    {
    }
}
namespace NServiceBus.Logging.Log4NetBridge
{
    [ObsoleteEx(
        Message = "Sensible defaults for logging are now built into NServicebus. To customise logging there are external nuget packages available to connect NServiceBus to the various popular logging frameworks.", 
        RemoveInVersion = "6", 
        TreatAsErrorFromVersion = "5")]
    public class ConfigureInternalLog4NetBridge
    {
    }
}
namespace NServiceBus.Logging.Loggers
{
    [ObsoleteEx(
        Message = "Sensible defaults for logging are now built into NServicebus. To customise logging there are external nuget packages available to connect NServiceBus to the various popular logging frameworks.", 
        RemoveInVersion = "6",
        TreatAsErrorFromVersion = "5")]
    public class ConsoleLogger
    {
    }
}
namespace NServiceBus.Logging.Loggers
{
    [ObsoleteEx(
        Message = "Sensible defaults for logging are now built into NServicebus. To customise logging there are external nuget packages available to connect NServiceBus to the various popular logging frameworks.",
        RemoveInVersion = "6", 
        TreatAsErrorFromVersion = "5")]
    public class ConsoleLoggerFactory
    {
    }
}
namespace NServiceBus.Logging.Loggers.Log4NetAdapter
{
    [ObsoleteEx(
        Message = "Sensible defaults for logging are now built into NServicebus. To customise logging there are external nuget packages available to connect NServiceBus to the various popular logging frameworks.", 
        RemoveInVersion = "6",
        TreatAsErrorFromVersion = "5")]
    public class Log4NetAppenderFactory
    {
    }
}
namespace NServiceBus.Logging.Loggers.Log4NetAdapter
{
    [ObsoleteEx(
        Message = "Sensible defaults for logging are now built into NServicebus. To customise logging there are external nuget packages available to connect NServiceBus to the various popular logging frameworks.", 
        RemoveInVersion = "6", 
        TreatAsErrorFromVersion = "5")]
    public class Log4NetConfigurator
    {
    }
}
namespace NServiceBus.Logging.Loggers.Log4NetAdapter
{
    [ObsoleteEx(
        Message = "Sensible defaults for logging are now built into NServicebus. To customise logging there are external nuget packages available to connect NServiceBus to the various popular logging frameworks.",
        RemoveInVersion = "6", 
        TreatAsErrorFromVersion = "5")]
    public class Log4NetLogger
    {
    }
}
namespace NServiceBus.Logging.Loggers.Log4NetAdapter
{
    [ObsoleteEx(
        Message = "Sensible defaults for logging are now built into NServicebus. To customise logging there are external nuget packages available to connect NServiceBus to the various popular logging frameworks.",
        RemoveInVersion = "6", 
        TreatAsErrorFromVersion = "5")]
    public class Log4NetLoggerFactory
    {
    }
}

namespace NServiceBus.Logging.Loggers.NLogAdapter
{
    [ObsoleteEx(
        Message = "Sensible defaults for logging are now built into NServicebus. To customise logging there are external nuget packages available to connect NServiceBus to the various popular logging frameworks.", 
        RemoveInVersion = "6", TreatAsErrorFromVersion = "5")]
    public class NLogConfigurator
    {
    }
}

namespace NServiceBus.Logging.Loggers.NLogAdapter
{
    [ObsoleteEx(
        Message = "Sensible defaults for logging are now built into NServicebus. To customise logging there are external nuget packages available to connect NServiceBus to the various popular logging frameworks.",
        RemoveInVersion = "6",
        TreatAsErrorFromVersion = "5")]
    public class NLogLogger
    {
    }
}
namespace NServiceBus.Logging.Loggers.NLogAdapter
{
    [ObsoleteEx(
        Message = "Sensible defaults for logging are now built into NServicebus. To customise logging there are external nuget packages available to connect NServiceBus to the various popular logging frameworks.",
        RemoveInVersion = "6", 
        TreatAsErrorFromVersion = "5")]
    public class NLogLoggerFactory
    {
    }
}
namespace NServiceBus.Logging.Loggers.NLogAdapter
{
    [ObsoleteEx(
        Message = "Sensible defaults for logging are now built into NServicebus. To customise logging there are external nuget packages available to connect NServiceBus to the various popular logging frameworks.",
        RemoveInVersion = "6", 
        TreatAsErrorFromVersion = "5")]
    public class NLogTargetFactory
    {
    }
}
namespace NServiceBus.Logging.Loggers
{
    [ObsoleteEx(
        Message = "Sensible defaults for logging are now built into NServicebus. To customise logging there are external nuget packages available to connect NServiceBus to the various popular logging frameworks.",
        RemoveInVersion = "6", 
        TreatAsErrorFromVersion = "5")]
    public class NullLogger
    {
    }
}
namespace NServiceBus.Logging.Loggers
{
    [ObsoleteEx(
        Message = "Sensible defaults for logging are now built into NServicebus. To customise logging there are external nuget packages available to connect NServiceBus to the various popular logging frameworks.",
        RemoveInVersion = "6", 
        TreatAsErrorFromVersion = "5")]
    public class NullLoggerFactory
    {
    }
}
namespace NServiceBus.Logging
{
    [ObsoleteEx(
        Message = "Since the case where this exception was thrown should not be handled by consumers of the API it has been removed", 
        RemoveInVersion = "6",
        TreatAsErrorFromVersion = "5")]
    public class LoggingLibraryException : Exception
    {
    }
}
namespace NServiceBus
{
    [ObsoleteEx(
        RemoveInVersion = "6", 
        TreatAsErrorFromVersion = "5",
        Replacement = "IHandleProfile is now passed an instance of Configure. IWantCustomInitialization is now expected to return a new instance of Configure.")]
    public interface IWantTheEndpointConfig 
    {
    }
}
namespace NServiceBus.Timeout.Core
{
    [ObsoleteEx(
        Message = "Timeout management is an internal concern and cannot be replaced.",
        RemoveInVersion = "6",
        TreatAsErrorFromVersion = "5")]
    public interface IManageTimeouts 
    {
    }
}
namespace NServiceBus.Installation.Environments
{
    [ObsoleteEx(
        Message = "IEnvironment is no longer required instead use the non generic INeedToInstallSomething and configure.CreateBus().RunInstallers(); to execute them", 
        RemoveInVersion = "6", 
        TreatAsErrorFromVersion = "5")]
    public class Windows 
    {
    }
}
namespace NServiceBus.Installation
{
    [ObsoleteEx(Message = "IEnvironment is no longer required instead use the non generic INeedToInstallSomething and configure.CreateBus().RunInstallers(); to execute them",
        RemoveInVersion = "6", TreatAsErrorFromVersion = "5")]
    public interface IEnvironment 
    {
    }
}
namespace NServiceBus.Installation
{
    [ObsoleteEx(
        Message = "IEnvironment is no longer required instead use the non generic INeedToInstallSomething and configure.CreateBus().RunInstallers(); to execute them",
        RemoveInVersion = "6", 
        TreatAsErrorFromVersion = "5")]
    public class INeedToInstallSomething<T>
    {
    }
}
namespace NServiceBus
{
    [ObsoleteEx(Message = "IEnvironment is no longer required instead use the non generic INeedToInstallSomething and configure.CreateBus().RunInstallers(); to execute them", 
        RemoveInVersion = "6", TreatAsErrorFromVersion = "5")]
    public class Installer<T>
    {
    }
}

namespace NServiceBus
{
    [ObsoleteEx(
        Message = "IEnvironment is no longer required instead use the non generic INeedToInstallSomething and configure.CreateBus().RunInstallers(); to execute them",
        Replacement = "configure.CreateBus().RunInstallers();", 
        RemoveInVersion = "6",
        TreatAsErrorFromVersion = "5")]
    public static class Install
    {

        [ObsoleteEx(
            Message = "IEnvironment is no longer required instead use the non generic INeedToInstallSomething and configure.CreateBus().RunInstallers(); to execute them",
            Replacement = "configure.CreateBus().RunInstallers();",
            RemoveInVersion = "6", 
            TreatAsErrorFromVersion = "5")]
        public static void ForInstallationOn(this Configure config)
        {
        }

        [ObsoleteEx(
            Message = "IEnvironment is no longer required instead use the non generic INeedToInstallSomething and configure.CreateBus().RunInstallers(); to execute them",
            Replacement = "configure.CreateBus().RunInstallers();",
            RemoveInVersion = "6", 
            TreatAsErrorFromVersion = "5")]
        public static void ForInstallationOn(this Configure config, string username)
        {

        }
    }

}

namespace NServiceBus
{
    using System;

    [ObsoleteEx(
        Replacement = "var configure = Configure.With(x => x.EndpointName(\"MyEndpointName\"));",
        RemoveInVersion = "6",
        TreatAsErrorFromVersion = "5")]
    public static class EndpointConventions
    {
        [ObsoleteEx(
            Replacement = "var configure = Configure.With(x => x.EndpointName(\"MyEndpointName\"));",
            RemoveInVersion = "6",
            TreatAsErrorFromVersion = "5")]
        public static void DefineEndpointName(this Configure config, Func<string> definesEndpointName)
        {
        }

        [ObsoleteEx(
            Replacement = "var configure = Configure.With(x => x.EndpointName(\"MyEndpointName\"));",
            RemoveInVersion = "6",
            TreatAsErrorFromVersion = "5")]
        public static void DefineEndpointName(this Configure config, string name)
        {
        }

    }
}