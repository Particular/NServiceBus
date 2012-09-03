namespace NServiceBus.Config
{
    using System.Configuration;

    /// <summary>
    /// Validates that NetBIOS name is shorter than 15 characters.
    /// See <a href="http://nservicebus.com/faq/DTCPIngWARNING.aspx">Unusual DTCPing result</a>
    /// </summary>
    public class CheckNetBiosNameLength : IWantToRunBeforeConfiguration
    {
        /// <summary>
        /// Test NetBios name length for normal work of DTC.
        /// </summary>
        public void Init()
        {
            var netBiosName = System.Environment.MachineName;
            if (netBiosName.Length < 15) return;
            throw new ConfigurationErrorsException(string.Format("NetBIOS name [{0}] is longer than 15 characters. Shorten it for DTC to work. See: unusual DTCPing result. See http://nservicebus.com/faq/DTCPIngWARNING.aspx", netBiosName));
        }
    }
}