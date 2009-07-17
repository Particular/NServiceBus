namespace NServiceBus
{
    /// <summary>
    /// A string whose value will be encrypted when sent over the wire.
    /// </summary>
    public class WireEncryptedString
    {
        /// <summary>
        /// The unencrypted string.
        /// </summary>
        public string Value { get; set; }

        /// <summary>
        /// Gets the string value from the WireEncryptedString.
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public static implicit operator string(WireEncryptedString s)
        {
            return s == null ? null : s.Value;
        }

        /// <summary>
        /// Creates a new WireEncryptedString from the given string.
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public static implicit operator WireEncryptedString(string s)
        {
            return new WireEncryptedString { Value = s };
        }
    }
}
