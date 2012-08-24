<?xml version="1.0" encoding="utf-8"?>
<serviceModel xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance" xmlns:xsd="http://www.w3.org/2001/XMLSchema" name="AzureService" generation="1" functional="0" release="0" Id="c08e2c58-6215-4e35-9e23-846dbc3e6a09" dslVersion="1.2.0.0" xmlns="http://schemas.microsoft.com/dsltools/RDSM">
  <groups>
    <group name="AzureServiceGroup" generation="1" functional="0" release="0">
      <componentports>
        <inPort name="OrderService:Microsoft.WindowsAzure.Plugins.RemoteForwarder.RdpInput" protocol="tcp">
          <inToChannel>
            <lBChannelMoniker name="/AzureService/AzureServiceGroup/LB:OrderService:Microsoft.WindowsAzure.Plugins.RemoteForwarder.RdpInput" />
          </inToChannel>
        </inPort>
        <inPort name="OrderWebSite:HttpIn" protocol="http">
          <inToChannel>
            <lBChannelMoniker name="/AzureService/AzureServiceGroup/LB:OrderWebSite:HttpIn" />
          </inToChannel>
        </inPort>
      </componentports>
      <settings>
        <aCS name="Certificate|OrderService:Microsoft.WindowsAzure.Plugins.RemoteAccess.PasswordEncryption" defaultValue="">
          <maps>
            <mapMoniker name="/AzureService/AzureServiceGroup/MapCertificate|OrderService:Microsoft.WindowsAzure.Plugins.RemoteAccess.PasswordEncryption" />
          </maps>
        </aCS>
        <aCS name="Certificate|OrderWebSite:Microsoft.WindowsAzure.Plugins.RemoteAccess.PasswordEncryption" defaultValue="">
          <maps>
            <mapMoniker name="/AzureService/AzureServiceGroup/MapCertificate|OrderWebSite:Microsoft.WindowsAzure.Plugins.RemoteAccess.PasswordEncryption" />
          </maps>
        </aCS>
        <aCS name="OrderService:AzureProfileConfig.Profiles" defaultValue="">
          <maps>
            <mapMoniker name="/AzureService/AzureServiceGroup/MapOrderService:AzureProfileConfig.Profiles" />
          </maps>
        </aCS>
        <aCS name="OrderService:AzureQueueConfig.ConnectionString" defaultValue="">
          <maps>
            <mapMoniker name="/AzureService/AzureServiceGroup/MapOrderService:AzureQueueConfig.ConnectionString" />
          </maps>
        </aCS>
        <aCS name="OrderService:AzureQueueConfig.QueueName" defaultValue="">
          <maps>
            <mapMoniker name="/AzureService/AzureServiceGroup/MapOrderService:AzureQueueConfig.QueueName" />
          </maps>
        </aCS>
        <aCS name="OrderService:AzureSubscriptionStorageConfig.ConnectionString" defaultValue="">
          <maps>
            <mapMoniker name="/AzureService/AzureServiceGroup/MapOrderService:AzureSubscriptionStorageConfig.ConnectionString" />
          </maps>
        </aCS>
        <aCS name="OrderService:Diagnostics.ConnectionString" defaultValue="">
          <maps>
            <mapMoniker name="/AzureService/AzureServiceGroup/MapOrderService:Diagnostics.ConnectionString" />
          </maps>
        </aCS>
        <aCS name="OrderService:Diagnostics.Level" defaultValue="">
          <maps>
            <mapMoniker name="/AzureService/AzureServiceGroup/MapOrderService:Diagnostics.Level" />
          </maps>
        </aCS>
        <aCS name="OrderService:MessageForwardingInCaseOfFaultConfig.ErrorQueue" defaultValue="">
          <maps>
            <mapMoniker name="/AzureService/AzureServiceGroup/MapOrderService:MessageForwardingInCaseOfFaultConfig.ErrorQueue" />
          </maps>
        </aCS>
        <aCS name="OrderService:Microsoft.WindowsAzure.Plugins.RemoteAccess.AccountEncryptedPassword" defaultValue="">
          <maps>
            <mapMoniker name="/AzureService/AzureServiceGroup/MapOrderService:Microsoft.WindowsAzure.Plugins.RemoteAccess.AccountEncryptedPassword" />
          </maps>
        </aCS>
        <aCS name="OrderService:Microsoft.WindowsAzure.Plugins.RemoteAccess.AccountExpiration" defaultValue="">
          <maps>
            <mapMoniker name="/AzureService/AzureServiceGroup/MapOrderService:Microsoft.WindowsAzure.Plugins.RemoteAccess.AccountExpiration" />
          </maps>
        </aCS>
        <aCS name="OrderService:Microsoft.WindowsAzure.Plugins.RemoteAccess.AccountUsername" defaultValue="">
          <maps>
            <mapMoniker name="/AzureService/AzureServiceGroup/MapOrderService:Microsoft.WindowsAzure.Plugins.RemoteAccess.AccountUsername" />
          </maps>
        </aCS>
        <aCS name="OrderService:Microsoft.WindowsAzure.Plugins.RemoteAccess.Enabled" defaultValue="">
          <maps>
            <mapMoniker name="/AzureService/AzureServiceGroup/MapOrderService:Microsoft.WindowsAzure.Plugins.RemoteAccess.Enabled" />
          </maps>
        </aCS>
        <aCS name="OrderService:Microsoft.WindowsAzure.Plugins.RemoteForwarder.Enabled" defaultValue="">
          <maps>
            <mapMoniker name="/AzureService/AzureServiceGroup/MapOrderService:Microsoft.WindowsAzure.Plugins.RemoteForwarder.Enabled" />
          </maps>
        </aCS>
        <aCS name="OrderService:MsmqTransportConfig.MaxRetries" defaultValue="">
          <maps>
            <mapMoniker name="/AzureService/AzureServiceGroup/MapOrderService:MsmqTransportConfig.MaxRetries" />
          </maps>
        </aCS>
        <aCS name="OrderService:MsmqTransportConfig.NumberOfWorkerThreads" defaultValue="">
          <maps>
            <mapMoniker name="/AzureService/AzureServiceGroup/MapOrderService:MsmqTransportConfig.NumberOfWorkerThreads" />
          </maps>
        </aCS>
        <aCS name="OrderService:UnicastBusConfig.LocalAddress" defaultValue="">
          <maps>
            <mapMoniker name="/AzureService/AzureServiceGroup/MapOrderService:UnicastBusConfig.LocalAddress" />
          </maps>
        </aCS>
        <aCS name="OrderServiceInstances" defaultValue="[1,1,1]">
          <maps>
            <mapMoniker name="/AzureService/AzureServiceGroup/MapOrderServiceInstances" />
          </maps>
        </aCS>
        <aCS name="OrderWebSite:AzureQueueConfig.ConnectionString" defaultValue="">
          <maps>
            <mapMoniker name="/AzureService/AzureServiceGroup/MapOrderWebSite:AzureQueueConfig.ConnectionString" />
          </maps>
        </aCS>
        <aCS name="OrderWebSite:AzureQueueConfig.QueueName" defaultValue="">
          <maps>
            <mapMoniker name="/AzureService/AzureServiceGroup/MapOrderWebSite:AzureQueueConfig.QueueName" />
          </maps>
        </aCS>
        <aCS name="OrderWebSite:AzureSubscriptionStorageConfig.ConnectionString" defaultValue="">
          <maps>
            <mapMoniker name="/AzureService/AzureServiceGroup/MapOrderWebSite:AzureSubscriptionStorageConfig.ConnectionString" />
          </maps>
        </aCS>
        <aCS name="OrderWebSite:Diagnostics.ConnectionString" defaultValue="">
          <maps>
            <mapMoniker name="/AzureService/AzureServiceGroup/MapOrderWebSite:Diagnostics.ConnectionString" />
          </maps>
        </aCS>
        <aCS name="OrderWebSite:Diagnostics.Level" defaultValue="">
          <maps>
            <mapMoniker name="/AzureService/AzureServiceGroup/MapOrderWebSite:Diagnostics.Level" />
          </maps>
        </aCS>
        <aCS name="OrderWebSite:MessageForwardingInCaseOfFaultConfig.ErrorQueue" defaultValue="">
          <maps>
            <mapMoniker name="/AzureService/AzureServiceGroup/MapOrderWebSite:MessageForwardingInCaseOfFaultConfig.ErrorQueue" />
          </maps>
        </aCS>
        <aCS name="OrderWebSite:Microsoft.WindowsAzure.Plugins.RemoteAccess.AccountEncryptedPassword" defaultValue="">
          <maps>
            <mapMoniker name="/AzureService/AzureServiceGroup/MapOrderWebSite:Microsoft.WindowsAzure.Plugins.RemoteAccess.AccountEncryptedPassword" />
          </maps>
        </aCS>
        <aCS name="OrderWebSite:Microsoft.WindowsAzure.Plugins.RemoteAccess.AccountExpiration" defaultValue="">
          <maps>
            <mapMoniker name="/AzureService/AzureServiceGroup/MapOrderWebSite:Microsoft.WindowsAzure.Plugins.RemoteAccess.AccountExpiration" />
          </maps>
        </aCS>
        <aCS name="OrderWebSite:Microsoft.WindowsAzure.Plugins.RemoteAccess.AccountUsername" defaultValue="">
          <maps>
            <mapMoniker name="/AzureService/AzureServiceGroup/MapOrderWebSite:Microsoft.WindowsAzure.Plugins.RemoteAccess.AccountUsername" />
          </maps>
        </aCS>
        <aCS name="OrderWebSite:Microsoft.WindowsAzure.Plugins.RemoteAccess.Enabled" defaultValue="">
          <maps>
            <mapMoniker name="/AzureService/AzureServiceGroup/MapOrderWebSite:Microsoft.WindowsAzure.Plugins.RemoteAccess.Enabled" />
          </maps>
        </aCS>
        <aCS name="OrderWebSite:MsmqTransportConfig.MaxRetries" defaultValue="">
          <maps>
            <mapMoniker name="/AzureService/AzureServiceGroup/MapOrderWebSite:MsmqTransportConfig.MaxRetries" />
          </maps>
        </aCS>
        <aCS name="OrderWebSite:MsmqTransportConfig.NumberOfWorkerThreads" defaultValue="">
          <maps>
            <mapMoniker name="/AzureService/AzureServiceGroup/MapOrderWebSite:MsmqTransportConfig.NumberOfWorkerThreads" />
          </maps>
        </aCS>
        <aCS name="OrderWebSiteInstances" defaultValue="[1,1,1]">
          <maps>
            <mapMoniker name="/AzureService/AzureServiceGroup/MapOrderWebSiteInstances" />
          </maps>
        </aCS>
      </settings>
      <channels>
        <lBChannel name="LB:OrderService:Microsoft.WindowsAzure.Plugins.RemoteForwarder.RdpInput">
          <toPorts>
            <inPortMoniker name="/AzureService/AzureServiceGroup/OrderService/Microsoft.WindowsAzure.Plugins.RemoteForwarder.RdpInput" />
          </toPorts>
        </lBChannel>
        <lBChannel name="LB:OrderWebSite:HttpIn">
          <toPorts>
            <inPortMoniker name="/AzureService/AzureServiceGroup/OrderWebSite/HttpIn" />
          </toPorts>
        </lBChannel>
        <sFSwitchChannel name="SW:OrderService:Microsoft.WindowsAzure.Plugins.RemoteAccess.Rdp">
          <toPorts>
            <inPortMoniker name="/AzureService/AzureServiceGroup/OrderService/Microsoft.WindowsAzure.Plugins.RemoteAccess.Rdp" />
          </toPorts>
        </sFSwitchChannel>
        <sFSwitchChannel name="SW:OrderWebSite:Microsoft.WindowsAzure.Plugins.RemoteAccess.Rdp">
          <toPorts>
            <inPortMoniker name="/AzureService/AzureServiceGroup/OrderWebSite/Microsoft.WindowsAzure.Plugins.RemoteAccess.Rdp" />
          </toPorts>
        </sFSwitchChannel>
      </channels>
      <maps>
        <map name="MapCertificate|OrderService:Microsoft.WindowsAzure.Plugins.RemoteAccess.PasswordEncryption" kind="Identity">
          <certificate>
            <certificateMoniker name="/AzureService/AzureServiceGroup/OrderService/Microsoft.WindowsAzure.Plugins.RemoteAccess.PasswordEncryption" />
          </certificate>
        </map>
        <map name="MapCertificate|OrderWebSite:Microsoft.WindowsAzure.Plugins.RemoteAccess.PasswordEncryption" kind="Identity">
          <certificate>
            <certificateMoniker name="/AzureService/AzureServiceGroup/OrderWebSite/Microsoft.WindowsAzure.Plugins.RemoteAccess.PasswordEncryption" />
          </certificate>
        </map>
        <map name="MapOrderService:AzureProfileConfig.Profiles" kind="Identity">
          <setting>
            <aCSMoniker name="/AzureService/AzureServiceGroup/OrderService/AzureProfileConfig.Profiles" />
          </setting>
        </map>
        <map name="MapOrderService:AzureQueueConfig.ConnectionString" kind="Identity">
          <setting>
            <aCSMoniker name="/AzureService/AzureServiceGroup/OrderService/AzureQueueConfig.ConnectionString" />
          </setting>
        </map>
        <map name="MapOrderService:AzureQueueConfig.QueueName" kind="Identity">
          <setting>
            <aCSMoniker name="/AzureService/AzureServiceGroup/OrderService/AzureQueueConfig.QueueName" />
          </setting>
        </map>
        <map name="MapOrderService:AzureSubscriptionStorageConfig.ConnectionString" kind="Identity">
          <setting>
            <aCSMoniker name="/AzureService/AzureServiceGroup/OrderService/AzureSubscriptionStorageConfig.ConnectionString" />
          </setting>
        </map>
        <map name="MapOrderService:Diagnostics.ConnectionString" kind="Identity">
          <setting>
            <aCSMoniker name="/AzureService/AzureServiceGroup/OrderService/Diagnostics.ConnectionString" />
          </setting>
        </map>
        <map name="MapOrderService:Diagnostics.Level" kind="Identity">
          <setting>
            <aCSMoniker name="/AzureService/AzureServiceGroup/OrderService/Diagnostics.Level" />
          </setting>
        </map>
        <map name="MapOrderService:MessageForwardingInCaseOfFaultConfig.ErrorQueue" kind="Identity">
          <setting>
            <aCSMoniker name="/AzureService/AzureServiceGroup/OrderService/MessageForwardingInCaseOfFaultConfig.ErrorQueue" />
          </setting>
        </map>
        <map name="MapOrderService:Microsoft.WindowsAzure.Plugins.RemoteAccess.AccountEncryptedPassword" kind="Identity">
          <setting>
            <aCSMoniker name="/AzureService/AzureServiceGroup/OrderService/Microsoft.WindowsAzure.Plugins.RemoteAccess.AccountEncryptedPassword" />
          </setting>
        </map>
        <map name="MapOrderService:Microsoft.WindowsAzure.Plugins.RemoteAccess.AccountExpiration" kind="Identity">
          <setting>
            <aCSMoniker name="/AzureService/AzureServiceGroup/OrderService/Microsoft.WindowsAzure.Plugins.RemoteAccess.AccountExpiration" />
          </setting>
        </map>
        <map name="MapOrderService:Microsoft.WindowsAzure.Plugins.RemoteAccess.AccountUsername" kind="Identity">
          <setting>
            <aCSMoniker name="/AzureService/AzureServiceGroup/OrderService/Microsoft.WindowsAzure.Plugins.RemoteAccess.AccountUsername" />
          </setting>
        </map>
        <map name="MapOrderService:Microsoft.WindowsAzure.Plugins.RemoteAccess.Enabled" kind="Identity">
          <setting>
            <aCSMoniker name="/AzureService/AzureServiceGroup/OrderService/Microsoft.WindowsAzure.Plugins.RemoteAccess.Enabled" />
          </setting>
        </map>
        <map name="MapOrderService:Microsoft.WindowsAzure.Plugins.RemoteForwarder.Enabled" kind="Identity">
          <setting>
            <aCSMoniker name="/AzureService/AzureServiceGroup/OrderService/Microsoft.WindowsAzure.Plugins.RemoteForwarder.Enabled" />
          </setting>
        </map>
        <map name="MapOrderService:MsmqTransportConfig.MaxRetries" kind="Identity">
          <setting>
            <aCSMoniker name="/AzureService/AzureServiceGroup/OrderService/MsmqTransportConfig.MaxRetries" />
          </setting>
        </map>
        <map name="MapOrderService:MsmqTransportConfig.NumberOfWorkerThreads" kind="Identity">
          <setting>
            <aCSMoniker name="/AzureService/AzureServiceGroup/OrderService/MsmqTransportConfig.NumberOfWorkerThreads" />
          </setting>
        </map>
        <map name="MapOrderService:UnicastBusConfig.LocalAddress" kind="Identity">
          <setting>
            <aCSMoniker name="/AzureService/AzureServiceGroup/OrderService/UnicastBusConfig.LocalAddress" />
          </setting>
        </map>
        <map name="MapOrderServiceInstances" kind="Identity">
          <setting>
            <sCSPolicyIDMoniker name="/AzureService/AzureServiceGroup/OrderServiceInstances" />
          </setting>
        </map>
        <map name="MapOrderWebSite:AzureQueueConfig.ConnectionString" kind="Identity">
          <setting>
            <aCSMoniker name="/AzureService/AzureServiceGroup/OrderWebSite/AzureQueueConfig.ConnectionString" />
          </setting>
        </map>
        <map name="MapOrderWebSite:AzureQueueConfig.QueueName" kind="Identity">
          <setting>
            <aCSMoniker name="/AzureService/AzureServiceGroup/OrderWebSite/AzureQueueConfig.QueueName" />
          </setting>
        </map>
        <map name="MapOrderWebSite:AzureSubscriptionStorageConfig.ConnectionString" kind="Identity">
          <setting>
            <aCSMoniker name="/AzureService/AzureServiceGroup/OrderWebSite/AzureSubscriptionStorageConfig.ConnectionString" />
          </setting>
        </map>
        <map name="MapOrderWebSite:Diagnostics.ConnectionString" kind="Identity">
          <setting>
            <aCSMoniker name="/AzureService/AzureServiceGroup/OrderWebSite/Diagnostics.ConnectionString" />
          </setting>
        </map>
        <map name="MapOrderWebSite:Diagnostics.Level" kind="Identity">
          <setting>
            <aCSMoniker name="/AzureService/AzureServiceGroup/OrderWebSite/Diagnostics.Level" />
          </setting>
        </map>
        <map name="MapOrderWebSite:MessageForwardingInCaseOfFaultConfig.ErrorQueue" kind="Identity">
          <setting>
            <aCSMoniker name="/AzureService/AzureServiceGroup/OrderWebSite/MessageForwardingInCaseOfFaultConfig.ErrorQueue" />
          </setting>
        </map>
        <map name="MapOrderWebSite:Microsoft.WindowsAzure.Plugins.RemoteAccess.AccountEncryptedPassword" kind="Identity">
          <setting>
            <aCSMoniker name="/AzureService/AzureServiceGroup/OrderWebSite/Microsoft.WindowsAzure.Plugins.RemoteAccess.AccountEncryptedPassword" />
          </setting>
        </map>
        <map name="MapOrderWebSite:Microsoft.WindowsAzure.Plugins.RemoteAccess.AccountExpiration" kind="Identity">
          <setting>
            <aCSMoniker name="/AzureService/AzureServiceGroup/OrderWebSite/Microsoft.WindowsAzure.Plugins.RemoteAccess.AccountExpiration" />
          </setting>
        </map>
        <map name="MapOrderWebSite:Microsoft.WindowsAzure.Plugins.RemoteAccess.AccountUsername" kind="Identity">
          <setting>
            <aCSMoniker name="/AzureService/AzureServiceGroup/OrderWebSite/Microsoft.WindowsAzure.Plugins.RemoteAccess.AccountUsername" />
          </setting>
        </map>
        <map name="MapOrderWebSite:Microsoft.WindowsAzure.Plugins.RemoteAccess.Enabled" kind="Identity">
          <setting>
            <aCSMoniker name="/AzureService/AzureServiceGroup/OrderWebSite/Microsoft.WindowsAzure.Plugins.RemoteAccess.Enabled" />
          </setting>
        </map>
        <map name="MapOrderWebSite:MsmqTransportConfig.MaxRetries" kind="Identity">
          <setting>
            <aCSMoniker name="/AzureService/AzureServiceGroup/OrderWebSite/MsmqTransportConfig.MaxRetries" />
          </setting>
        </map>
        <map name="MapOrderWebSite:MsmqTransportConfig.NumberOfWorkerThreads" kind="Identity">
          <setting>
            <aCSMoniker name="/AzureService/AzureServiceGroup/OrderWebSite/MsmqTransportConfig.NumberOfWorkerThreads" />
          </setting>
        </map>
        <map name="MapOrderWebSiteInstances" kind="Identity">
          <setting>
            <sCSPolicyIDMoniker name="/AzureService/AzureServiceGroup/OrderWebSiteInstances" />
          </setting>
        </map>
      </maps>
      <components>
        <groupHascomponents>
          <role name="OrderService" generation="1" functional="0" release="0" software="D:\Github.com\NServiceBus-3.2.8\Samples\Azure\AzurePubSub\csx\Debug\roles\OrderService" entryPoint="base\x64\WaHostBootstrapper.exe" parameters="base\x64\WaWorkerHost.exe " memIndex="1792" hostingEnvironment="consoleroleadmin" hostingEnvironmentVersion="2">
            <componentports>
              <inPort name="Microsoft.WindowsAzure.Plugins.RemoteForwarder.RdpInput" protocol="tcp" />
              <inPort name="Microsoft.WindowsAzure.Plugins.RemoteAccess.Rdp" protocol="tcp" portRanges="3389" />
              <outPort name="OrderService:Microsoft.WindowsAzure.Plugins.RemoteAccess.Rdp" protocol="tcp">
                <outToChannel>
                  <sFSwitchChannelMoniker name="/AzureService/AzureServiceGroup/SW:OrderService:Microsoft.WindowsAzure.Plugins.RemoteAccess.Rdp" />
                </outToChannel>
              </outPort>
              <outPort name="OrderWebSite:Microsoft.WindowsAzure.Plugins.RemoteAccess.Rdp" protocol="tcp">
                <outToChannel>
                  <sFSwitchChannelMoniker name="/AzureService/AzureServiceGroup/SW:OrderWebSite:Microsoft.WindowsAzure.Plugins.RemoteAccess.Rdp" />
                </outToChannel>
              </outPort>
            </componentports>
            <settings>
              <aCS name="AzureProfileConfig.Profiles" defaultValue="" />
              <aCS name="AzureQueueConfig.ConnectionString" defaultValue="" />
              <aCS name="AzureQueueConfig.QueueName" defaultValue="" />
              <aCS name="AzureSubscriptionStorageConfig.ConnectionString" defaultValue="" />
              <aCS name="Diagnostics.ConnectionString" defaultValue="" />
              <aCS name="Diagnostics.Level" defaultValue="" />
              <aCS name="MessageForwardingInCaseOfFaultConfig.ErrorQueue" defaultValue="" />
              <aCS name="Microsoft.WindowsAzure.Plugins.RemoteAccess.AccountEncryptedPassword" defaultValue="" />
              <aCS name="Microsoft.WindowsAzure.Plugins.RemoteAccess.AccountExpiration" defaultValue="" />
              <aCS name="Microsoft.WindowsAzure.Plugins.RemoteAccess.AccountUsername" defaultValue="" />
              <aCS name="Microsoft.WindowsAzure.Plugins.RemoteAccess.Enabled" defaultValue="" />
              <aCS name="Microsoft.WindowsAzure.Plugins.RemoteForwarder.Enabled" defaultValue="" />
              <aCS name="MsmqTransportConfig.MaxRetries" defaultValue="" />
              <aCS name="MsmqTransportConfig.NumberOfWorkerThreads" defaultValue="" />
              <aCS name="UnicastBusConfig.LocalAddress" defaultValue="" />
              <aCS name="__ModelData" defaultValue="&lt;m role=&quot;OrderService&quot; xmlns=&quot;urn:azure:m:v1&quot;&gt;&lt;r name=&quot;OrderService&quot;&gt;&lt;e name=&quot;Microsoft.WindowsAzure.Plugins.RemoteAccess.Rdp&quot; /&gt;&lt;e name=&quot;Microsoft.WindowsAzure.Plugins.RemoteForwarder.RdpInput&quot; /&gt;&lt;/r&gt;&lt;r name=&quot;OrderWebSite&quot;&gt;&lt;e name=&quot;HttpIn&quot; /&gt;&lt;e name=&quot;Microsoft.WindowsAzure.Plugins.RemoteAccess.Rdp&quot; /&gt;&lt;/r&gt;&lt;/m&gt;" />
            </settings>
            <resourcereferences>
              <resourceReference name="DiagnosticStore" defaultAmount="[4096,4096,4096]" defaultSticky="true" kind="Directory" />
              <resourceReference name="EventStore" defaultAmount="[1000,1000,1000]" defaultSticky="false" kind="LogStore" />
            </resourcereferences>
            <storedcertificates>
              <storedCertificate name="Stored0Microsoft.WindowsAzure.Plugins.RemoteAccess.PasswordEncryption" certificateStore="My" certificateLocation="System">
                <certificate>
                  <certificateMoniker name="/AzureService/AzureServiceGroup/OrderService/Microsoft.WindowsAzure.Plugins.RemoteAccess.PasswordEncryption" />
                </certificate>
              </storedCertificate>
            </storedcertificates>
            <certificates>
              <certificate name="Microsoft.WindowsAzure.Plugins.RemoteAccess.PasswordEncryption" />
            </certificates>
          </role>
          <sCSPolicy>
            <sCSPolicyIDMoniker name="/AzureService/AzureServiceGroup/OrderServiceInstances" />
            <sCSPolicyFaultDomainMoniker name="/AzureService/AzureServiceGroup/OrderServiceFaultDomains" />
          </sCSPolicy>
        </groupHascomponents>
        <groupHascomponents>
          <role name="OrderWebSite" generation="1" functional="0" release="0" software="D:\Github.com\NServiceBus-3.2.8\Samples\Azure\AzurePubSub\csx\Debug\roles\OrderWebSite" entryPoint="base\x64\WaHostBootstrapper.exe" parameters="base\x64\WaIISHost.exe " memIndex="1792" hostingEnvironment="frontendadmin" hostingEnvironmentVersion="2">
            <componentports>
              <inPort name="HttpIn" protocol="http" portRanges="80" />
              <inPort name="Microsoft.WindowsAzure.Plugins.RemoteAccess.Rdp" protocol="tcp" portRanges="3389" />
              <outPort name="OrderService:Microsoft.WindowsAzure.Plugins.RemoteAccess.Rdp" protocol="tcp">
                <outToChannel>
                  <sFSwitchChannelMoniker name="/AzureService/AzureServiceGroup/SW:OrderService:Microsoft.WindowsAzure.Plugins.RemoteAccess.Rdp" />
                </outToChannel>
              </outPort>
              <outPort name="OrderWebSite:Microsoft.WindowsAzure.Plugins.RemoteAccess.Rdp" protocol="tcp">
                <outToChannel>
                  <sFSwitchChannelMoniker name="/AzureService/AzureServiceGroup/SW:OrderWebSite:Microsoft.WindowsAzure.Plugins.RemoteAccess.Rdp" />
                </outToChannel>
              </outPort>
            </componentports>
            <settings>
              <aCS name="AzureQueueConfig.ConnectionString" defaultValue="" />
              <aCS name="AzureQueueConfig.QueueName" defaultValue="" />
              <aCS name="AzureSubscriptionStorageConfig.ConnectionString" defaultValue="" />
              <aCS name="Diagnostics.ConnectionString" defaultValue="" />
              <aCS name="Diagnostics.Level" defaultValue="" />
              <aCS name="MessageForwardingInCaseOfFaultConfig.ErrorQueue" defaultValue="" />
              <aCS name="Microsoft.WindowsAzure.Plugins.RemoteAccess.AccountEncryptedPassword" defaultValue="" />
              <aCS name="Microsoft.WindowsAzure.Plugins.RemoteAccess.AccountExpiration" defaultValue="" />
              <aCS name="Microsoft.WindowsAzure.Plugins.RemoteAccess.AccountUsername" defaultValue="" />
              <aCS name="Microsoft.WindowsAzure.Plugins.RemoteAccess.Enabled" defaultValue="" />
              <aCS name="MsmqTransportConfig.MaxRetries" defaultValue="" />
              <aCS name="MsmqTransportConfig.NumberOfWorkerThreads" defaultValue="" />
              <aCS name="__ModelData" defaultValue="&lt;m role=&quot;OrderWebSite&quot; xmlns=&quot;urn:azure:m:v1&quot;&gt;&lt;r name=&quot;OrderService&quot;&gt;&lt;e name=&quot;Microsoft.WindowsAzure.Plugins.RemoteAccess.Rdp&quot; /&gt;&lt;e name=&quot;Microsoft.WindowsAzure.Plugins.RemoteForwarder.RdpInput&quot; /&gt;&lt;/r&gt;&lt;r name=&quot;OrderWebSite&quot;&gt;&lt;e name=&quot;HttpIn&quot; /&gt;&lt;e name=&quot;Microsoft.WindowsAzure.Plugins.RemoteAccess.Rdp&quot; /&gt;&lt;/r&gt;&lt;/m&gt;" />
            </settings>
            <resourcereferences>
              <resourceReference name="DiagnosticStore" defaultAmount="[4096,4096,4096]" defaultSticky="true" kind="Directory" />
              <resourceReference name="EventStore" defaultAmount="[1000,1000,1000]" defaultSticky="false" kind="LogStore" />
            </resourcereferences>
            <storedcertificates>
              <storedCertificate name="Stored0Microsoft.WindowsAzure.Plugins.RemoteAccess.PasswordEncryption" certificateStore="My" certificateLocation="System">
                <certificate>
                  <certificateMoniker name="/AzureService/AzureServiceGroup/OrderWebSite/Microsoft.WindowsAzure.Plugins.RemoteAccess.PasswordEncryption" />
                </certificate>
              </storedCertificate>
            </storedcertificates>
            <certificates>
              <certificate name="Microsoft.WindowsAzure.Plugins.RemoteAccess.PasswordEncryption" />
            </certificates>
          </role>
          <sCSPolicy>
            <sCSPolicyIDMoniker name="/AzureService/AzureServiceGroup/OrderWebSiteInstances" />
            <sCSPolicyFaultDomainMoniker name="/AzureService/AzureServiceGroup/OrderWebSiteFaultDomains" />
          </sCSPolicy>
        </groupHascomponents>
      </components>
      <sCSPolicy>
        <sCSPolicyFaultDomain name="OrderServiceFaultDomains" defaultPolicy="[2,2,2]" />
        <sCSPolicyFaultDomain name="OrderWebSiteFaultDomains" defaultPolicy="[2,2,2]" />
        <sCSPolicyID name="OrderServiceInstances" defaultPolicy="[1,1,1]" />
        <sCSPolicyID name="OrderWebSiteInstances" defaultPolicy="[1,1,1]" />
      </sCSPolicy>
    </group>
  </groups>
  <implements>
    <implementation Id="3af23798-869d-486d-8ac5-47a253c129b6" ref="Microsoft.RedDog.Contract\ServiceContract\AzureServiceContract@ServiceDefinition.build">
      <interfacereferences>
        <interfaceReference Id="657404eb-c1d6-4b28-b316-28ab9ca5c8ec" ref="Microsoft.RedDog.Contract\Interface\OrderService:Microsoft.WindowsAzure.Plugins.RemoteForwarder.RdpInput@ServiceDefinition.build">
          <inPort>
            <inPortMoniker name="/AzureService/AzureServiceGroup/OrderService:Microsoft.WindowsAzure.Plugins.RemoteForwarder.RdpInput" />
          </inPort>
        </interfaceReference>
        <interfaceReference Id="e6dc0971-0234-4594-867b-554e6c91c0a8" ref="Microsoft.RedDog.Contract\Interface\OrderWebSite:HttpIn@ServiceDefinition.build">
          <inPort>
            <inPortMoniker name="/AzureService/AzureServiceGroup/OrderWebSite:HttpIn" />
          </inPort>
        </interfaceReference>
      </interfacereferences>
    </implementation>
  </implements>
</serviceModel>