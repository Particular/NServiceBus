namespace NServiceBus.Pipeline
{
    interface IPipeline
    {
        void Start();
        void Stop();
    }
}