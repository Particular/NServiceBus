#pragma warning disable 1591
namespace NServiceBus.Encryption.Rijndael
{
    [ObsoleteEx(
        TreatAsErrorFromVersion = "5.0",
        RemoveInVersion = "6.0",
        Message = "The Rijndael encryption functionality was an internal implementation detail of NServicebus as such it has been removed from the public API.")]
    public class EncryptionService 
    {
    }
}
