function RunTest (
    [Parameter(Position=0,Mandatory=0)]
    [int]$numThreads = 10,
    [Parameter(Position=1,Mandatory=0)]
    [int]$numMessages = 10000,
    [Parameter(Position=2,Mandatory=0)]
    [string]$serializationFormat = "xml",
    [Parameter(Position=3,Mandatory=0)]
    [string]$transport = "msmq",
    [Parameter(Position=4,Mandatory=0)]
    [string]$mode = "nonvolatile",
    [Parameter(Position=5,Mandatory=0)]
    [string]$messagemode = "normalmessages",
    [Parameter(Position=6,Mandatory=0)]
    [string]$persistence = "ravendb",
	[Parameter(Position=7,Mandatory=0)]
    [string]$concurrency = "1"
)
{

  
    ..\.\bin\release\Runner.exe $numThreads $numMessages $serializationFormat $transport $mode $messagemode $persistence $concurrency

}

function Cleanup ()
{
	sqlcmd -S .\SQLEXPRESS -d NServiceBus -i .\Reset-Database.sql | Out-Null
	
	..\..\NServiceBus.Core\Transports\Msmq\Scripts\Reset-Msmq.ps1 | Out-Null
}