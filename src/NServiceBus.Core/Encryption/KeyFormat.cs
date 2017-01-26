namespace NServiceBus.Config
{
    /// <summary>
    /// The format in which an encryption value is specified.
    /// </summary>
    [ObsoleteEx(
        Message = "Message property encryption is released as a dedicated 'NServiceBus.Encryption.MessageProperty' package.",
        RemoveInVersion = "8",
        TreatAsErrorFromVersion = "7")]
    public enum KeyFormat
    {
        /// <summary>
        /// Key is specified as in ASCII characters.
        /// </summary>
        Ascii = 0,

        /// <summary>
        /// Key is specified as a Base64 encoded string.
        /// </summary>
        Base64 = 1
    }
}