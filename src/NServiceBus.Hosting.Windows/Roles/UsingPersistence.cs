namespace NServiceBus
{
    using Hosting.Roles;
    using Persistence;

    /// <summary>
    /// Role used to specify the desired persistence to use
    /// </summary>
    /// <typeparam name="T">The <see cref="PersistenceDefinition"/> to use.</typeparam>
    public interface UsingPersistence<T> : IRole where T : PersistenceDefinition
    {
    }
}