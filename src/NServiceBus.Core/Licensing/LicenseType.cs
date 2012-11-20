namespace NServiceBus.Licensing
{
    /// <summary>
    /// License types.
    /// </summary>
    public static class LicenseType
    {
        public const string Standard = "Standard";
        public const string Express = "Express";
        // Express license
        public const string Basic0 = "Basic0";
        public const string Trial = "Trial";
        // FREE, No license file, 1 message per second, 1 worker thread, 2 worker nodes
        public const string Basic1 = "Basic1";
        // 2 message per second, 1 worker thread, 2 worker nodes
        public const string Basic2 = "Basic2";
        // 4 message per second, 1 worker thread, 2 worker nodes
        public const string Basic4 = "Basic4";
        // 8 message per second, 1 worker thread, 2 worker nodes
        public const string Basic8 = "Basic8";
        // 16 message per second, 1 worker thread, 2 worker nodes
        public const string Basic16 = "Basic16";
        // 32 message per second, 1 worker thread, 2 worker nodes
        public const string Basic32 = "Basic32";
    }
}
