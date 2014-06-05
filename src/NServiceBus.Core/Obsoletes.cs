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