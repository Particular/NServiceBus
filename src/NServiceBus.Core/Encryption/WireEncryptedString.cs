namespace NServiceBus
{
    using System;
    using System.Runtime.Serialization;

    /// <summary>
    /// A string whose value will be encrypted when sent over the wire.
    /// </summary>
    [Serializable]
    public class WireEncryptedString : ISerializable
    {
        /// <summary>
        /// Default constructor
        /// </summary>
        public WireEncryptedString()
        {}

        /// <summary>
        /// Deserializing constructor
        /// </summary>
        public WireEncryptedString(SerializationInfo info, StreamingContext context)
        {
            Guard.AgainstNull(info,"info");
            Guard.AgainstNull(context, "context");
            EncryptedValue = info.GetValue("EncryptedValue", typeof (EncryptedValue)) as EncryptedValue;
        }
        /// <summary>
        /// The unencrypted string.
        /// </summary>
        public string Value { get; set; }

        /// <summary>
        /// The encrypted value of this string
        /// </summary>
        public EncryptedValue EncryptedValue
        {
            get
            {
                return encryptedValue;
            }
            set
            {
                encryptedValue = value;
            }
        }
        EncryptedValue encryptedValue;
        
        //**** we need to duplicate to make versions > 3.2.7 backwards compatible with 2.X

        /// <summary>
        /// Only kept for backwards compatibility reasons
        /// </summary>
        [ObsoleteEx(TreatAsErrorFromVersion = "6.0", RemoveInVersion = "7.0", Message = "No longer required")]
        public string EncryptedBase64Value { get; set; }

        /// <summary>
        /// Only kept for backwards compatibility reasons
        /// </summary>
        [ObsoleteEx(TreatAsErrorFromVersion = "6.0", RemoveInVersion = "7.0", Message = "No longer required")]
        public string Base64Iv { get; set; }
        
        //****

        /// <summary>
        /// Gets the string value from the WireEncryptedString.
        /// </summary>
        public static implicit operator string(WireEncryptedString s)
        {
            return s == null ? null : s.Value;
        }

        /// <summary>
        /// Creates a new WireEncryptedString from the given string.
        /// </summary>
        public static implicit operator WireEncryptedString(string s)
        {
            return new WireEncryptedString { Value = s };
        }

        /// <summary>
        /// Method for making default XML serialization work properly for this type.
        /// </summary>
        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            Guard.AgainstNull(info, "info");
            Guard.AgainstNull(context, "context");
            info.AddValue("EncryptedValue", EncryptedValue);
        }
    }
}
