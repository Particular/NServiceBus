namespace NServiceBus.Hosting.Profiles
{
    /// <summary>
    /// Abstraction for code that will be called that will take dependent action based upon
    /// the Profile(s) that are active. Useful for implementing special functionality if
    /// a specific profile is activated, and implementing default functionality otherwise.
    /// </summary>
    public interface IHandleAnyProfile : IHandleProfile<IProfile>, IWantTheListOfActiveProfiles
    {
    }
}