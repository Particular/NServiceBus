namespace NServiceBus
{
    using System;
    using System.Runtime.Serialization;

    /// <summary>
    /// A string whose value will be encrypted when sent over the wire.
    /// </summary>
    [Serializable]
    [ObsoleteEx(
        Message = "Message property encryption is released as a dedicated 'NServiceBus.Encryption.MessageProperty' package.",
        ReplacementTypeOrMember = "NServiceBus.Encryption.MessageProperty.WireEncryptedString",
        RemoveInVersion = "8",
        TreatAsErrorFromVersion = "7")]
    public class WireEncryptedString : ISerializable
    {
        /// <summary>
        /// Initializes a new instance of <see cref="WireEncryptedString" />.
        /// </summary>
        [ObsoleteEx(
            Message = "Message property encryption is released as a dedicated 'NServiceBus.Encryption.MessageProperty' package.",
            RemoveInVersion = "8",
            TreatAsErrorFromVersion = "7")]
        public WireEncryptedString()
        {
        }

        /// <summary>
        /// Initializes a new instance of <see cref="WireEncryptedString" />.
        /// </summary>
        [ObsoleteEx(
            Message = "Message property encryption is released as a dedicated 'NServiceBus.Encryption.MessageProperty' package.",
            RemoveInVersion = "8",
            TreatAsErrorFromVersion = "7")]
        public WireEncryptedString(SerializationInfo info, StreamingContext context)
        {
            Guard.AgainstNull(nameof(info), info);
            EncryptedValue = info.GetValue("EncryptedValue", typeof(EncryptedValue)) as EncryptedValue;
        }

        /// <summary>
        /// The unencrypted string.
        /// </summary>
        [ObsoleteEx(
            Message = "Message property encryption is released as a dedicated 'NServiceBus.Encryption.MessageProperty' package.",
            RemoveInVersion = "8",
            TreatAsErrorFromVersion = "7")]
        public string Value { get; set; }

        /// <summary>
        /// The encrypted value of this string.
        /// </summary>
        [ObsoleteEx(
            Message = "Message property encryption is released as a dedicated 'NServiceBus.Encryption.MessageProperty' package.",
            RemoveInVersion = "8",
            TreatAsErrorFromVersion = "7")]
        public EncryptedValue EncryptedValue
        {
            get { return encryptedValue; }
            set { encryptedValue = value; }
        }

        // we need to duplicate to make versions > 3.2.7 backwards compatible with 2.X

        /// <summary>
        /// Only kept for backwards compatibility reasons.
        /// </summary>
        [ObsoleteEx(
            TreatAsErrorFromVersion = "6.0",
            RemoveInVersion = "7.0",
            Message = "No longer required")]
        public string EncryptedBase64Value { get; set; }

        /// <summary>
        /// Only kept for backwards compatibility reasons.
        /// </summary>
        [ObsoleteEx(
            TreatAsErrorFromVersion = "6.0",
            RemoveInVersion = "7.0",
            Message = "No longer required")]
        public string Base64Iv { get; set; }

        /// <summary>
        /// Method for making default XML serialization work properly for this type.
        /// </summary>
        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            Guard.AgainstNull(nameof(info), info);
            info.AddValue("EncryptedValue", EncryptedValue);
        }

        /// <summary>
        /// Gets the string value from the WireEncryptedString.
        /// </summary>
        public static implicit operator string(WireEncryptedString s)
        {
            return s?.Value;
        }

        /// <summary>
        /// Creates a new WireEncryptedString from the given string.
        /// </summary>
        public static implicit operator WireEncryptedString(string s)
        {
            return new WireEncryptedString
            {
                Value = s
            };
        }

        EncryptedValue encryptedValue;
    }
}