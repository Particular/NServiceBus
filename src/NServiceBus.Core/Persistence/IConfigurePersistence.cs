namespace NServiceBus.Persistence
{
    /// <summary>
    /// Enables the given persistence using the default settings
    /// </summary>
    public interface IConfigurePersistence
    {
        void Enable(Configure config);
    }

    /// <summary>
    /// The generic counterpart to IConfigurePersistence
    /// </summary>
    public interface IConfigurePersistence<T> : IConfigurePersistence where T : PersistenceDefinition { }
}