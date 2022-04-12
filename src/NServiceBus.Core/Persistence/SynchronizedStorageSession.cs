namespace NServiceBus.Persistence
{
    /// <summary>
    /// Represents a storage session.
    /// </summary>
#pragma warning disable IDE1006 // Naming Styles
    public interface SynchronizedStorageSession
#pragma warning restore IDE1006 // Naming Styles
    {
    }

    ///// <summary>
    ///// Represents a storage session.
    ///// </summary>
    //public interface ISynchronizedStorageSession
    //{
    //}

    //TODO
    //Version constraints:
    //- old persistence packages need to work with NServiceBus 7.8.
    //- Persisters that target 7.8 don't work with core 7.7 and earlier.
    //
    //Plan:
    //New interface ICompletableSSS
    //7.8-compatible persisters register their SSS impl as scoped in the container. They DON'T implement the SynchronizedStorage and Adapter interfaces
    //
    //Core tries to resolve either SynchronizedStorage/Adapter or 
}