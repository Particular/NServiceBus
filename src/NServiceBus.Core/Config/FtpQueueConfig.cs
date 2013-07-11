namespace NServiceBus.Config
{
    using System;
    using System.Configuration;

    public class FtpQueueConfig : ConfigurationSection
    {
        /// <summary>
        /// The temp directory where files will be place before sending
        /// </summary>
        [ObsoleteEx(Message = "Not required any longer. You can safely remove this attribute.", RemoveInVersion = "5.0", TreatAsErrorFromVersion = "4.0")]
        [ConfigurationProperty("SendDirectory", IsRequired = false)]
        public String SendDirectory 
        { 
            get{ return this["SendDirectory"].ToString(); }
            set{ this["SendDirectory"] = value; }
        }

        /// <summary>
        /// The temp directory where files will be watched for when they 
        /// come through FTP
        /// </summary>
        [ConfigurationProperty("ReceiveDirectory", IsRequired = true)]
        public String ReceiveDirectory 
        { 
            get{ return this["ReceiveDirectory"].ToString(); }
            set{ this["ReceiveDirectory"] = value; }
        }

        /// <summary>
        /// The user name to use when making FTP connections
        /// </summary>
        [ConfigurationProperty("UserName", IsRequired = true)]
        public String UserName 
        { 
            get{ return this["UserName"].ToString(); }
            set{ this["UserName"] = value; }
        }
        /// <summary>
        /// The password to use when making FTP connections
        /// </summary>
        [ConfigurationProperty("Password", IsRequired = true)]
        public String Password
        {
            get { return this["Password"].ToString(); }
            set { this["Password"] = value; }
        }
    }
}

