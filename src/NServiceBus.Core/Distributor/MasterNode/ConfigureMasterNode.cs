// ReSharper disable UnusedParameter.Global
namespace NServiceBus
{
    using System;

    [ObsoleteEx(TreatAsErrorFromVersion = "5.0", RemoveInVersion = "6.0", Message = "The NServiceBus Distributor was moved into its own assembly (NServiceBus.Distributor.MSMQ.dll), please make sure you reference the new assembly.")]
#pragma warning disable 1591
    public static class ConfigureMasterNode
    {
        public static Configure AsMasterNode(this Configure config)
        {
            throw new Exception("Obsolete");
        }

        public static bool IsConfiguredAsMasterNode(this Configure config)
        {
            throw new Exception("Obsolete");
        }

        public static bool HasMasterNode(this Configure config)
        {
            throw new Exception("Obsolete");
        }

        public static string GetMasterNode(this Configure config)
        {
            throw new Exception("Obsolete");
        }

        public static Address GetMasterNodeAddress(this Configure config)
        {
            throw new Exception("Obsolete");
        }
    }
#pragma warning restore 1591
}
