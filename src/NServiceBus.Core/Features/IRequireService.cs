namespace NServiceBus.Features
{
    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IRequireService<T>:IRequireService where T : IFeatureService
    {
    }

    /// <summary>
    /// 
    /// </summary>
    public interface IRequireService
    {
        
    }
}