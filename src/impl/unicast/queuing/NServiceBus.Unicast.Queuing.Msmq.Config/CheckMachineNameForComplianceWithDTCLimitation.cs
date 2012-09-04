namespace NServiceBus.Unicast.Queuing.Msmq.Config
{
    using System.Configuration;
    using System.Runtime.InteropServices;
    using System.Text;
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
    /// See <a href="http://nservicebus.com/faq/DTCPIngWARNING.aspx">DTCPing warning</a>
    /// </summary>
    public class CheckMachineNameForComplianceWithDtcLimitation : IWantToRunWhenConfigurationIsComplete 
    {
        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        static extern bool GetComputerNameEx(COMPUTER_NAME_FORMAT nameType, [Out] StringBuilder lpBuffer, ref uint lpnSize);

        /// <summary>
        /// Method invoked to run custom code.
        /// </summary>
        public void Run()
        {
            if (!ConfigureMsmqMessageQueue.Selected)
                return;
            
            uint capacity = 24;
            var buffer = new StringBuilder((int)capacity);
            if (!GetComputerNameEx(COMPUTER_NAME_FORMAT.ComputerNameNetBIOS, buffer, ref capacity)) 
                return;
            var netbiosName = buffer.ToString();
            if (netbiosName.Length < 15) return;

            throw new ConfigurationErrorsException(string.Format(
                "NetBIOS name [{0}] is longer than 15 characters. Shorten it for DTC to work. See: http://nservicebus.com/faq/DTCPIngWARNING.aspx", netbiosName));
        }
    }
}
