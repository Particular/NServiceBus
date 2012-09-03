namespace NServiceBus.Config
{
    using System.Configuration;

    /// <summary>
    /// Validates that NetBIOS name is shorter than 15 characters.
    /// See <a href="http://social.msdn.microsoft.com/Forums/en/windowstransactionsprogramming/thread/1ddb9665-1a28-4d3e-bddd-50de2f07543a"></a>
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
            throw new ConfigurationErrorsException(string.Format("NetBIOS name [{0}] is longer than 15 characters. Shorten it for DTC to work.", netBiosName));
        }
    }
}