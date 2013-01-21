function Sync (
    [Parameter(Position=0,Mandatory=1)]
    [string]$transport = "MSMQ",
    [Parameter(Position=1,Mandatory=1)]
    [string]$connectionString = "MSMQ"
)
{
    robocopy Messaging.SqlServer Messaging.$transport /MIR /FFT /Z /XA:H /W:5
	Rename-Item Messaging.$transport\Messaging.SqlServer.sln Messaging.$transport.sln
    
	(Get-Content Messaging.$transport\MyWebClient\Web.config) | 
    Foreach-Object {
        $_ -replace "Data Source=\.\\SQLEXPRESS;Initial Catalog=nservicebus;Integrated Security=True", $connectionString
    } | 
    Set-Content Messaging.$transport\MyWebClient\Web.config
	
	(Get-Content Messaging.$transport\MySubscriber\App.config) | 
    Foreach-Object {
        $_ -replace "Data Source=\.\\SQLEXPRESS;Initial Catalog=nservicebus;Integrated Security=True", $connectionString
    } | 
    Set-Content Messaging.$transport\MySubscriber\App.config
	
	(Get-Content Messaging.$transport\MyServer\App.config) | 
    Foreach-Object {
        $_ -replace "Data Source=\.\\SQLEXPRESS;Initial Catalog=nservicebus;Integrated Security=True", $connectionString
    } | 
    Set-Content Messaging.$transport\MyServer\App.config
	
	(Get-Content Messaging.$transport\MyRequestResponseEndpoint\App.config) | 
    Foreach-Object {
        $_ -replace "Data Source=\.\\SQLEXPRESS;Initial Catalog=nservicebus;Integrated Security=True", $connectionString
    } | 
    Set-Content Messaging.$transport\MyRequestResponseEndpoint\App.config
    
    (Get-Content Messaging.$transport\MyWebClient\Global.asax.cs) | 
    Foreach-Object {
        $_ -replace "UseTransport<SqlServer>", "UseTransport<$transport>"
    } | 
    Set-Content Messaging.$transport\MyWebClient\Global.asax.cs
    
    (Get-Content Messaging.$transport\MySubscriber\EndpointConfig.cs) | 
    Foreach-Object {
        $_ -replace "SqlServer", $transport
    } | 
    Set-Content Messaging.$transport\MySubscriber\EndpointConfig.cs
    
    (Get-Content Messaging.$transport\MyServer\EndpointConfig.cs) | 
    Foreach-Object {
        $_ -replace "SqlServer", $transport
    } | 
    Set-Content Messaging.$transport\MyServer\EndpointConfig.cs
    
    (Get-Content Messaging.$transport\MyRequestResponseEndpoint\EndpointConfig.cs) | 
    Foreach-Object {
        $_ -replace "SqlServer", $transport
    } | 
    Set-Content Messaging.$transport\MyRequestResponseEndpoint\EndpointConfig.cs
}


Sync -transport "RabbitMQ" -connectionString "host=localhost"
Sync -transport "Msmq" -connectionString "cache=true"
Sync -transport "ActiveMQ" -connectionString "activemq:tcp://localhost:61616"