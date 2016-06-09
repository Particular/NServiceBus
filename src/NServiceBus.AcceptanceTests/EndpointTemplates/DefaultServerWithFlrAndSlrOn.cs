namespace NServiceBus.AcceptanceTests.EndpointTemplates
{
    class DefaultServerWithFlrAndSlrOn : DefaultServer
    {
        protected override bool DisableFLR()
        {
            return false;
        }

        protected override bool DisableSLR()
        {
            return false;
        }
    }
}