function RunTest ()
{
	param(
		[Parameter(Position=0,Mandatory=$false)]
		[int] $numThreads = 10,
		[Parameter(Position=1,Mandatory=$false)]
		[int] $numMessages = 10000,
		[Parameter(Position=2,Mandatory=$false)]
		[string] $serializationFormat = "xml",
		[Parameter(Position=3,Mandatory=$false)]
		[string] $transport = "msmq",
		[Parameter(Position=4,Mandatory=$false)]
		[string] $mode = "nonvolatile",
		[Parameter(Position=5,Mandatory=$false)]
		[string] $messagemode = "normalmessages",
		[Parameter(Position=6,Mandatory=$false)]
		[string] $persistence = "ravendb",
		[Parameter(Position=7,Mandatory=$false)]
		[string] $concurrency = "1"
)

	$file = "..\.\bin\debug\Runner.exe"
	$fullpath = Resolve-Path $file  -ErrorAction SilentlyContinue
	
	if (!$fullpath) {
		throw "{0} not found - exiting..." -f $file
	}
	else {
	 . $file $numThreads $numMessages $serializationFormat $transport $mode $messagemode $persistence $concurrency
	}
}

function Reset-Msmq() {
	AssertIsAnAdministrator
	if (-not ([System.Management.Automation.PSTypeName]'System.Messaging.MessageQueue').Type) {
		 [void] [Reflection.Assembly]::LoadWithPartialName("System.Messaging") 
	}

	[System.Messaging.MessageQueue]::GetPrivateQueuesByMachine("localhost") | % {".\" + $_.QueueName} | % {[System.Messaging.MessageQueue]::Delete($_) } | Out-Null
}


function Cleanup()
{
	sqlcmd -S .\SQLEXPRESS -d NServiceBus -i .\Reset-Database.sql | Out-Null
	Reset-Msmq 
}

function AssertIsAnAdministrator()
{
	$currentIdentity = [System.Security.Principal.WindowsIdentity]::GetCurrent()
	$currentPrincipal = new-object System.Security.Principal.WindowsPrincipal($currentIdentity)
	$adminRole = [ System.Security.Principal.WindowsBuiltInRole]::Administrator

	if (-not $currentPrincipal.IsInRole($adminRole)) {
		throw "Elevation required to run"
	}
}




 