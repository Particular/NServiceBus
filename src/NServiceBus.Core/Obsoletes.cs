#pragma warning disable 1591
namespace NServiceBus
{
    using System;

    [ObsoleteEx(Message = "Since the case where this exception was thrown should not be handled by consumers of the API it has been removed", RemoveInVersion = "6", TreatAsErrorFromVersion = "5")]
    public class MessageConventionException : Exception
    {
    }

}

namespace NServiceBus.IdGeneration
{
    [ObsoleteEx(Message = "This class was never intended to be exposed as part of the public API.", RemoveInVersion = "6", TreatAsErrorFromVersion = "5")]
    public static class CombGuid
    {
    }
}

namespace NServiceBus.Utils
{
    [ObsoleteEx(Message = "This class was never intended to be exposed as part of the public API.", RemoveInVersion = "6", TreatAsErrorFromVersion = "5")]
    public static class RegistryReader<T>
    {
    }
}

namespace NServiceBus.Utils
{
    [ObsoleteEx(Message = "This class was never intended to be exposed as part of the public API.", RemoveInVersion = "6", TreatAsErrorFromVersion = "5")]
    public class FileVersionRetriever
    {
    }
}
namespace NServiceBus.Unicast
{
    [ObsoleteEx(Replacement = "ICallback", RemoveInVersion = "6", TreatAsErrorFromVersion = "5")]
    public class Callback
    {
    }
}
namespace NServiceBus.Unicast
{
    [ObsoleteEx(Replacement = "IBus", RemoveInVersion = "6", TreatAsErrorFromVersion = "5")]
    public class IUnicastBus
    {
    }
}

namespace NServiceBus.Hosting
{
    [ObsoleteEx(Message = "This class was never intended to be exposed as part of the public API.", RemoveInVersion = "6", TreatAsErrorFromVersion = "5")]
    public class GenericHost
    {
    }
}

namespace NServiceBus.Hosting
{
    [ObsoleteEx(Message = "This class was never intended to be exposed as part of the public API.", RemoveInVersion = "6", TreatAsErrorFromVersion = "5")]
    public class IHost
    {
    }
}


namespace System.Threading.Tasks.Schedulers
{
    [ObsoleteEx(Message = "This class was never intended to be exposed as part of the public API.", RemoveInVersion = "6", TreatAsErrorFromVersion = "5")]
    public sealed class MTATaskScheduler 
    {
    }
}
namespace NServiceBus.Logging.Log4NetBridge
{
    [ObsoleteEx(Message = "Sensible defaults for logging are now built into NServicebus. To customise logging there are external nuget packages available to connect NServiceBus to the various popular logging frameworks.", RemoveInVersion = "6", TreatAsErrorFromVersion = "5")]
    public class ConfigureInternalLog4NetBridge
    {
    }
}
namespace NServiceBus.Logging.Loggers
{
    [ObsoleteEx(Message = "Sensible defaults for logging are now built into NServicebus. To customise logging there are external nuget packages available to connect NServiceBus to the various popular logging frameworks.", RemoveInVersion = "6", TreatAsErrorFromVersion = "5")]
    public class ConsoleLogger
    {
    }
}
namespace NServiceBus.Logging.Loggers
{
    [ObsoleteEx(Message = "Sensible defaults for logging are now built into NServicebus. To customise logging there are external nuget packages available to connect NServiceBus to the various popular logging frameworks.", RemoveInVersion = "6", TreatAsErrorFromVersion = "5")]
    public class ConsoleLoggerFactory
    {
    }
}
namespace NServiceBus.Logging.Loggers.Log4NetAdapter
{
    [ObsoleteEx(Message = "Sensible defaults for logging are now built into NServicebus. To customise logging there are external nuget packages available to connect NServiceBus to the various popular logging frameworks.", RemoveInVersion = "6", TreatAsErrorFromVersion = "5")]
    public class Log4NetAppenderFactory
    {
    }
}
namespace NServiceBus.Logging.Loggers.Log4NetAdapter
{
    [ObsoleteEx(Message = "Sensible defaults for logging are now built into NServicebus. To customise logging there are external nuget packages available to connect NServiceBus to the various popular logging frameworks.", RemoveInVersion = "6", TreatAsErrorFromVersion = "5")]
    public class Log4NetConfigurator
    {
    }
}
namespace NServiceBus.Logging.Loggers.Log4NetAdapter
{
    [ObsoleteEx(Message = "Sensible defaults for logging are now built into NServicebus. To customise logging there are external nuget packages available to connect NServiceBus to the various popular logging frameworks.", RemoveInVersion = "6", TreatAsErrorFromVersion = "5")]
    public class Log4NetLogger
    {
    }
}
namespace NServiceBus.Logging.Loggers.Log4NetAdapter
{
    [ObsoleteEx(Message = "Sensible defaults for logging are now built into NServicebus. To customise logging there are external nuget packages available to connect NServiceBus to the various popular logging frameworks.", RemoveInVersion = "6", TreatAsErrorFromVersion = "5")]
    public class Log4NetLoggerFactory
    {
    }
}
namespace NServiceBus.Logging.Loggers.NLogAdapter
{
    [ObsoleteEx(Message = "Sensible defaults for logging are now built into NServicebus. To customise logging there are external nuget packages available to connect NServiceBus to the various popular logging frameworks.", RemoveInVersion = "6", TreatAsErrorFromVersion = "5")]
    public class NLogConfigurator
    {
    }
}
namespace NServiceBus.Logging.Loggers.NLogAdapter
{
    [ObsoleteEx(Message = "Sensible defaults for logging are now built into NServicebus. To customise logging there are external nuget packages available to connect NServiceBus to the various popular logging frameworks.", RemoveInVersion = "6", TreatAsErrorFromVersion = "5")]
    public class NLogLogger
    {
    }
}
namespace NServiceBus.Logging.Loggers.NLogAdapter
{
    [ObsoleteEx(Message = "Sensible defaults for logging are now built into NServicebus. To customise logging there are external nuget packages available to connect NServiceBus to the various popular logging frameworks.", RemoveInVersion = "6", TreatAsErrorFromVersion = "5")]
    public class NLogLoggerFactory
    {
    }
}
namespace NServiceBus.Logging.Loggers.NLogAdapter
{
    [ObsoleteEx(Message = "Sensible defaults for logging are now built into NServicebus. To customise logging there are external nuget packages available to connect NServiceBus to the various popular logging frameworks.", RemoveInVersion = "6", TreatAsErrorFromVersion = "5")]
    public class NLogTargetFactory
    {
    }
}
namespace NServiceBus.Logging.Loggers
{
    [ObsoleteEx(Message = "Sensible defaults for logging are now built into NServicebus. To customise logging there are external nuget packages available to connect NServiceBus to the various popular logging frameworks.", RemoveInVersion = "6", TreatAsErrorFromVersion = "5")]
    public class NullLogger
    {
    }
}
namespace NServiceBus.Logging.Loggers
{
    [ObsoleteEx(Message = "Sensible defaults for logging are now built into NServicebus. To customise logging there are external nuget packages available to connect NServiceBus to the various popular logging frameworks.", RemoveInVersion = "6", TreatAsErrorFromVersion = "5")]
    public class NullLoggerFactory
    {
    }
}