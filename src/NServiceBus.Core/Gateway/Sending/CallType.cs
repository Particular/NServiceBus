namespace NServiceBus.Gateway.Sending
{
    public enum CallType
    {
        [ObsoleteEx(RemoveInVersion = "6.0", TreatAsErrorFromVersion = "5.0")] Submit,
        [ObsoleteEx(RemoveInVersion = "6.0", TreatAsErrorFromVersion = "5.0")] Ack,
        [ObsoleteEx(RemoveInVersion = "6.0", TreatAsErrorFromVersion = "5.0")] DatabusProperty,

        SingleCallSubmit,
        SingleCallDatabusProperty
    }
}