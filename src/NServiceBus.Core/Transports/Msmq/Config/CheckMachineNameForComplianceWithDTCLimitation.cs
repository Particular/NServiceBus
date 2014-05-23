namespace NServiceBus.Transports.Msmq.Config
{
    using System.Runtime.InteropServices;
    using System.Text;
    using Features;
    using Logging;
    using NServiceBus.Config;

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
    

    /// <summary>
    /// Make sure NETBios name is shorter than 15 characters. 
    /// See <a href="http://particular.net/articles/dtcping-warning-the-cid-values-for-both-test-machines-are-the-same">DTCPing warning</a>
    /// </summary>
    public class CheckMachineNameForComplianceWithDtcLimitation : IWantToRunWhenConfigurationIsComplete 
    {
        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        static extern bool GetComputerNameEx(COMPUTER_NAME_FORMAT nameType, [Out] StringBuilder lpBuffer, ref uint lpnSize);

        static ILog Logger = LogManager.GetLogger<CheckMachineNameForComplianceWithDtcLimitation>();

        /// <summary>
        /// Method invoked to run custom code.
        /// </summary>
        public void Run(Configure config)
        {
            if (!Feature.IsEnabled<MsmqTransport>())
            {
                return;
            }
                
            uint capacity = 24;
            var buffer = new StringBuilder((int)capacity);
            if (!GetComputerNameEx(COMPUTER_NAME_FORMAT.ComputerNameNetBIOS, buffer, ref capacity)) 
                return;
            var netbiosName = buffer.ToString();
            if (netbiosName.Length <= 15) return;

            Logger.Warn(string.Format(
                "NetBIOS name [{0}] is longer than 15 characters. Shorten it for DTC to work. See: http://particular.net/articles/dtcping-warning-the-cid-values-for-both-test-machines-are-the-same", netbiosName));
        }
    }
}
