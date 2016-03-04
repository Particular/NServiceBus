namespace NServiceBus.Features
{
    using System.Runtime.InteropServices;
    using System.Text;
    using Logging;

    class CheckMachineNameForComplianceWithDtcLimitation
    {
        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        static extern bool GetComputerNameEx(COMPUTER_NAME_FORMAT nameType, [Out] StringBuilder lpBuffer, ref uint lpnSize);

        /// <summary>
        /// Method invoked to run custom code.
        /// </summary>
        public void Check()
        {
            uint capacity = 24;
            var buffer = new StringBuilder((int) capacity);
            if (!GetComputerNameEx(COMPUTER_NAME_FORMAT.ComputerNameNetBIOS, buffer, ref capacity))
            {
                return;
            }
            var netbiosName = buffer.ToString();
            if (netbiosName.Length <= 15)
            {
                return;
            }

            Logger.WarnFormat("NetBIOS name [{0}] is longer than 15 characters. Shorten it for DTC to work.", netbiosName);
        }

        static ILog Logger = LogManager.GetLogger<CheckMachineNameForComplianceWithDtcLimitation>();

        enum COMPUTER_NAME_FORMAT
        {
            ComputerNameNetBIOS,
            ComputerNameDnsHostname,
            ComputerNameDnsDomain,
            ComputerNameDnsFullyQualified,
            ComputerNamePhysicalNetBIOS,
            ComputerNamePhysicalDnsHostname,
            ComputerNamePhysicalDnsDomain,
            ComputerNamePhysicalDnsFullyQualified
        }
    }
}