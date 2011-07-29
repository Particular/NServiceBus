using System.Linq;
using System.Management;

namespace NServiceBus.Licensing
{
    public class SystemInfo
    {
        public static int GetNumerOfCores()
        {
            int cores = new ManagementObjectSearcher("SELECT * FROM Win32_Processor")
                                    .Get()
                                    .Cast<ManagementBaseObject>()
                                    .Sum(item => (int)item["NumberOfCores"]);

            return cores;
        }
    }
}