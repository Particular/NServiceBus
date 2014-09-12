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

namespace NServiceBus.Persistence
{
    [ObsoleteEx(
        Message = "Since the case where this exception was thrown should not be handled by consumers of the API it has been removed", 
        RemoveInVersion = "6", 
        TreatAsErrorFromVersion = "5")]
    public class ConcurrencyException : Exception
    {
    }
}

namespace NServiceBus.Unicast.Transport
{
    [ObsoleteEx(
        Message = "Since the case where this exception was thrown should not be handled by consumers of the API it has been removed",
        RemoveInVersion = "6",
        TreatAsErrorFromVersion = "5")]
    public class TransportMessageHandlingFailedException : Exception
    {
    }
}

namespace NServiceBus.Unicast.Queuing
{
    [ObsoleteEx(
        Message = "Since the case where this exception was thrown should not be handled by consumers of the API it has been removed", 
        RemoveInVersion = "6", 
        TreatAsErrorFromVersion = "5")]
    public class FailedToSendMessageException : Exception
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
        Message = "`IHandleProfile` is now passed an instance of `Configure`. `IWantCustomInitialization` is now expected to return a new instance of `Configure`.")]
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
        Message = "IEnvironment is no longer required instead use the non generic `INeedToInstallSomething` and use `configuration.EnableInstallers()`, where `configuration` is an instance of type `BusConfiguration` to execute them.", 
        RemoveInVersion = "6", 
        TreatAsErrorFromVersion = "5")]
    public class Windows 
    {
    }
}
namespace NServiceBus.Installation
{
    [ObsoleteEx(
        Message = "`IEnvironment` is no longer required instead use the non generic `INeedToInstallSomething` and use `configuration.EnableInstallers()`, where `configuration` is an instance of type `BusConfiguration` to execute them.",
        RemoveInVersion = "6", 
        TreatAsErrorFromVersion = "5")]
    public interface IEnvironment 
    {
    }
}
namespace NServiceBus.Installation
{
    [ObsoleteEx(
        Message = "`IEnvironment` is no longer required instead use the non generic `INeedToInstallSomething` and use `configuration.EnableInstallers()`, where `configuration` is an instance of type `BusConfiguration` to execute them.",
        RemoveInVersion = "6", 
        TreatAsErrorFromVersion = "5")]
    public class INeedToInstallSomething<T>
    {
    }
}
namespace NServiceBus
{
    [ObsoleteEx(
        Message = "`IEnvironment` is no longer required instead use the non generic `INeedToInstallSomething` and use `configuration.EnableInstallers()`, where `configuration` is an instance of type `BusConfiguration` to execute them.", 
        RemoveInVersion = "6", 
        TreatAsErrorFromVersion = "5")]
    public class Installer<T>
    {
    }
}

namespace NServiceBus
{
    [ObsoleteEx(
        Message = "`IEnvironment` is no longer required instead use the non generic `INeedToInstallSomething` and use `configuration.EnableInstallers()`, where `configuration` is an instance of type `BusConfiguration` to execute them.",
        RemoveInVersion = "6",
        TreatAsErrorFromVersion = "5")]
    public static class Install
    {

        [ObsoleteEx(
            Message = "`IEnvironment` is no longer required instead use the non generic `INeedToInstallSomething` and use `configuration.EnableInstallers()`, where `configuration` is an instance of type `BusConfiguration` to execute them.",
            RemoveInVersion = "6", 
            TreatAsErrorFromVersion = "5")]
        public static Installer<T> ForInstallationOn<T>(this Configure config)
        {
            throw new NotImplementedException();
        }

        [ObsoleteEx(
            Message = "`IEnvironment` is no longer required instead use the non generic `INeedToInstallSomething` and use `configuration.EnableInstallers()`, where `configuration` is an instance of type `BusConfiguration` to execute them.",
            RemoveInVersion = "6", 
            TreatAsErrorFromVersion = "5")]
        public static Installer<T> ForInstallationOn<T>(this Configure config, string username)
        {
            throw new NotImplementedException();
        }
    }

}

namespace NServiceBus
{
    using System;

    [ObsoleteEx(
        Message = "Use `configuration.EndpointName(myEndpointName)`, where `configuration` is an instance of type `BusConfiguration`.",
        RemoveInVersion = "6",
        TreatAsErrorFromVersion = "5")]
    public static class EndpointConventions
    {
        [ObsoleteEx(
            Message = "Use `configuration.EndpointName(myEndpointName)`, where `configuration` is an instance of type `BusConfiguration`.",
            RemoveInVersion = "6",
            TreatAsErrorFromVersion = "5")]
        public static Configure DefineEndpointName(this Configure config, Func<string> definesEndpointName)
        {
            throw new NotImplementedException();
        }

        [ObsoleteEx(
            Message = "Use `configuration.EndpointName(myEndpointName)`, where `configuration` is an instance of type `BusConfiguration`.",
            RemoveInVersion = "6",
            TreatAsErrorFromVersion = "5")]
        public static Configure DefineEndpointName(this Configure config, string name)
        {
            throw new NotImplementedException();
        }

    }
}

namespace NServiceBus
{
    [ObsoleteEx(
        Message = "RavenDB has been moved to its own stand alone nuget 'NServiceBus.RavenDB'.",
        RemoveInVersion = "6",
        TreatAsErrorFromVersion = "5")]
    public static class ConfigureRavenPersistence
    {

        [ObsoleteEx(
            Message = "RavenDB has been moved to its own stand alone nuget 'NServiceBus.RavenDB'.",
            RemoveInVersion = "6",
            TreatAsErrorFromVersion = "5")]
        public static Configure CustomiseRavenPersistence(this Configure config, object callback)
        {
            throw new NotImplementedException();
        }

        [ObsoleteEx(
            Message = "RavenDB has been moved to its own stand alone nuget 'NServiceBus.RavenDB'. Install the nuget package. Use `configuration.UsePersistence<RavenDBPersistence>().SetMessageToDatabaseMappingConvention(convention)`, where `configuration` is an instance of type `BusConfiguration`.",
            RemoveInVersion = "6",
            TreatAsErrorFromVersion = "5")]
        public static Configure MessageToDatabaseMappingConvention(this Configure config, Func<IMessageContext, string> convention)
        {
            throw new NotImplementedException();
        }

        [ObsoleteEx(
            Message = "RavenDB has been moved to its own stand alone nuget 'NServiceBus.RavenDB'. Install the nuget package.` Use `configuration.UsePersistence<RavenDBPersistence>()`, where `configuration` is an instance of type `BusConfiguration`.",
            RemoveInVersion = "6",
            TreatAsErrorFromVersion = "5")]
        public static Configure RavenPersistence(this Configure config)
        {
            throw new NotImplementedException();
        }

        [ObsoleteEx(
            Message = "RavenDB has been moved to its own stand alone nuget 'NServiceBus.RavenDB'. Install the nuget package. Use `configuration.UsePersistence<RavenDBPersistence>().SetDefaultDocumentStore(...)`, where `configuration` is an instance of type `BusConfiguration`.",
            RemoveInVersion = "6",
            TreatAsErrorFromVersion = "5")]
        public static Configure RavenPersistence(this Configure config, string connectionStringName)
        {
            throw new NotImplementedException();
        }

        [ObsoleteEx(
            Message = "RavenDB has been moved to its own stand alone nuget 'NServiceBus.RavenDB'. Install the nuget package. Use `configuration.UsePersistence<RavenDBPersistence>().SetDefaultDocumentStore(...)`, where `configuration` is an instance of type `BusConfiguration`.",
            RemoveInVersion = "6",
            TreatAsErrorFromVersion = "5")]
        public static Configure RavenPersistence(this Configure config, string connectionStringName, string database)
        {
            throw new NotImplementedException();
        }

        [ObsoleteEx(
            Message = "RavenDB has been moved to its own stand alone nuget 'NServiceBus.RavenDB'. Install the nuget package. Use `configuration.UsePersistence<RavenDBPersistence>().SetDefaultDocumentStore(...)`, where `configuration` is an instance of type `BusConfiguration`.",
            RemoveInVersion = "6",
            TreatAsErrorFromVersion = "5")]
        public static Configure RavenPersistence(this Configure config, Func<string> getConnectionString)
        {
            throw new NotImplementedException();
        }

        [ObsoleteEx(
            Message = "RavenDB has been moved to its own stand alone nuget 'NServiceBus.RavenDB'. Install the nuget package. Use `configuration.UsePersistence<RavenDBPersistence>().SetDefaultDocumentStore(...)`, where `configuration` is an instance of type `BusConfiguration`.",
            RemoveInVersion = "6",
            TreatAsErrorFromVersion = "5")]
        public static Configure RavenPersistence(this Configure config, Func<string> getConnectionString, string database)
        {
            throw new NotImplementedException();
        }


        [ObsoleteEx(
            Message = "RavenDB has been moved to its own stand alone nuget 'NServiceBus.RavenDB'. Install the nuget package. Use `configuration.UsePersistence<RavenDBPersistence>().SetDefaultDocumentStore(documentStore)`, where `configuration` is an instance of type `BusConfiguration`.",
            RemoveInVersion = "6",
            TreatAsErrorFromVersion = "5")]
        public static Configure RavenPersistenceWithStore(this Configure config, object documentStore)
        {
            throw new NotImplementedException();
        }

        [ObsoleteEx(
            Message = "RavenDB has been moved to its own stand alone nuget 'NServiceBus.RavenDB'. Install the nuget package. Use `configuration.UsePersistence<RavenDBPersistence>()`, where `configuration` is an instance of type `BusConfiguration`.",
            RemoveInVersion = "6",
            TreatAsErrorFromVersion = "5")]
        public static void RegisterDefaults()
        {
        }

    }
}
namespace NServiceBus
{
    [ObsoleteEx(
            Message = "RavenDB has been moved to its own stand alone nuget 'NServiceBus.RavenDB'.",
            RemoveInVersion = "6",
            TreatAsErrorFromVersion = "5")]
    public static class ConfigureRavenSagaPersister
    {
        [ObsoleteEx(
            Message = "RavenDB has been moved to its own stand alone nuget 'NServiceBus.RavenDB'. Install the nuget package. Use `configuration.UsePersistence<RavenDBPersistence>().For(Storage.Sagas)`, where `configuration` is an instance of type `BusConfiguration`.",
            RemoveInVersion = "6",
            TreatAsErrorFromVersion = "5")]
        public static Configure RavenSagaPersister(this Configure config)
        {
            throw new NotImplementedException();
        }
    }
}

namespace NServiceBus
{
    [ObsoleteEx(
        Message = "RavenDB has been moved to its own stand alone nuget 'NServiceBus.RavenDB'.",
        RemoveInVersion = "6",
        TreatAsErrorFromVersion = "5")]
    public static class ConfigureRavenSubscriptionStorage
    {
        [ObsoleteEx(
            Message = "RavenDB has been moved to its own stand alone nuget 'NServiceBus.RavenDB'. Install the nuget package. Use `configuration.UsePersistence<RavenDBPersistence>().For(Storage.Subscriptions)`, where `configuration` is an instance of type `BusConfiguration`.",
            RemoveInVersion = "6",
            TreatAsErrorFromVersion = "5")]
        public static Configure RavenSubscriptionStorage(this Configure config)
        {
            throw new NotImplementedException();
        }
    }
}

namespace NServiceBus
{
    [ObsoleteEx(
        RemoveInVersion = "6",
        TreatAsErrorFromVersion = "5")]
    public static class ConfigureTimeoutManager
    {
        [ObsoleteEx(
            Message = "Use `configuration.DisableFeature<TimeoutManager>()`, where `configuration` is an instance of type `BusConfiguration`.",
            RemoveInVersion = "6",
            TreatAsErrorFromVersion = "5")]
        public static Configure DisableTimeoutManager(this Configure config)
        {
            throw new NotImplementedException();
        }


        [ObsoleteEx(
            Message = "Use `configuration.UsePersistence<InMemoryPersistence>()`, where `configuration` is an instance of type `BusConfiguration`.",
            RemoveInVersion = "6",
            TreatAsErrorFromVersion = "5")]
        public static Configure UseInMemoryTimeoutPersister(this Configure config)
        {
            throw new NotImplementedException();
        }

        [ObsoleteEx(
            Message = "RavenDB has been moved to its own stand alone nuget 'NServiceBus.RavenDB'. Install the nuget package. Use `configuration.UsePersistence<RavenDBPersistence>().For(Storage.Timeouts)`, where `configuration` is an instance of type `BusConfiguration`.",
            RemoveInVersion = "6",
            TreatAsErrorFromVersion = "5")]
        public static Configure UseRavenTimeoutPersister(this Configure config)
        {
            throw new NotImplementedException();
        }
    }
}

namespace NServiceBus
{
    [ObsoleteEx(
        RemoveInVersion = "6",
        TreatAsErrorFromVersion = "5")]
    public static class ConfigureUnicastBus
    {

        //TODO: add replacement
        [ObsoleteEx(
            RemoveInVersion = "6",
            TreatAsErrorFromVersion = "5")]
        public static Address GetTimeoutManagerAddress(this Configure config)
        {
            throw new NotImplementedException();
        }

        [ObsoleteEx(
            Message = "UnicastBus is now the default and hence calling this method is redundant. `Bus.Create(configuration)`, where `configuration` is an instance of type `BusConfiguration`.",
            RemoveInVersion = "6",
            TreatAsErrorFromVersion = "5")]
        public static Configure UnicastBus(this Configure config)
        {
            throw new NotImplementedException();
        }
    }
}

namespace NServiceBus
{
    [ObsoleteEx(
        Message = "Inject an instance of `IBus` in the constructor and assign that to a field for use",
        RemoveInVersion = "6",
        TreatAsErrorFromVersion = "5")]
    public static class MessageHandlerExtensionMethods
    {
        [ObsoleteEx(
            Message = "Inject an instance of `IBus` in the constructor and assign that to a field for use",
            RemoveInVersion = "6",
            TreatAsErrorFromVersion = "5")]
        public static IBus Bus<T>(this IHandleMessages<T> handler)
        {
            throw new NotImplementedException();
        }
    }
}

namespace NServiceBus.Transports.Msmq
{
    [ObsoleteEx(
        Message = "`MsmqUtilities` was never intended to be exposed as part of the public API. PLease copy the required functionality into your codebase.",
        RemoveInVersion = "6",
        TreatAsErrorFromVersion = "5")]
    public class MsmqUtilities
    {
    }
}

namespace NServiceBus.Unicast.Config
{
    [ObsoleteEx(
        Replacement = "Configure",
        RemoveInVersion = "6",
        TreatAsErrorFromVersion = "5")]
    public class ConfigUnicastBus 
    {
    }
}

namespace NServiceBus.Features
{
    [ObsoleteEx(
        Replacement = "NServiceBus.Features.StorageDrivenPublishing",
        RemoveInVersion = "6",
        TreatAsErrorFromVersion = "5")]
    public class StorageDrivenPublisher
    {
    }
}

namespace NServiceBus
{
    [ObsoleteEx(
        RemoveInVersion = "6",
        TreatAsErrorFromVersion = "5")]
    public static class TransportReceiverConfig
    {

        [ObsoleteEx(
            Message = "Use `configuration.UseTransport(transportDefinitionType).ConnectionString()`, where `configuration` is an instance of type `BusConfiguration`.",
            RemoveInVersion = "6",
            TreatAsErrorFromVersion = "5")]
        public static Configure UseTransport<T>(this Configure config, Func<string> definesConnectionString)
        {
            throw new NotImplementedException();
        }

        [ObsoleteEx(
            Message = "Use `configuration.UseTransport(transportDefinitionType).ConnectionString()`, where `configuration` is an instance of type `BusConfiguration`.",
            RemoveInVersion = "6",
            TreatAsErrorFromVersion = "5")]
        public static Configure UseTransport(this Configure config, Type transportDefinitionType, Func<string> definesConnectionString)
        {
            throw new NotImplementedException();
        }
    }
}
