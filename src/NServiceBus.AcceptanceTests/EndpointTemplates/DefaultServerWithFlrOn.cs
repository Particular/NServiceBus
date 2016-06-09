namespace NServiceBus.AcceptanceTests.EndpointTemplates
{
    class DefaultServerWithFlrOn : DefaultServer
    {
        protected override bool DisableFlr()
        {
            return false;
        }
    }
}