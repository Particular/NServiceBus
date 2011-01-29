namespace NServiceBus.Installation
{
    public interface INeedToInstallSomething
    {
        void Install();
    }

    public interface INeedToInstallSomething<T> : INeedToInstallSomething where T : IEnvironment
    {
        
    }
}
