namespace NServiceBus
{
    interface IPipeline
    {
        void Start();
        void Stop();
    }
}