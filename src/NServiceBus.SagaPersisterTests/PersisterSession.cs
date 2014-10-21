
namespace NServiceBus.SagaPersisterTests
{
// ReSharper disable once PartialTypeWithSinglePart
    partial class PersisterSession
    {
        public void Begin()
        {
            OnBegin();
        }

        public void End()
        {
            OnEnd();
        }

// ReSharper disable once PartialMethodWithSinglePart
        partial void OnBegin();

// ReSharper disable once PartialMethodWithSinglePart
        partial void OnEnd();
    }
}
